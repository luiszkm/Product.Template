namespace Product.Template.Kernel.Application.Ai;

public interface IEmbeddingService
{
    Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
