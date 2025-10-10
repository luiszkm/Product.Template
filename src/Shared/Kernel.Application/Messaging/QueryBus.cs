using Product.Template.Kernel.Application.Messaging.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Product.Template.Kernel.Application.Messaging;

public class QueryBus : IQueryBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<IQueryBehavior> _behaviors;

    public QueryBus(IServiceProvider serviceProvider, IEnumerable<IQueryBehavior> behaviors)
    {
        _serviceProvider = serviceProvider;
        _behaviors = behaviors;
    }

    public async Task<TResponse> Send<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();

        async Task<TResponse> HandlerDelegate() => await handler.Handle(query, cancellationToken);

        var pipeline = _behaviors.Reverse().Aggregate(
            (Func<Task<TResponse>>)HandlerDelegate,
            (next, behavior) => () => behavior.Handle(query, next, cancellationToken));

        return await pipeline();
    }
}

