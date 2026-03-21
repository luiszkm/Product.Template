using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Product.Template.Api.HealthChecks;
using Product.Template.Kernel.Infrastructure.HostDb;
using Product.Template.Kernel.Infrastructure.Persistence;
namespace Product.Template.Api.Configurations;

public static class HealthCheckConfiguration
{
    public static IServiceCollection AddHealthChecksConfiguration(
        this IServiceCollection services)
    {
        services.AddHealthChecks()
            // AppDb — custom check handles latency threshold + structured data.
            // AddDbContextCheck<AppDbContext> removed: it duplicates the same CanConnectAsync
            // call that DatabaseHealthCheck already performs, wasting one DB round-trip per poll.
            .AddCheck<DatabaseHealthCheck>(
                name: "appdb",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "database", "ready"])

            // HostDb — critical for tenant resolution; a failure here silently breaks multi-tenancy.
            .AddDbContextCheck<HostDbContext>(
                name: "hostdb",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "database", "ready"])

            // Memory — threshold from config (default 1 GB)
            .AddCheck("memory", () =>
            {
                var allocated = GC.GetTotalMemory(forceFullCollection: false);
                var threshold = 1024L * 1024L * 1024L; // 1 GB — adjust via appsettings if needed

                var mb = allocated / 1024 / 1024;
                return allocated < threshold
                    ? HealthCheckResult.Healthy($"Memory: {mb} MB")
                    : HealthCheckResult.Degraded($"Memory: {mb} MB — high usage");
            }, tags: ["system"])

            // Disk space
            .AddCheck("disk-space", () =>
            {
                var drive = DriveInfo.GetDrives()
                    .FirstOrDefault(d => d.IsReady && d.DriveType == DriveType.Fixed);

                if (drive == null)
                    return HealthCheckResult.Degraded("No fixed drive found (container?)");

                var freeSpaceGB  = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
                var totalSpaceGB = drive.TotalSize         / 1024.0 / 1024.0 / 1024.0;
                var freePercent  = totalSpaceGB > 0 ? (freeSpaceGB / totalSpaceGB) * 100 : 100;

                return freePercent > 10
                    ? HealthCheckResult.Healthy($"Disk: {freeSpaceGB:F2} GB free ({freePercent:F1}%)")
                    : HealthCheckResult.Degraded($"Disk: {freeSpaceGB:F2} GB free ({freePercent:F1}%) — low");
            }, tags: ["system"]);

        // Health Checks UI (only useful in development — disable in production via env)
        // TODO: re-enable when Xabaril releases an AspNetCore.HealthChecks.UI version
        //       compatible with .NET 10 (currently blocked by IdentityModel 5.2.0 dep).
        //       Track: https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks
        // services.AddHealthChecksUI(...).AddInMemoryStorage();

        return services;
    }

    public static WebApplication UseHealthChecksConfiguration(this WebApplication app)
    {
        var isDev = app.Environment.IsDevelopment();

        // /health/live — liveness probe (Docker HEALTHCHECK, k8s livenessProbe).
        // Predicate = _ => false → zero checks run; always 200 as long as the process is alive.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = async (context, _) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status    = "Healthy",
                    timestamp = DateTime.UtcNow
                });
            }
        });

        // /health/ready — readiness probe (k8s readinessProbe): only DB checks.
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate    = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // /health — full check; restricted to Development to avoid information disclosure.
        // In production, protect this endpoint behind [Authorize] or an IP allowlist.
        var fullHealthEndpoint = app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate  = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy]   = StatusCodes.Status200OK,
                [HealthStatus.Degraded]  = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        if (!isDev)
            fullHealthEndpoint.RequireAuthorization(SecurityConfiguration.AdminOnlyPolicy);

        // /healthchecks-ui — disabled: AspNetCore.HealthChecks.UI 9.0.0 requires IdentityModel 5.2.0
        // which is incompatible with .NET 10. Re-enable when a compatible version is released.
        // if (isDev) { app.MapHealthChecksUI(options => { options.UIPath = "/healthchecks-ui"; ... }); }

        return app;
    }
}
