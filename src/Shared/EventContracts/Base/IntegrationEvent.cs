namespace EventContracts.Base;

/// <summary>
/// Base class for all integration events in the system
/// Used for communication between microservices via Azure Service Bus and Event Hubs
/// 
/// Event-Driven Architecture Benefits:
/// - Loose coupling between services
/// - Asynchronous communication for better scalability
/// - Event sourcing for complete audit trail
/// - Temporal decoupling (services don't need to be online simultaneously)
/// 
/// Interview Key Points:
/// - Integration Events vs Domain Events: Integration events cross bounded contexts
/// - Idempotency: EventId ensures messages can be safely reprocessed
/// - Metadata: Timestamps and correlation IDs for distributed tracing
/// </summary>
public abstract class IntegrationEvent
{
    /// <summary>
    /// Unique identifier for this event instance
    /// Used for idempotency and deduplication
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Timestamp when event was created (UTC)
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Event version for schema evolution
    /// Allows backwards compatibility when event structure changes
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Correlation ID for distributed tracing
    /// Links related events across service boundaries
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// User ID who triggered the event
    /// </summary>
    public string? TriggeredBy { get; set; }

    /// <summary>
    /// Source service that published the event
    /// </summary>
    public string SourceService { get; set; } = string.Empty;

    /// <summary>
    /// Event type name (derived class name)
    /// Used for routing and filtering
    /// </summary>
    public string EventType => GetType().Name;

    protected IntegrationEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        Version = 1;
    }

    protected IntegrationEvent(Guid eventId, DateTime occurredAt)
    {
        EventId = eventId;
        OccurredAt = occurredAt;
        Version = 1;
    }
}
