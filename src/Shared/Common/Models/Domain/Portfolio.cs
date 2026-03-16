namespace Common.Models.Domain;

/// <summary>
/// Represents an investment portfolio containing positions, cash, and trading history
/// Implements aggregate root pattern in DDD
/// </summary>
public class Portfolio : BaseEntity
{
    /// <summary>
    /// User ID who owns this portfolio
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Portfolio name (e.g., "Retirement Account", "Trading Portfolio")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Portfolio description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Available cash balance in base currency (USD)
    /// </summary>
    public decimal CashBalance { get; set; }

    /// <summary>
    /// Base currency code (e.g., "USD", "EUR")
    /// </summary>
    public string BaseCurrency { get; set; } = "USD";

    /// <summary>
    /// Total market value of all positions + cash
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Total invested capital (initial deposits + subsequent deposits - withdrawals)
    /// </summary>
    public decimal InvestedCapital { get; set; }

    /// <summary>
    /// Total profit/loss amount
    /// </summary>
    public decimal TotalProfitLoss { get; set; }

    /// <summary>
    /// Total profit/loss percentage
    /// </summary>
    public decimal TotalProfitLossPercentage { get; set; }

    /// <summary>
    /// Daily profit/loss amount
    /// </summary>
    public decimal DailyProfitLoss { get; set; }

    /// <summary>
    /// Daily profit/loss percentage
    /// </summary>
    public decimal DailyProfitLossPercentage { get; set; }

    /// <summary>
    /// Risk score for the entire portfolio (0-100)
    /// Calculated based on volatility, concentration, leverage
    /// </summary>
    public decimal RiskScore { get; set; }

    /// <summary>
    /// Risk level classification
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Maximum allowed leverage for this portfolio
    /// </summary>
    public decimal MaxLeverage { get; set; } = 1.0m;

    /// <summary>
    /// Current leverage ratio
    /// </summary>
    public decimal CurrentLeverage { get; set; }

    /// <summary>
    /// Whether margin trading is enabled
    /// </summary>
    public bool MarginEnabled { get; set; }

    /// <summary>
    /// Available margin for trading
    /// </summary>
    public decimal AvailableMargin { get; set; }

    /// <summary>
    /// Used margin (for open positions)
    /// </summary>
    public decimal UsedMargin { get; set; }

    /// <summary>
    /// Is portfolio active or archived
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Collection of positions in this portfolio
    /// </summary>
    public List<Position> Positions { get; set; } = new();

    /// <summary>
    /// Collection of transaction history
    /// </summary>
    public List<Transaction> Transactions { get; set; } = new();

    /// <summary>
    /// Calculate total position value
    /// </summary>
    public decimal GetTotalPositionValue()
    {
        return Positions
            .Where(p => !p.IsDeleted)
            .Sum(p => p.MarketValue);
    }

    /// <summary>
    /// Calculate buying power (cash + available margin)
    /// </summary>
    public decimal GetBuyingPower()
    {
        return CashBalance + (MarginEnabled ? AvailableMargin : 0);
    }

    /// <summary>
    /// Check if portfolio has sufficient funds for an order
    /// </summary>
    public bool HasSufficientFunds(decimal requiredAmount)
    {
        return GetBuyingPower() >= requiredAmount;
    }

    /// <summary>
    /// Update portfolio metrics (called after position changes)
    /// </summary>
    public void RecalculateMetrics()
    {
        var positionValue = GetTotalPositionValue();
        TotalValue = CashBalance + positionValue;
        
        if (InvestedCapital > 0)
        {
            TotalProfitLoss = TotalValue - InvestedCapital;
            TotalProfitLossPercentage = (TotalProfitLoss / InvestedCapital) * 100;
        }

        // Calculate leverage
        if (TotalValue > 0)
        {
            CurrentLeverage = (positionValue + UsedMargin) / TotalValue;
        }
    }
}
