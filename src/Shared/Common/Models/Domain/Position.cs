namespace Common.Models.Domain;

/// <summary>
/// Represents a position (holding) in a portfolio
/// Tracks quantity, cost basis, current value, and P&L
/// </summary>
public class Position : BaseEntity
{
    /// <summary>
    /// Portfolio ID this position belongs to
    /// </summary>
    public Guid PortfolioId { get; set; }

    /// <summary>
    /// Trading symbol (e.g., "AAPL", "MSFT")
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Asset class (Equity, Cryptocurrency, etc.)
    /// </summary>
    public AssetClass AssetClass { get; set; }

    /// <summary>
    /// Total quantity held (can be negative for short positions)
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Average cost per unit
    /// </summary>
    public decimal AverageCost { get; set; }

    /// <summary>
    /// Total cost basis (quantity * average cost)
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Current market price per unit
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Current market value (quantity * current price)
    /// </summary>
    public decimal MarketValue { get; set; }

    /// <summary>
    /// Unrealized profit/loss amount
    /// </summary>
    public decimal UnrealizedProfitLoss { get; set; }

    /// <summary>
    /// Unrealized profit/loss percentage
    /// </summary>
    public decimal UnrealizedProfitLossPercentage { get; set; }

    /// <summary>
    /// Realized profit/loss from closed portions of this position
    /// </summary>
    public decimal RealizedProfitLoss { get; set; }

    /// <summary>
    /// Total dividend/interest received for this position
    /// </summary>
    public decimal TotalDividends { get; set; }

    /// <summary>
    /// Day change amount
    /// </summary>
    public decimal DayChange { get; set; }

    /// <summary>
    /// Day change percentage
    /// </summary>
    public decimal DayChangePercentage { get; set; }

    /// <summary>
    /// Timestamp of last price update
    /// </summary>
    public DateTime LastPriceUpdateAt { get; set; }

    /// <summary>
    /// Whether this is a short position
    /// </summary>
    public bool IsShortPosition { get; set; }

    /// <summary>
    /// Portfolio reference navigation property
    /// </summary>
    public Portfolio? Portfolio { get; set; }

    /// <summary>
    /// Update position with new market price
    /// </summary>
    public void UpdateMarketPrice(decimal newPrice)
    {
        var previousPrice = CurrentPrice;
        CurrentPrice = newPrice;
        MarketValue = Quantity * CurrentPrice;
        
        // Calculate unrealized P&L
        UnrealizedProfitLoss = MarketValue - TotalCost;
        if (TotalCost != 0)
        {
            UnrealizedProfitLossPercentage = (UnrealizedProfitLoss / TotalCost) * 100;
        }

        // Calculate day change
        if (previousPrice != 0)
        {
            var priceChange = CurrentPrice - previousPrice;
            DayChange = priceChange * Quantity;
            DayChangePercentage = (priceChange / previousPrice) * 100;
        }

        LastPriceUpdateAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add to position (buying more)
    /// </summary>
    public void AddToPosition(decimal quantity, decimal price)
    {
        var additionalCost = quantity * price;
        TotalCost += additionalCost;
        Quantity += quantity;
        
        // Recalculate average cost
        if (Quantity != 0)
        {
            AverageCost = TotalCost / Quantity;
        }

        UpdateMarketPrice(price);
    }

    /// <summary>
    /// Reduce position (selling)
    /// </summary>
    public void ReducePosition(decimal quantity, decimal price)
    {
        if (quantity > Quantity)
        {
            throw new InvalidOperationException("Cannot reduce position by more than available quantity");
        }

        var soldValue = quantity * price;
        var soldCost = quantity * AverageCost;
        var profitLoss = soldValue - soldCost;

        RealizedProfitLoss += profitLoss;
        TotalCost -= soldCost;
        Quantity -= quantity;

        UpdateMarketPrice(price);
    }

    /// <summary>
    /// Check if position is closed (quantity is zero)
    /// </summary>
    public bool IsClosed() => Quantity == 0;

    /// <summary>
    /// Get total return (realized + unrealized P&L + dividends)
    /// </summary>
    public decimal GetTotalReturn()
    {
        return RealizedProfitLoss + UnrealizedProfitLoss + TotalDividends;
    }
}
