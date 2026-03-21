using Product.Template.Kernel.Application.Ai;

namespace Kernel.Infrastructure.Ai.Null;

internal sealed class NullAiUsageTracker : IAiUsageTracker
{
    public Task TrackAsync(AiUsageRecord record, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
