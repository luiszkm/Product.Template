using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

namespace Neuraptor.ERP.Api.Configurations;

public static class SerilogConfiguration
{
    public static WebApplicationBuilder AddSerilogConfiguration(this WebApplicationBuilder builder)
    {
        // Configurar Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "Neuraptor.ERP.Api")
            .Enrich.WithExceptionDetails()
            .CreateLogger();

        // Usar Serilog como provedor de logging
        builder.Host.UseSerilog();

        return builder;
    }

    public static WebApplication UseSerilogConfiguration(this WebApplication app)
    {
        // Request logging do Serilog (mais performático que middleware customizado)
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
            options.GetLevel = (httpContext, elapsed, ex) => ex != null
                ? LogEventLevel.Error
                : httpContext.Response.StatusCode > 499
                    ? LogEventLevel.Error
                    : LogEventLevel.Information;

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
                diagnosticContext.Set("CorrelationId", httpContext.Response.Headers["X-Correlation-ID"].ToString());
            };
        });

        return app;
    }
}
