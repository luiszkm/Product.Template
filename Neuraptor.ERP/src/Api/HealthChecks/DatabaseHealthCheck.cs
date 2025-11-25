using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Neuraptor.ERP.Kernel.Infrastructure.Persistence;

namespace Neuraptor.ERP.Api.HealthChecks;

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
            // Tenta abrir uma conexão com o banco de dados
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy(
                    "Unable to connect to database",
                    data: new Dictionary<string, object>
                    {
                        ["ConnectionString"] = MaskConnectionString(_dbContext.Database.GetConnectionString() ?? "")
                    });
            }

            // Executa um comando simples para verificar se a conexão está funcional
            var startTime = DateTime.UtcNow;
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var data = new Dictionary<string, object>
            {
                ["ResponseTime"] = $"{responseTime}ms",
                ["Database"] = _dbContext.Database.GetDbConnection().Database,
                ["Provider"] = _dbContext.Database.ProviderName ?? "Unknown"
            };

            // Se a resposta demorou mais de 1 segundo, marca como degradado
            if (responseTime > 1000)
            {
                return HealthCheckResult.Degraded(
                    $"Database is responding slowly ({responseTime}ms)",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Database is healthy (response time: {responseTime}ms)",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");

            return HealthCheckResult.Unhealthy(
                "Database health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["Error"] = ex.Message,
                    ["StackTrace"] = ex.StackTrace ?? "N/A"
                });
        }
    }

    private string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "[Empty]";

        // Mascara a senha na connection string
        var parts = connectionString.Split(';');
        var maskedParts = parts.Select(part =>
        {
            var keyValue = part.Split('=');
            if (keyValue.Length == 2)
            {
                var key = keyValue[0].Trim().ToLower();
                if (key == "password" || key == "pwd")
                {
                    return $"{keyValue[0]}=***";
                }
            }
            return part;
        });

        return string.Join(";", maskedParts);
    }
}
