using Common.Models.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingEngine.API.Commands;
using TradingEngine.API.Queries;

namespace TradingEngine.API.Controllers;

/// <summary>
/// Trading Engine API - Core order execution and management
/// 
/// Responsibilities:
/// - Order creation and validation
/// - Order execution (market, limit, stop orders)
/// - Order cancellation and modification
/// - Order status tracking
/// - Integration with risk analysis
/// 
/// Interview Key Points:
/// - Idempotency: Duplicate order prevention using idempotency keys
/// - CQRS: Commands for writes, queries for reads
/// - Event Sourcing: Complete audit trail of order state changes
/// - Circuit Breaker: Graceful degradation when external services fail
/// - Rate Limiting: Protection against order spam
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new order
    /// POST /api/orders
    /// </summary>
    /// <remarks>
    /// Idempotency: Include X-Idempotency-Key header to prevent duplicate orders
    /// Rate Limiting: 100 orders/minute for authenticated users
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<OrderResponse>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey)
    {
        try
        {
            var userId = User.Identity?.Name ?? throw new UnauthorizedAccessException();

            var command = new CreateOrderCommand
            {
                UserId = userId,
                PortfolioId = request.PortfolioId,
                Symbol = request.Symbol,
                OrderType = request.OrderType,
                Side = request.Side,
                Quantity = request.Quantity,
                LimitPrice = request.LimitPrice,
                StopPrice = request.StopPrice,
                TimeInForce = request.TimeInForce,
                IdempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString()
            };

            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "DUPLICATE_ORDER" => Conflict(new ProblemDetails
                    {
                        Title = "Duplicate Order",
                        Detail = result.ErrorMessage,
                        Status = StatusCodes.Status409Conflict
                    }),
                    "INSUFFICIENT_FUNDS" => BadRequest(new ProblemDetails
                    {
                        Title = "Insufficient Funds",
                        Detail = result.ErrorMessage,
                        Status = StatusCodes.Status400BadRequest
                    }),
                    "RISK_CHECK_FAILED" => BadRequest(new ProblemDetails
                    {
                        Title = "Risk Check Failed",
                        Detail = result.ErrorMessage,
                        Status = StatusCodes.Status400BadRequest
                    }),
                    _ => BadRequest(new ProblemDetails
                    {
                        Title = "Order Creation Failed",
                        Detail = result.ErrorMessage,
                        Status = StatusCodes.Status400BadRequest
                    })
                };
            }

            _logger.LogInformation(
                "Order created successfully. OrderId: {OrderId}, UserId: {UserId}, Symbol: {Symbol}",
                result.Order!.Id,
                userId,
                request.Symbol
            );

            return CreatedAtAction(
                nameof(GetOrder),
                new { orderId = result.Order.Id },
                new OrderResponse(result.Order)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for symbol {Symbol}", request.Symbol);
            throw;
        }
    }

    /// <summary>
    /// Get order by ID
    /// GET /api/orders/{orderId}
    /// </summary>
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetOrder(Guid orderId)
    {
        var userId = User.Identity?.Name ?? throw new UnauthorizedAccessException();

        var query = new GetOrderQuery { OrderId = orderId, UserId = userId };
        var order = await _mediator.Send(query);

        if (order == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Order Not Found",
                Detail = $"Order with ID {orderId} not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new OrderResponse(order));
    }

    /// <summary>
    /// Get all orders for current user
    /// GET /api/orders?status=Pending&symbol=AAPL&pageSize=50&pageNumber=1
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderResponse>>> GetOrders(
        [FromQuery] OrderStatus? status = null,
        [FromQuery] string? symbol = null,
        [FromQuery] Guid? portfolioId = null,
        [FromQuery] int pageSize = 50,
        [FromQuery] int pageNumber = 1)
    {
        var userId = User.Identity?.Name ?? throw new UnauthorizedAccessException();

        var query = new GetOrdersQuery
        {
            UserId = userId,
            Status = status,
            Symbol = symbol,
            PortfolioId = portfolioId,
            PageSize = Math.Min(pageSize, 100), // Max 100 per page
            PageNumber = Math.Max(pageNumber, 1)
        };

        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Cancel an order
    /// POST /api/orders/{orderId}/cancel
    /// </summary>
    [HttpPost("{orderId:guid}/cancel")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> CancelOrder(
        Guid orderId,
        [FromBody] CancelOrderRequest? request = null)
    {
        var userId = User.Identity?.Name ?? throw new UnauthorizedAccessException();

        var command = new CancelOrderCommand
        {
            OrderId = orderId,
            UserId = userId,
            CancellationReason = request?.Reason ?? "User requested"
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "ORDER_NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Order Not Found",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status404NotFound
                }),
                "CANNOT_CANCEL" => BadRequest(new ProblemDetails
                {
                    Title = "Cannot Cancel Order",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                }),
                _ => BadRequest(new ProblemDetails
                {
                    Title = "Cancellation Failed",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                })
            };
        }

        _logger.LogInformation(
            "Order cancelled. OrderId: {OrderId}, UserId: {UserId}",
            orderId,
            userId
        );

        return Ok(new OrderResponse(result.Order!));
    }
}

// DTOs
public record CreateOrderRequest(
    Guid PortfolioId,
    string Symbol,
    OrderType OrderType,
    OrderSide Side,
    decimal Quantity,
    decimal? LimitPrice,
    decimal? StopPrice,
    TimeInForce TimeInForce
);

public record CancelOrderRequest(string Reason);

public record OrderResponse
{
    public Guid Id { get; init; }
    public Guid PortfolioId { get; init; }
    public string Symbol { get; init; }
    public string OrderType { get; init; }
    public string Side { get; init; }
    public decimal Quantity { get; init; }
    public decimal? LimitPrice { get; init; }
    public decimal? StopPrice { get; init; }
    public string Status { get; init; }
    public decimal FilledQuantity { get; init; }
    public decimal? AverageFillPrice { get; init; }
    public decimal TotalValue { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? FilledAt { get; init; }

    public OrderResponse(Order order)
    {
        Id = order.Id;
        PortfolioId = order.PortfolioId;
        Symbol = order.Symbol;
        OrderType = order.Type.ToString();
        Side = order.Side.ToString();
        Quantity = order.Quantity;
        LimitPrice = order.LimitPrice;
        StopPrice = order.StopPrice;
        Status = order.Status.ToString();
        FilledQuantity = order.FilledQuantity;
        AverageFillPrice = order.AverageFillPrice;
        TotalValue = order.TotalValue;
        CreatedAt = order.CreatedAt;
        SubmittedAt = order.SubmittedAt;
        FilledAt = order.FilledAt;
    }
}

public record PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
