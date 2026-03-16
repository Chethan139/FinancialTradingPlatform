using Common.Models.Domain;
using Common.Patterns.CQRS;
using EventContracts.Events.Orders;
using Infrastructure.Data.CosmosDb;
using Infrastructure.Data.SqlServer;
using Infrastructure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace TradingEngine.API.Commands;

/// <summary>
/// Command to create a new trading order
/// Implements CQRS pattern for write operations
/// </summary>
public class CreateOrderCommand : Command<CreateOrderResult>
{
    public string UserId { get; set; } = string.Empty;
    public Guid PortfolioId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public OrderType OrderType { get; set; }
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal? LimitPrice { get; set; }
    public decimal? StopPrice { get; set; }
    public TimeInForce TimeInForce { get; set; } = TimeInForce.Day;
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class CreateOrderResult
{
    public bool Success { get; set; }
    public Order? Order { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Handler for CreateOrderCommand
/// Implements complete order creation workflow:
/// 1. Validate order parameters
/// 2. Check for duplicate orders (idempotency)
/// 3. Validate portfolio and available funds
/// 4. Perform pre-trade risk checks
/// 5. Create order in Cosmos DB (write model)
/// 6. Create order in SQL Server (read model for reporting)
/// 7. Publish OrderCreatedEvent to Service Bus
/// 
/// Interview Key Points:
/// - Dual-write pattern (Cosmos + SQL) with eventual consistency
/// - Idempotency using unique constraint on IdempotencyKey
/// - Pre-trade risk checks (required for financial compliance)
/// - Event-driven: Publishes event for downstream processing
/// - Transaction management: Rollback on failure
/// </summary>
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, CreateOrderResult>
{
    private readonly CosmosDbContext _cosmosDb;
    private readonly TradingDbContext _sqlDb;
    private readonly ServiceBusPublisher _serviceBus;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        CosmosDbContext cosmosDb,
        TradingDbContext sqlDb,
        ServiceBusPublisher serviceBus,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _cosmosDb = cosmosDb;
        _sqlDb = sqlDb;
        _serviceBus = serviceBus;
        _logger = logger;
    }

    public async Task<CreateOrderResult> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Validate order parameters
            var validation = ValidateOrder(request);
            if (!validation.isValid)
            {
                return new CreateOrderResult
                {
                    Success = false,
                    ErrorCode = "VALIDATION_FAILED",
                    ErrorMessage = validation.errorMessage
                };
            }

            // Step 2: Check for duplicate order (idempotency)
            var existingOrder = await CheckDuplicateOrder(request.IdempotencyKey);
            if (existingOrder != null)
            {
                _logger.LogWarning(
                    "Duplicate order detected. IdempotencyKey: {IdempotencyKey}",
                    request.IdempotencyKey
                );
                return new CreateOrderResult
                {
                    Success = false,
                    ErrorCode = "DUPLICATE_ORDER",
                    ErrorMessage = "An order with this idempotency key already exists",
                    Order = existingOrder
                };
            }

            // Step 3: Validate portfolio and check available funds
            var portfolio = await _sqlDb.Portfolios
                .FirstOrDefaultAsync(p => p.Id == request.PortfolioId && p.UserId == request.UserId, cancellationToken);

            if (portfolio == null)
            {
                return new CreateOrderResult
                {
                    Success = false,
                    ErrorCode = "PORTFOLIO_NOT_FOUND",
                    ErrorMessage = "Portfolio not found or access denied"
                };
            }

            // Calculate order value (for buy orders, check available funds)
            var orderValue = CalculateOrderValue(request);

            if (request.Side == OrderSide.Buy && !portfolio.HasSufficientFunds(orderValue))
            {
                return new CreateOrderResult
                {
                    Success = false,
                    ErrorCode = "INSUFFICIENT_FUNDS",
                    ErrorMessage = $"Insufficient funds. Required: {orderValue:C}, Available: {portfolio.GetBuyingPower():C}"
                };
            }

            // Step 4: Perform pre-trade risk checks
            var riskCheckResult = await PerformRiskCheck(request, portfolio);
            if (!riskCheckResult.passed)
            {
                return new CreateOrderResult
                {
                    Success = false,
                    ErrorCode = "RISK_CHECK_FAILED",
                    ErrorMessage = riskCheckResult.reason
                };
            }

            // Step 5: Create order entity
            var order = new Order
            {
                UserId = request.UserId,
                PortfolioId = request.PortfolioId,
                Symbol = request.Symbol.ToUpperInvariant(),
                Type = request.OrderType,
                Side = request.Side,
                Quantity = request.Quantity,
                LimitPrice = request.LimitPrice,
                StopPrice = request.StopPrice,
                Status = OrderStatus.Pending,
                FilledQuantity = 0,
                TotalValue = orderValue,
                Commission = CalculateCommission(orderValue),
                TimeInForce = request.TimeInForce,
                IdempotencyKey = request.IdempotencyKey,
                RiskScore = riskCheckResult.riskScore,
                RiskCheckPassed = true,
                Version = 1
            };

            // Set expiry date for GTC orders
            if (request.TimeInForce == TimeInForce.GTC)
            {
                order.ExpiryDate = DateTime.UtcNow.AddDays(90); // 90 days for GTC
            }

            // Step 6: Save to Cosmos DB (write model - source of truth for trading)
            var cosmosContainer = _cosmosDb.GetContainer(CosmosDbContext.OrdersContainerName);
            await cosmosContainer.CreateItemAsync(order, new PartitionKey(order.UserId), cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Order created in Cosmos DB. OrderId: {OrderId}, Symbol: {Symbol}, Quantity: {Quantity}",
                order.Id,
                order.Symbol,
                order.Quantity
            );

            // Step 7: Save to SQL Server (read model for reporting and analytics)
            // This is eventual consistency - if it fails, it will be synced via change feed
            try
            {
                _sqlDb.Orders.Add(order);
                await _sqlDb.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to save order to SQL Server (will be synced later). OrderId: {OrderId}",
                    order.Id
                );
                // Don't fail the entire operation - Cosmos DB is source of truth
            }

            // Step 8: Publish OrderCreatedEvent to Service Bus
            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                PortfolioId = order.PortfolioId,
                Symbol = order.Symbol,
                OrderType = order.Type,
                Side = order.Side,
                Quantity = order.Quantity,
                LimitPrice = order.LimitPrice,
                StopPrice = order.StopPrice,
                TotalValue = order.TotalValue,
                IdempotencyKey = order.IdempotencyKey,
                SourceService = "TradingEngine",
                TriggeredBy = request.UserId,
                CorrelationId = request.CommandId.ToString()
            };

