namespace Neuraptor.ERP.Kernel.Application.Messaging.Interfaces
{
    public interface IQueryBehavior
    {
        Task<TResponse> Handle<TQuery, TResponse>(TQuery query, Func<Task<TResponse>> next, CancellationToken cancellationToken)
             where TQuery : IQuery<TResponse>;
    }
}

