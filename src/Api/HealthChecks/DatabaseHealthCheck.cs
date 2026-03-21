using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Api.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        AppDbContext dbContext,
        ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            sw.Stop();

            var responseTime = sw.ElapsedMilliseconds;

            var data = new Dictionary<string, object>
            {
                ["ResponseTimeMs"] = responseTime,
                ["Database"]       = _dbContext.Database.GetDbConnection().Database,
                ["Provider"]       = _dbContext.Database.ProviderName ?? "Unknown"
            };

            return responseTime > 1000
                ? HealthCheckResult.Degraded($"Database responding slowly ({responseTime} ms)", data: data)
                : HealthCheckResult.Healthy($"Database healthy ({responseTime} ms)", data: data);
        }
        catch (Exception ex)
        {
            // Log internally — never expose stack trace or connection details in the response
            _logger.LogError(ex, "Database health check failed");

            return HealthCheckResult.Unhealthy(
                "Database health check failed — see application logs for details",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["ErrorType"] = ex.GetType().Name
                });
        }
    }
}
