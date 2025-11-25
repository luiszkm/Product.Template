using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Neuraptor.ERP.Api.HealthChecks;
using Neuraptor.ERP.Kernel.Infrastructure.Persistence;

namespace Neuraptor.ERP.Api.Configurations;

public static class HealthCheckConfiguration
{
    public static IServiceCollection AddHealthChecksConfiguration(
        this IServiceCollection services)
    {
        services.AddHealthChecks()
            // Database Health Check
            .AddDbContextCheck<AppDbContext>(
                name: "database",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "db", "sql" })

            // Custom Health Checks
            .AddCheck<DatabaseHealthCheck>(
                name: "database-custom",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "database" })

            // Memory Health Check
            .AddCheck("memory", () =>
            {
                var allocated = GC.GetTotalMemory(forceFullCollection: false);
                var threshold = 1024L * 1024L * 1024L; // 1GB

                return allocated < threshold
                    ? HealthCheckResult.Healthy($"Memory: {allocated / 1024 / 1024}MB")
                    : HealthCheckResult.Degraded($"Memory: {allocated / 1024 / 1024}MB - High usage");
            }, tags: new[] { "memory" })

            // Disk Space Health Check
            .AddCheck("disk-space", () =>
            {
                var drive = DriveInfo.GetDrives()
                    .FirstOrDefault(d => d.IsReady && d.DriveType == DriveType.Fixed);

                if (drive == null)
                    return HealthCheckResult.Unhealthy("No fixed drive found");

                var freeSpaceGB = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
                var totalSpaceGB = drive.TotalSize / 1024.0 / 1024.0 / 1024.0;
                var freeSpacePercent = (freeSpaceGB / totalSpaceGB) * 100;

                return freeSpacePercent > 10
                    ? HealthCheckResult.Healthy($"Disk: {freeSpaceGB:F2}GB free ({freeSpacePercent:F1}%)")
                    : HealthCheckResult.Degraded($"Disk: {freeSpaceGB:F2}GB free ({freeSpacePercent:F1}%) - Low disk space");
            }, tags: new[] { "disk" });

        // Health Checks UI
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(60); // Avalia a cada 60 segundos
            setup.MaximumHistoryEntriesPerEndpoint(50);
            setup.AddHealthCheckEndpoint("API Health", "/health");
        })
        .AddInMemoryStorage();

        return services;
    }

    public static WebApplication UseHealthChecksConfiguration(this WebApplication app)
    {
        // Endpoint básico de health check
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // Endpoint de readiness (apenas checks críticos)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("database"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Endpoint de liveness (apenas verificações básicas)
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false, // Sempre retorna 200 se a aplicação está rodando
            ResponseWriter = async (context, _) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = "Healthy",
                    timestamp = DateTime.UtcNow
                });
            }
        });

        // UI do Health Checks (disponível em /healthchecks-ui)
        app.MapHealthChecksUI(options =>
        {
            options.UIPath = "/healthchecks-ui";
            options.ApiPath = "/healthchecks-api";
        });

        return app;
    }
}
