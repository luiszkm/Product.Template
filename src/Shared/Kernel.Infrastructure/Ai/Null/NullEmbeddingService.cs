using Product.Template.Kernel.Application.Ai;
using Product.Template.Kernel.Application.Exceptions;

namespace Kernel.Infrastructure.Ai.Null;

internal sealed class NullEmbeddingService : IEmbeddingService
{
    private const string Message = "AI capabilities are not enabled in this environment. Set FeatureFlags:EnableAI = true to enable.";

    public Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken = default)
        => throw new BusinessRuleException(Message);

    public Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
        => throw new BusinessRuleException(Message);
}
