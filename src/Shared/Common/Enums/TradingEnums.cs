namespace Common.Models.Domain;

/// <summary>
/// Type of trading order
/// </summary>
public enum OrderType
{
    /// <summary>
    /// Market order - executes immediately at current market price
    /// </summary>
    Market = 1,

    /// <summary>
    /// Limit order - executes only at specified price or better
    /// </summary>
    Limit = 2,

    /// <summary>
    /// Stop-loss order - triggers when price falls to stop price
    /// </summary>
    StopLoss = 3,

    /// <summary>
    /// Take-profit order - triggers when price rises to target price
    /// </summary>
    TakeProfit = 4,

    /// <summary>
    /// Stop-limit order - combination of stop and limit order
    /// </summary>
    StopLimit = 5,

    /// <summary>
    /// Trailing stop order - dynamic stop price that follows market
    /// </summary>
    TrailingStop = 6
}

/// <summary>
/// Side of the order - buy or sell
/// </summary>
public enum OrderSide
{
    Buy = 1,
    Sell = 2
}

/// <summary>
/// Current status of an order through its lifecycle
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order created but not yet submitted to market
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Order submitted to market, awaiting execution
    /// </summary>
    Submitted = 2,

    /// <summary>
    /// Order partially filled, remaining quantity active
    /// </summary>
    PartiallyFilled = 3,

    /// <summary>
    /// Order completely filled
    /// </summary>
    Filled = 4,

    /// <summary>
    /// Order cancelled by user or system
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Order rejected due to validation or risk checks
    /// </summary>
    Rejected = 6,

    /// <summary>
    /// Order expired (for time-limited orders)
    /// </summary>
    Expired = 7,

    /// <summary>
    /// Order failed due to system error
    /// </summary>
    Failed = 8
}

/// <summary>
/// Time-in-force specifies how long an order remains active
/// </summary>
public enum TimeInForce
{
    /// <summary>
    /// Day order - valid until end of trading day
    /// </summary>
    Day = 1,

    /// <summary>
    /// Good Till Cancelled - remains active until filled or cancelled
    /// </summary>
    GTC = 2,

    /// <summary>
    /// Immediate or Cancel - fill immediately or cancel unfilled portion
    /// </summary>
    IOC = 3,

    /// <summary>
    /// Fill or Kill - fill entire order immediately or cancel completely
    /// </summary>
    FOK = 4
}

/// <summary>
/// Transaction type for portfolio operations
/// </summary>
public enum TransactionType
{
    Buy = 1,
    Sell = 2,
    Deposit = 3,
    Withdrawal = 4,
    Dividend = 5,
    Interest = 6,
    Fee = 7,
    Commission = 8,
    Transfer = 9
}

/// <summary>
/// Asset class categorization
/// </summary>
public enum AssetClass
{
    Equity = 1,
    FixedIncome = 2,
    Commodity = 3,
    Currency = 4,
    Cryptocurrency = 5,
    Derivative = 6,
    RealEstate = 7,
    Alternative = 8
}

/// <summary>
/// Risk level categorization for risk analysis
/// </summary>
public enum RiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Market data event types
/// </summary>
public enum MarketDataEventType
{
    Trade = 1,
    Quote = 2,
    OrderBook = 3,
    TickerUpdate = 4,
    NewsUpdate = 5,
    CorporateAction = 6
}

/// <summary>
/// Notification channel types
/// </summary>
public enum NotificationChannel
{
    Email = 1,
    SMS = 2,
    Push = 3,
    InApp = 4,
    Webhook = 5
}

/// <summary>
/// Notification priority levels
/// </summary>
public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}
