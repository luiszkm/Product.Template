using MediatR;

namespace Product.Template.Kernel.Application.Messaging.Interfaces;

public interface IQuery { }
public interface IQuery<TResponse> : IRequest<TResponse> { }


