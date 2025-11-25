using Neuraptor.ERP.Kernel.Application.Messaging.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Neuraptor.ERP.Kernel.Application.Behaviors;

public class LoggingBehavior : ICommandBehavior, IQueryBehavior
{
    private readonly ILogger<LoggingBehavior> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior> logger)
    {
        _logger = logger;
    }

    // -------------------------------
    // COMMAND (sem retorno)
    // -------------------------------
    public async Task Handle<TCommand>(
        TCommand command,
        Func<Task> next,
        CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        await HandleCommandInternal(command, next);
    }

    private async Task HandleCommandInternal<TCommand>(TCommand command, Func<Task> next)
        where TCommand : ICommand
    {
        var name = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting command {CommandName} {@Command}", name, command);

        try
        {
            await next();
            _logger.LogInformation("Completed command {CommandName}", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command {CommandName} failed", name);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("Command {CommandName} executed in {ElapsedMilliseconds}ms", name, stopwatch.ElapsedMilliseconds);
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
        var name = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting command {CommandName} {@Command}", name, command);

        try
        {
            var response = await next();
            _logger.LogInformation("Completed command {CommandName}", name);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command {CommandName} failed", name);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("Command {CommandName} executed in {ElapsedMilliseconds}ms", name, stopwatch.ElapsedMilliseconds);
        }
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
        var name = typeof(TQuery).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting query {QueryName} {@Query}", name, query);

        try
        {
            var response = await next();
            _logger.LogInformation("Completed query {QueryName}", name);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query {QueryName} failed", name);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("Query {QueryName} executed in {ElapsedMilliseconds}ms", name, stopwatch.ElapsedMilliseconds);
        }
    }
}
