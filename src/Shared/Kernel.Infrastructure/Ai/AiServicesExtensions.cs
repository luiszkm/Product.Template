using Azure;
using Azure.AI.OpenAI;
using Kernel.Infrastructure.Configurations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Product.Template.Kernel.Application.Ai;

namespace Kernel.Infrastructure.Ai;

public static class AiServicesExtensions
{
    public static IServiceCollection AddNullAiServices(this IServiceCollection services)
    {
        services.AddSingleton<ILlmService, Null.NullLlmService>();
        services.AddSingleton<IEmbeddingService, Null.NullEmbeddingService>();
        services.AddSingleton<IOcrService, Null.NullOcrService>();
        services.AddSingleton<ITextToSpeechService, Null.NullTextToSpeechService>();
        services.AddSingleton<ISpeechToTextService, Null.NullSpeechToTextService>();
        services.AddSingleton<IAiUsageTracker, Null.NullAiUsageTracker>();
        return services;
    }

    public static IServiceCollection AddAiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection("AI"));
        var opts = configuration.GetSection("AI").Get<AiOptions>() ?? new AiOptions();

        var azureOpts = opts.AzureOpenAI;
        services.AddSingleton(_ => new AzureOpenAIClient(
            new Uri(azureOpts.Endpoint),
            new AzureKeyCredential(azureOpts.ApiKey)));

        services.AddScoped<ILlmService, AzureOpenAiLlmService>();
        services.AddScoped<AzureOpenAiEmbeddingService>();
        services.AddScoped<IEmbeddingService>(sp =>
            new CachedEmbeddingService(
                sp.GetRequiredService<AzureOpenAiEmbeddingService>(),
                sp.GetRequiredService<IDistributedCache>()));

        services.AddScoped<IOcrService, AzureOcrService>();
        services.AddScoped<ITextToSpeechService, AzureTextToSpeechService>();
        services.AddScoped<ISpeechToTextService, AzureSpeechToTextService>();

        services.AddSingleton<IAiUsageTracker, AiUsageTracker>();

        return services;
    }
}
