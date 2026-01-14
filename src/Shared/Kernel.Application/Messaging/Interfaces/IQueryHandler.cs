using MediatR;

namespace Product.Template.Kernel.Application.Messaging.Interfaces;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
{
}


