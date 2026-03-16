namespace Common.Models.Domain;

/// <summary>
/// Represents a financial transaction in a portfolio
/// Immutable record of all financial activities for audit trail
/// </summary>
public class Transaction : BaseEntity
{
    /// <summary>
    /// Portfolio ID
    /// </summary>
    public Guid PortfolioId { get; set; }

    /// <summary>
    /// Related order ID (if transaction is from order execution)
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Transaction type (Buy, Sell, Deposit, etc.)
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Trading symbol (null for cash transactions)
    /// </summary>
    public string? Symbol { get; set; }

    /// <summary>
    /// Quantity (for securities transactions)
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Price per unit (for securities transactions)
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Transaction amount (positive for credits, negative for debits)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Commission/fees charged
    /// </summary>
    public decimal Commission { get; set; }

    /// <summary>
    /// Net amount (amount - commission)
    /// </summary>
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Cash balance after transaction
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Transaction description/notes
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Transaction reference number
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Settlement date (T+2 for stocks, etc.)
    /// </summary>
    public DateTime? SettlementDate { get; set; }

    /// <summary>
    /// Whether transaction is settled
    /// </summary>
    public bool IsSettled { get; set; }

    /// <summary>
    /// Portfolio reference
    /// </summary>
    public Portfolio? Portfolio { get; set; }

    /// <summary>
    /// Order reference
    /// </summary>
    public Order? Order { get; set; }
}
