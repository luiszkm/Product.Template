namespace Product.Template.Kernel.Application.Messaging.Interfaces;

public interface ICommandBus
{
    Task Send<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand;
    Task<TResponse> Send<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>;
}

