using EventContracts.Base;

namespace EventContracts.Events.Portfolio;

/// <summary>
/// Event published when portfolio is updated
/// </summary>
public class PortfolioUpdatedEvent : IntegrationEvent
{
    public Guid PortfolioId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public decimal CashBalance { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public decimal TotalProfitLossPercentage { get; set; }
}

/// <summary>
/// Event published when a position is opened or added to
/// </summary>
public class PositionOpenedEvent : IntegrationEvent
{
    public Guid PortfolioId { get; set; }
    public Guid PositionId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal TotalCost { get; set; }
}

/// <summary>
/// Event published when a position is closed or reduced
/// </summary>
public class PositionClosedEvent : IntegrationEvent
{
    public Guid PortfolioId { get; set; }
    public Guid PositionId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal RealizedProfitLoss { get; set; }
    public bool IsFullyClosed { get; set; }
}

namespace EventContracts.Events.Market;

/// <summary>
/// Event published when market data is updated
/// High-frequency event published to Event Hubs for real-time processing
/// </summary>
public class MarketDataUpdatedEvent : IntegrationEvent
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercentage { get; set; }
    public long Volume { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Open { get; set; }
    public decimal Close { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Event published for trade execution data
/// Used for real-time trade surveillance and reporting
/// </summary>
public class TradeExecutedEvent : IntegrationEvent
{
    public Guid TradeId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public string Side { get; set; } = string.Empty; // Buy/Sell
    public DateTime ExecutionTime { get; set; }
    public string Exchange { get; set; } = string.Empty;
}

namespace EventContracts.Events.Risk;

/// <summary>
/// Event published when portfolio risk assessment is completed
/// </summary>
public class RiskAssessmentCompletedEvent : IntegrationEvent
{
    public Guid PortfolioId { get; set; }
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public Dictionary<string, decimal> RiskMetrics { get; set; } = new();
    public List<string> RiskWarnings { get; set; } = new();
    public DateTime AssessmentTime { get; set; }
}

/// <summary>
/// Event published when risk threshold is breached
/// Critical alert requiring immediate action
/// </summary>
public class RiskThresholdBreachedEvent : IntegrationEvent
{
    public Guid PortfolioId { get; set; }
    public string ThresholdType { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal ThresholdValue { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

namespace EventContracts.Events.Notifications;

/// <summary>
/// Event to trigger notification delivery
/// </summary>
public class SendNotificationEvent : IntegrationEvent
{
    public string UserId { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty; // Email, SMS, Push
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string Priority { get; set; } = "Normal";
}