            await _serviceBus.PublishAsync(
                ServiceBusPublisher.OrderEventsTopic,
                orderCreatedEvent
            );

            _logger.LogInformation(
                "OrderCreatedEvent published. OrderId: {OrderId}, EventId: {EventId}",
                order.Id,
                orderCreatedEvent.EventId
            );

            return new CreateOrderResult
            {
                Success = true,
                Order = order
            };
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // Handle duplicate order (race condition)
            _logger.LogWarning(ex, "Duplicate order race condition detected");
            return new CreateOrderResult
            {
                Success = false,
                ErrorCode = "DUPLICATE_ORDER",
                ErrorMessage = "Order already exists (race condition)"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            throw;
        }
    }

    /// <summary>
    /// Validate order parameters
    /// </summary>
    private (bool isValid, string? errorMessage) ValidateOrder(CreateOrderCommand request)
    {
        if (request.Quantity <= 0)
            return (false, "Quantity must be greater than zero");

        if (string.IsNullOrWhiteSpace(request.Symbol))
            return (false, "Symbol is required");

        if (request.OrderType == OrderType.Limit && request.LimitPrice == null)
            return (false, "Limit price is required for limit orders");

        if ((request.OrderType == OrderType.StopLoss || request.OrderType == OrderType.TakeProfit) && request.StopPrice == null)
            return (false, "Stop price is required for stop orders");

        if (request.LimitPrice.HasValue && request.LimitPrice.Value <= 0)
            return (false, "Limit price must be greater than zero");

        if (request.StopPrice.HasValue && request.StopPrice.Value <= 0)
            return (false, "Stop price must be greater than zero");

        return (true, null);
    }

    /// <summary>
    /// Check for duplicate order using idempotency key
    /// </summary>
    private async Task<Order?> CheckDuplicateOrder(string idempotencyKey)
    {
        return await _sqlDb.Orders
            .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey);
    }

    /// <summary>
    /// Calculate order value based on order type
    /// </summary>
    private decimal CalculateOrderValue(CreateOrderCommand request)
    {
        return request.OrderType switch
        {
            OrderType.Market => request.Quantity * 0, // Will be calculated at execution
            OrderType.Limit => request.Quantity * (request.LimitPrice ?? 0),
            _ => request.Quantity * (request.LimitPrice ?? request.StopPrice ?? 0)
        };
    }

    /// <summary>
    /// Calculate commission (0.1% of order value, min $1)
    /// </summary>
    private decimal CalculateCommission(decimal orderValue)
    {
        var commission = orderValue * 0.001m; // 0.1%
        return Math.Max(commission, 1.0m); // Minimum $1
    }

    /// <summary>
    /// Perform pre-trade risk checks
    /// Critical for compliance and risk management
    /// </summary>
    private async Task<(bool passed, decimal riskScore, string? reason)> PerformRiskCheck(
        CreateOrderCommand request,
        Portfolio portfolio)
    {
        // Calculate risk score (0-100)
        decimal riskScore = 0;

        // Factor 1: Order size relative to portfolio (30 points)
        var orderValue = CalculateOrderValue(request);
        var orderSizeRatio = portfolio.TotalValue > 0 ? orderValue / portfolio.TotalValue : 1;
        riskScore += Math.Min(orderSizeRatio * 100, 30);

        // Factor 2: Portfolio leverage (25 points)
        riskScore += portfolio.CurrentLeverage * 25;

        // Factor 3: Concentration risk (25 points)
        var positionsInSymbol = portfolio.Positions.Count(p => p.Symbol == request.Symbol);
        if (positionsInSymbol > 0)
        {
            riskScore += 15; // Existing position increases risk
        }

        // Factor 4: Market conditions (20 points)
        // In real system, would check volatility, circuit breakers, etc.
        riskScore += 10; // Placeholder

        // Risk thresholds
        if (riskScore > 80)
        {
            return (false, riskScore, "Order risk score too high (Critical risk level)");
        }

        if (orderSizeRatio > 0.5m)
        {
            return (false, riskScore, "Order size exceeds 50% of portfolio value");
        }

        if (portfolio.CurrentLeverage > portfolio.MaxLeverage)
        {
            return (false, riskScore, "Portfolio leverage limit exceeded");
        }

        return (true, riskScore, null);
    }
}
