using FluentValidation;
using Kernel.Application.Messaging.Interfaces;

namespace Kernel.Application.Behaviors;

public class ValidationBehavior : ICommandBehavior, IQueryBehavior
{
    private readonly IEnumerable<IValidator<object>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<object>> validators)
    {
        // Cast seguro para IEnumerable<IValidator<T>>
        _validators = validators;
    }

    // -------------------------------
    // COMMAND (sem retorno)
    // -------------------------------
    public async Task Handle<TCommand>(TCommand command, Func<Task> next, CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        await HandleCommandInternal(command, next, cancellationToken);
    }

    private async Task HandleCommandInternal<TCommand>(
        TCommand command,
        Func<Task> next,
        CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        await ValidateAsync(command, cancellationToken);
        await next();
    }

    // -------------------------------
    // COMMAND (com retorno)
    // -------------------------------
    async Task<TResponse> ICommandBehavior.Handle<TCommand, TResponse>(
        TCommand command,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        return await HandleCommandInternal(command, next, cancellationToken);
    }

    private async Task<TResponse> HandleCommandInternal<TCommand, TResponse>(
        TCommand command,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
        where TCommand : ICommand<TResponse>
    {
        await ValidateAsync(command, cancellationToken);
        return await next();
    }

    // -------------------------------
    // QUERY (com retorno)
    // -------------------------------
    async Task<TResponse> IQueryBehavior.Handle<TQuery, TResponse>(
        TQuery query,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        return await HandleQueryInternal(query, next, cancellationToken);
    }

    private async Task<TResponse> HandleQueryInternal<TQuery, TResponse>(
        TQuery query,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
        where TQuery : IQuery<TResponse>
    {
        await ValidateAsync(query, cancellationToken);
        return await next();
    }

    // -------------------------------
    // MÉTODO PRIVADO DE VALIDAÇÃO
    // -------------------------------
    private async Task ValidateAsync<T>(T message, CancellationToken cancellationToken)
    {
        // Filtra apenas validadores compatíveis com T
        var validators = _validators
            .OfType<IValidator<T>>()
            .ToList();

        if (!validators.Any())
            return;

        var context = new ValidationContext<T>(message);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);
    }
}
