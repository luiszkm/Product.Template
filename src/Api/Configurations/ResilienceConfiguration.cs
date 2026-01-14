using Microsoft.Extensions.Http.Resilience;
using Polly; 

namespace Product.Template.Api.Configurations;

public static class ResilienceConfiguration
{
    public static IServiceCollection AddResiliencePolicies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Captura as configurações (mantendo seus valores padrão)
        var retryCount = configuration.GetValue("ResiliencePolicies:RetryCount", 3);
        var circuitBreakerThreshold = configuration.GetValue("ResiliencePolicies:CircuitBreakerThreshold", 0.5); // 0.5 = 50% de falhas
        var circuitBreakerDurationSeconds = configuration.GetValue("ResiliencePolicies:CircuitBreakerDurationSeconds", 30);
        var timeoutSeconds = configuration.GetValue("ResiliencePolicies:TimeoutSeconds", 30);

        // 2. Configura o HttpClient com o Handler de Resiliência Moderno
        services.AddHttpClient("ResilientHttpClient")
            .AddResilienceHandler("custom-pipeline", builder =>
            {
                // A ordem importa! No Polly v8, a ordem é de "fora para dentro" da chamada.
                // Pipeline: Timeout Total -> Retry -> Circuit Breaker -> Timeout por Tentativa

                // A. Timeout Total da Requisição (Opcional, mas recomendado)
                builder.AddTimeout(TimeSpan.FromSeconds(timeoutSeconds));

                // B. Retry (Tentativas)
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = retryCount,
                    BackoffType = DelayBackoffType.Exponential, // Já substitui o Math.Pow
                    UseJitter = true, // Adiciona aleatoriedade para evitar "thundering herd"
                    Delay = TimeSpan.FromSeconds(2),
                    // Filtra quais erros devem disparar o retry (já inclui 5xx e 408 por padrão)
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(response => response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                });

                // C. Circuit Breaker (Disjuntor)
                builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    SamplingDuration = TimeSpan.FromSeconds(60), // Janela de tempo para analisar falhas
                    FailureRatio = 0.5, // Se 50% das requisições falharem, abre o circuito
                    MinimumThroughput = (int)circuitBreakerThreshold, // Conversão explícita de double para int
                    BreakDuration = TimeSpan.FromSeconds(circuitBreakerDurationSeconds),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(response => response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                });

                // D. Timeout por Tentativa (Opcional: impede que uma única tentativa trave tudo)
                builder.AddTimeout(TimeSpan.FromSeconds(10));
            });

        return services;
    }
}
