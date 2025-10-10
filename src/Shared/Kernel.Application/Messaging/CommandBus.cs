using Kernel.Application.Messaging.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Data;


namespace Kernel.Application.Messaging
{
    public class CommandBus : ICommandBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<ICommandBehavior> _behaviors;

        public CommandBus(IServiceProvider serviceProvider, IEnumerable<ICommandBehavior> behaviors)
        {
            _serviceProvider = serviceProvider;
            _behaviors = behaviors;
        }

        public async Task Send<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand
        {
            var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();

            async Task HandlerDelegate() => await handler.Handle(command, cancellationToken);

            var pipeline = _behaviors.Reverse().Aggregate(
                (Func<Task>)HandlerDelegate,
                (next, behavior) => () => behavior.Handle(command, next, cancellationToken));

            await pipeline();
        }

        public async Task<TResponse> Send<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResponse>
        {
            var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();

            async Task<TResponse> HandlerDelegate() => await handler.Handle(command, cancellationToken);

            var pipeline = _behaviors.Reverse().Aggregate(
                (Func<Task<TResponse>>)HandlerDelegate,
                (next, behavior) => () => behavior.Handle(command, next, cancellationToken));

            return await pipeline();
        }
    }
}
