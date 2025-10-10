
namespace Kernel.Application.Messaging.Interfaces;

public interface ICommandBehavior
{
    Task Handle<TCommand>(TCommand command, Func<Task> next, CancellationToken cancellationToken) where TCommand : ICommand;
    Task<TResponse> Handle<TCommand, TResponse>(TCommand command, Func<Task<TResponse>> next, CancellationToken cancellationToken)
        where TCommand : ICommand<TResponse>;
}


