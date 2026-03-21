namespace Product.Template.Kernel.Application.Ai;

public interface IAiUsageTracker
{
    Task TrackAsync(AiUsageRecord record, CancellationToken cancellationToken = default);
}
