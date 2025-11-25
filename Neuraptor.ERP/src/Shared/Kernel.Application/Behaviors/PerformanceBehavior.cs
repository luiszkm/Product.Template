using Neuraptor.ERP.Kernel.Application.Messaging.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Neuraptor.ERP.Kernel.Application.Behaviors;

public class PerformanceBehavior : ICommandBehavior, IQueryBehavior
{
    private readonly ILogger<PerformanceBehavior> _logger;
    private readonly int _slowRequestThresholdMs;

    public PerformanceBehavior(ILogger<PerformanceBehavior> logger, int slowRequestThresholdMs = 500)
    {
        _logger = logger;
        _slowRequestThresholdMs = slowRequestThresholdMs;
    }

    // -------------------------------
    // COMMAND (sem retorno)
    // -------------------------------
    public async Task Handle<TCommand>(TCommand command, Func<Task> next, CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        await HandleCommandInternal(command, next);
    }

    private async Task HandleCommandInternal<TCommand>(TCommand command, Func<Task> next)
        where TCommand : ICommand
    {
        var stopwatch = Stopwatch.StartNew();

        await next();

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > _slowRequestThresholdMs)
        {
            var commandName = typeof(TCommand).Name;
            _logger.LogWarning(
                "Slow Command: {CommandName} took {ElapsedMilliseconds}ms to complete. Command: {@Command}",
                commandName, stopwatch.ElapsedMilliseconds, command);
        }
    }

    // -------------------------------
    // COMMAND (com retorno)
    // -------------------------------
    async Task<TResponse> ICommandBehavior.Handle<TCommand, TResponse>(
        TCommand command,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        return await HandleCommandInternal(command, next);
    }

    private async Task<TResponse> HandleCommandInternal<TCommand, TResponse>(
        TCommand command,
        Func<Task<TResponse>> next)
        where TCommand : ICommand<TResponse>
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > _slowRequestThresholdMs)
        {
            var commandName = typeof(TCommand).Name;
            _logger.LogWarning(
                "Slow Command: {CommandName} took {ElapsedMilliseconds}ms to complete. Command: {@Command}",
                commandName, stopwatch.ElapsedMilliseconds, command);
        }

        return response;
    }

    // -------------------------------
    // QUERY (com retorno)
    // -------------------------------
    async Task<TResponse> IQueryBehavior.Handle<TQuery, TResponse>(
        TQuery query,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        return await HandleQueryInternal(query, next);
    }

    private async Task<TResponse> HandleQueryInternal<TQuery, TResponse>(
        TQuery query,
        Func<Task<TResponse>> next)
        where TQuery : IQuery<TResponse>
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > _slowRequestThresholdMs)
        {
            var queryName = typeof(TQuery).Name;
            _logger.LogWarning(
                "Slow Query: {QueryName} took {ElapsedMilliseconds}ms to complete. Query: {@Query}",
                queryName, stopwatch.ElapsedMilliseconds, query);
        }

        return response;
    }
}
