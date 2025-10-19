using Polly;
using Polly.Extensions.Http;

namespace Product.Template.Api.Configurations;

public static class ResilienceConfiguration
{
    public static IServiceCollection AddResiliencePolicies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var retryCount = configuration.GetValue<int>("ResiliencePolicies:RetryCount", 3);
        var circuitBreakerThreshold = configuration.GetValue<int>("ResiliencePolicies:CircuitBreakerThreshold", 5);
        var circuitBreakerDurationSeconds = configuration.GetValue<int>("ResiliencePolicies:CircuitBreakerDurationSeconds", 30);
        var timeoutSeconds = configuration.GetValue<int>("ResiliencePolicies:TimeoutSeconds", 30);

        // Configuração de HttpClient com Polly para serviços externos
        services.AddHttpClient("ResilientHttpClient")
            .AddPolicyHandler(GetRetryPolicy(retryCount))
            .AddPolicyHandler(GetCircuitBreakerPolicy(circuitBreakerThreshold, circuitBreakerDurationSeconds))
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(timeoutSeconds));

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // Log retry attempts (pode ser expandido para usar ILogger via context)
                    Console.WriteLine($"Delaying for {timespan.TotalSeconds}s, then making retry {retryAttempt}");
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
        int threshold,
        int durationSeconds)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: threshold,
                durationOfBreak: TimeSpan.FromSeconds(durationSeconds),
                onBreak: (outcome, duration) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker closed");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("Circuit breaker half-open, testing connection");
                });
    }
}
