using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Neuraptor.ERP.Api.Configurations;

public static class OpenTelemetryConfiguration
{
    public static IServiceCollection AddOpenTelemetryConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otelConfig = configuration.GetSection("OpenTelemetry");
        var serviceName = otelConfig["ServiceName"] ?? "Neuraptor.ERP.Api";
        var serviceVersion = otelConfig["ServiceVersion"] ?? "1.0.0";
        var enableTraces = otelConfig.GetValue<bool>("EnableTraces", true);
        var enableMetrics = otelConfig.GetValue<bool>("EnableMetrics", true);
        var enableConsoleExporter = otelConfig.GetValue<bool>("EnableConsoleExporter", false);
        var otlpEndpoint = otelConfig["OtlpEndpoint"];

        // Resource attributes (identificam o serviço)
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development",
                ["host.name"] = Environment.MachineName
            });

        services.AddOpenTelemetry()
            .ConfigureResource(rb => rb.AddService(serviceName, serviceVersion: serviceVersion))
            .WithTracing(tracingBuilder =>
            {
                if (!enableTraces) return;

                tracingBuilder
                    .SetResourceBuilder(resourceBuilder)
                    // Instrumentação automática de ASP.NET Core (requests HTTP)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, httpRequest) =>
                        {
                            activity.SetTag("http.request.client_ip", httpRequest.HttpContext.Connection.RemoteIpAddress?.ToString());
                        };
                        options.EnrichWithHttpResponse = (activity, httpResponse) =>
                        {
                            activity.SetTag("http.response.status_code", httpResponse.StatusCode);
                        };
                    })
                    // Instrumentação automática de HttpClient (chamadas externas)
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    // ActivitySource customizado para spans manuais
                    .AddSource("Neuraptor.ERP.Api");

                // Console Exporter (útil para desenvolvimento)
                if (enableConsoleExporter)
                {
                    tracingBuilder.AddConsoleExporter();
                }

                // OTLP Exporter (Jaeger, Tempo, Datadog, etc)
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracingBuilder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            })
            .WithMetrics(metricsBuilder =>
            {
                if (!enableMetrics) return;

                metricsBuilder
                    .SetResourceBuilder(resourceBuilder)
                    // Métricas de runtime (.NET - GC, threads, exceptions)
                    .AddRuntimeInstrumentation()
                    // Métricas de ASP.NET Core (requests, duração, etc)
                    .AddAspNetCoreInstrumentation()
                    // Métricas de HttpClient
                    .AddHttpClientInstrumentation();

                // Console Exporter (útil para desenvolvimento)
                if (enableConsoleExporter)
                {
                    metricsBuilder.AddConsoleExporter();
                }

                // OTLP Exporter (Prometheus, Grafana, etc)
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metricsBuilder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            });

        return services;
    }

    /// <summary>
    /// ActivitySource para criar custom spans/traces
    /// Use este ActivitySource nos seus serviços para criar spans customizados
    /// </summary>
    public static readonly ActivitySource ActivitySource = new("Neuraptor.ERP.Api");
}
