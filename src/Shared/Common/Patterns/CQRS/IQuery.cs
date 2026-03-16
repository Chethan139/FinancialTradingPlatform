using MediatR;

namespace Common.Patterns.CQRS;

/// <summary>
/// Base interface for queries in CQRS pattern
/// Queries represent read operations that don't modify system state
/// Example: GetPortfolioQuery, GetOrderHistoryQuery, GetMarketDataQuery
/// 
/// Key CQRS Benefits:
/// - Separation of read and write models
/// - Independent scaling of reads vs writes
/// - Optimized read models for specific use cases
/// - Event sourcing for complete audit trail
/// </summary>
/// <typeparam name="TResponse">Type of data returned by query</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
    /// <summary>
    /// Unique identifier for this query instance
    /// Used for logging and tracing
    /// </summary>
    Guid QueryId { get; }

    /// <summary>
    /// Timestamp when query was created
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// User ID who initiated the query
    /// </summary>
    string RequestedBy { get; }
}

/// <summary>
/// Base abstract class for queries with common properties
/// </summary>
public abstract class Query<TResponse> : IQuery<TResponse>
{
    public Guid QueryId { get; }
    public DateTime CreatedAt { get; }
    public string RequestedBy { get; set; } = string.Empty;

    protected Query()
    {
        QueryId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}
