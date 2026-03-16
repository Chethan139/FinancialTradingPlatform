namespace Common.Models.Domain;

/// <summary>
/// Base entity class for all domain entities
/// Implements common properties like Id, timestamps, and soft delete functionality
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Timestamp when the entity was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User ID who created the entity
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Timestamp when the entity was last updated (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User ID who last updated the entity
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Soft delete flag - entity is logically deleted but physically retained
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Timestamp when entity was soft deleted (UTC)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Row version for optimistic concurrency control
    /// Prevents race conditions in distributed systems
    /// </summary>
    public byte[]? RowVersion { get; set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }
}
