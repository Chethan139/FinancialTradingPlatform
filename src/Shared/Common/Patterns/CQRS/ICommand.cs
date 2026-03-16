using MediatR;

namespace Common.Patterns.CQRS;

/// <summary>
/// Base interface for commands in CQRS pattern
/// Commands represent write operations that modify system state
/// Example: CreateOrderCommand, CancelOrderCommand, UpdatePortfolioCommand
/// </summary>
/// <typeparam name="TResponse">Type of response returned by command</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
    /// <summary>
    /// Unique identifier for this command instance
    /// Used for idempotency and de-duplication
    /// </summary>
    Guid CommandId { get; }

    /// <summary>
    /// Timestamp when command was created
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// User ID who initiated the command
    /// </summary>
    string InitiatedBy { get; }
}

/// <summary>
/// Base interface for commands that don't return a value
/// </summary>
public interface ICommand : ICommand<Unit>
{
}

/// <summary>
/// Base abstract class for commands with common properties
/// </summary>
public abstract class Command<TResponse> : ICommand<TResponse>
{
    public Guid CommandId { get; }
    public DateTime CreatedAt { get; }
    public string InitiatedBy { get; set; } = string.Empty;

    protected Command()
    {
        CommandId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Base abstract class for commands without return value
/// </summary>
public abstract class Command : Command<Unit>, ICommand
{
}
