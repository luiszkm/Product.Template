namespace Kernel.Application.Messaging.Interfaces;

// Interface marcadora para um comando.
public interface ICommand { }

// Interface marcadora para um comando que retorna um valor.
public interface ICommand<TResponse> { }
