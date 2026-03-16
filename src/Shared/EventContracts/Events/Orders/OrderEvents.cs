using Common.Models.Domain;
using EventContracts.Base;

namespace EventContracts.Events.Orders;

/// <summary>
/// Event published when a new order is created
/// Triggers: Risk analysis, portfolio validation, market data subscription
/// </summary>
public class OrderCreatedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid PortfolioId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public OrderType Type { get; set; }
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal? LimitPrice { get; set; }
    public decimal? StopPrice { get; set; }
    public TimeInForce TimeInForce { get; set; }
    public decimal RiskScore { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

/// <summary>
/// Event published when order passes risk validation
/// Triggers: Order submission to market, portfolio update
/// </summary>
public class OrderValidatedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public bool RiskCheckPassed { get; set; }
    public decimal RiskScore { get; set; }
    public string? RejectionReason { get; set; }
}

/// <summary>
/// Event published when order is submitted to market
/// Triggers: Market data monitoring, execution tracking
/// </summary>
public class OrderSubmittedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public OrderType Type { get; set; }
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public DateTime SubmittedAt { get; set; }
}

/// <summary>
/// Event published when order is filled (fully or partially)
/// Triggers: Portfolio update, transaction creation, notification, reporting
/// </summary>
public class OrderFilledEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid PortfolioId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public decimal FilledQuantity { get; set; }
    public decimal FillPrice { get; set; }
    public decimal Commission { get; set; }
    public decimal TotalValue { get; set; }
    public bool IsFullyFilled { get; set; }
    public DateTime FilledAt { get; set; }
}

/// <summary>
/// Event published when order is cancelled
/// Triggers: Portfolio update, notification
/// </summary>
public class OrderCancelledEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid PortfolioId { get; set; }
    public string CancellationReason { get; set; } = string.Empty;
    public DateTime CancelledAt { get; set; }
}

/// <summary>
/// Event published when order is rejected
/// Triggers: Notification, audit logging
/// </summary>
public class OrderRejectedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid PortfolioId { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public DateTime RejectedAt { get; set; }
}
