using Azure.AI.OpenAI;
using Kernel.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using Product.Template.Kernel.Application.Ai;

namespace Kernel.Infrastructure.Ai;

internal sealed class AzureOpenAiEmbeddingService : IEmbeddingService
{
    private readonly AzureOpenAIClient _azureClient;
    private readonly AiOptions _options;

    public AzureOpenAiEmbeddingService(AzureOpenAIClient azureClient, IOptions<AiOptions> options)
    {
        _azureClient = azureClient;
        _options = options.Value;
    }

    public async Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var client = _azureClient.GetEmbeddingClient(_options.AzureOpenAI.EmbeddingDeploymentName);
        var response = await client.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        var embedding = response.Value;
        var vector = embedding.ToFloats().ToArray();

        return new EmbeddingResult(
            Vector: vector,
            Dimensions: vector.Length,
            TokensUsed: 0,
            Model: _options.AzureOpenAI.EmbeddingDeploymentName
        );
    }

    public async Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        var client = _azureClient.GetEmbeddingClient(_options.AzureOpenAI.EmbeddingDeploymentName);
        var response = await client.GenerateEmbeddingsAsync(textList, cancellationToken: cancellationToken);

        return response.Value.Select(e =>
        {
            var vector = e.ToFloats().ToArray();
            return new EmbeddingResult(
                Vector: vector,
                Dimensions: vector.Length,
                TokensUsed: 0,
                Model: _options.AzureOpenAI.EmbeddingDeploymentName
            );
        }).ToList();
    }
}
