namespace Common.Models.Domain;

/// <summary>
/// Represents a trading order in the financial system
/// Supports market orders, limit orders, stop-loss, and take-profit orders
/// Implements event sourcing pattern for complete audit trail
/// </summary>
public class Order : BaseEntity
{
    /// <summary>
    /// User ID who placed the order
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Portfolio ID to which this order belongs
    /// </summary>
    public Guid PortfolioId { get; set; }

    /// <summary>
    /// Trading symbol (e.g., "AAPL", "MSFT", "BTC-USD")
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Order type: Market, Limit, StopLoss, TakeProfit
    /// </summary>
    public OrderType Type { get; set; }

    /// <summary>
    /// Order side: Buy or Sell
    /// </summary>
    public OrderSide Side { get; set; }

    /// <summary>
    /// Quantity of shares/units to trade
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Limit price for limit orders (null for market orders)
    /// </summary>
    public decimal? LimitPrice { get; set; }

    /// <summary>
    /// Stop price for stop-loss/take-profit orders
    /// </summary>
    public decimal? StopPrice { get; set; }

    /// <summary>
    /// Current status of the order
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Quantity filled so far (for partial fills)
    /// </summary>
    public decimal FilledQuantity { get; set; }

    /// <summary>
    /// Average fill price
    /// </summary>
    public decimal? AverageFillPrice { get; set; }

    /// <summary>
    /// Total value of the order (quantity * price)
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Commission/fees charged for this order
    /// </summary>
    public decimal Commission { get; set; }

    /// <summary>
    /// Time-in-force: Day, GTC (Good Till Cancelled), IOC (Immediate or Cancel)
    /// </summary>
    public TimeInForce TimeInForce { get; set; }

    /// <summary>
    /// Expiry date for GTC orders
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Timestamp when order was submitted to market
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Timestamp when order was filled/completed
    /// </summary>
    public DateTime? FilledAt { get; set; }

    /// <summary>
    /// Timestamp when order was cancelled
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Reason for cancellation (if applicable)
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Parent order ID (for bracket orders, OCO orders)
    /// </summary>
    public Guid? ParentOrderId { get; set; }

    /// <summary>
    /// Related orders (stop-loss, take-profit for a main order)
    /// </summary>
    public List<Guid> RelatedOrderIds { get; set; } = new();

    /// <summary>
    /// Risk score calculated at order placement (0-100)
    /// High-frequency trading requires pre-trade risk checks
    /// </summary>
    public decimal RiskScore { get; set; }

    /// <summary>
    /// Whether the order passed pre-trade risk checks
    /// </summary>
    public bool RiskCheckPassed { get; set; }

    /// <summary>
    /// Idempotency key to prevent duplicate order submissions
    /// Critical for distributed systems to ensure exactly-once processing
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>
    /// Version number for event sourcing
    /// Tracks the number of state changes
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Calculate remaining quantity to be filled
    /// </summary>
    public decimal GetRemainingQuantity() => Quantity - FilledQuantity;

    /// <summary>
    /// Check if order is fully filled
    /// </summary>
    public bool IsFullyFilled() => FilledQuantity >= Quantity;

    /// <summary>
    /// Check if order is partially filled
    /// </summary>
    public bool IsPartiallyFilled() => FilledQuantity > 0 && FilledQuantity < Quantity;

    /// <summary>
    /// Check if order can be cancelled
    /// </summary>
    public bool CanBeCancelled() =>
        Status == OrderStatus.Pending ||
        Status == OrderStatus.PartiallyFilled ||
        Status == OrderStatus.Submitted;
}
