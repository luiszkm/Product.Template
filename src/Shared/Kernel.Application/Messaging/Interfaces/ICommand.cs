using MediatR;

namespace Product.Template.Kernel.Application.Messaging.Interfaces;

public interface ICommand : IRequest { }

public interface ICommand<TResponse> : IRequest<TResponse> { }


