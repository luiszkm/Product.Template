namespace Kernel.Infrastructure.Configurations;

public sealed class AiOptions
{
    public string DefaultModel { get; init; } = "gpt-4o-mini";
    public int MaxTokens { get; init; } = 4096;
    public float Temperature { get; init; } = 0.2f;
    public AzureOpenAiOptions AzureOpenAI { get; init; } = new();
    public AzureCognitiveOptions AzureCognitive { get; init; } = new();
}

public sealed class AzureOpenAiOptions
{
    public string Endpoint { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ChatDeploymentName { get; init; } = "gpt-4o-mini";
    public string EmbeddingDeploymentName { get; init; } = "text-embedding-3-small";
}

public sealed class AzureCognitiveOptions
{
    public string DocumentIntelligenceEndpoint { get; init; } = string.Empty;
    public string DocumentIntelligenceApiKey { get; init; } = string.Empty;
    public string SpeechServiceKey { get; init; } = string.Empty;
    public string SpeechServiceRegion { get; init; } = string.Empty;
}
