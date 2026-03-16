using MediatR;

namespace Common.Patterns.CQRS;

/// <summary>
/// Interface for command handlers
/// Implements the handler side of CQRS pattern
/// </summary>
/// <typeparam name="TCommand">Type of command to handle</typeparam>
/// <typeparam name="TResponse">Type of response</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// Interface for command handlers without return value
/// </summary>
/// <typeparam name="TCommand">Type of command to handle</typeparam>
public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Unit>
    where TCommand : ICommand<Unit>
{
}

/// <summary>
/// Interface for query handlers
/// Implements the handler side of CQRS pattern for queries
/// </summary>
/// <typeparam name="TQuery">Type of query to handle</typeparam>
/// <typeparam name="TResponse">Type of response</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
