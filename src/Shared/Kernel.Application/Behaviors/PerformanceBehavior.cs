using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Product.Template.Kernel.Application.Behaviors;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly int _slowRequestThresholdMs;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger, int slowRequestThresholdMs = 500)
    {
        _logger = logger;
        _slowRequestThresholdMs = slowRequestThresholdMs;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > _slowRequestThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogWarning(
                "Slow Request: {RequestName} took {ElapsedMilliseconds}ms to complete. Request: {@Request}",
                requestName, stopwatch.ElapsedMilliseconds, request);
        }

        return response;
    }
}

