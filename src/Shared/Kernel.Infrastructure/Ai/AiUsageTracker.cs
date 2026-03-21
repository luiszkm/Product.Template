using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Product.Template.Kernel.Application.Ai;

namespace Kernel.Infrastructure.Ai;

internal sealed class AiUsageTracker : IAiUsageTracker
{
    private readonly ILogger<AiUsageTracker> _logger;
    private static readonly Meter Meter = new("Product.Template.Ai", "1.0.0");
    private static readonly Counter<long> TokenCounter = Meter.CreateCounter<long>("ai.tokens.used");
    private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>("ai.requests.total");
    private static readonly Histogram<double> LatencyHistogram = Meter.CreateHistogram<double>("ai.request.duration_ms");

    public AiUsageTracker(ILogger<AiUsageTracker> logger)
    {
        _logger = logger;
    }

    public Task TrackAsync(AiUsageRecord record, CancellationToken cancellationToken = default)
    {
        var tags = new TagList
        {
            { "service", record.Service },
            { "provider", record.Provider },
            { "model", record.Model },
            { "module", record.Module },
            { "success", record.Success.ToString() }
        };

        RequestCounter.Add(1, tags);
        LatencyHistogram.Record(record.Latency.TotalMilliseconds, tags);

        if (record.TokensUsed.HasValue)
            TokenCounter.Add(record.TokensUsed.Value, tags);

        if (record.Success)
        {
            _logger.LogInformation(
                "AI usage: service={Service} provider={Provider} model={Model} module={Module} " +
                "operation={Operation} tenant={TenantId} tokens={Tokens} latency={LatencyMs}ms",
                record.Service, record.Provider, record.Model, record.Module,
                record.Operation, record.TenantId, record.TokensUsed, record.Latency.TotalMilliseconds);
        }
        else
        {
            _logger.LogWarning(
                "AI failure: service={Service} provider={Provider} model={Model} module={Module} " +
                "operation={Operation} tenant={TenantId} error={ErrorCode} latency={LatencyMs}ms",
                record.Service, record.Provider, record.Model, record.Module,
                record.Operation, record.TenantId, record.ErrorCode, record.Latency.TotalMilliseconds);
        }

        return Task.CompletedTask;
    }
}
