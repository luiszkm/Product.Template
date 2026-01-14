using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Product.Template.Kernel.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting request {RequestName} {@Request}", requestName, request);

        try
        {
            var response = await next();
            _logger.LogInformation("Completed request {RequestName}", requestName);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request {RequestName} failed", requestName);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("Request {RequestName} executed in {ElapsedMilliseconds}ms", 
                requestName, stopwatch.ElapsedMilliseconds);
        }
    }
}

