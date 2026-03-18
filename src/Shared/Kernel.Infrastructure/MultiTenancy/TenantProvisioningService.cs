using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.HostDb;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Kernel.Infrastructure.MultiTenancy;

public class TenantProvisioningService(
    HostDbContext hostDbContext,
    ITenantStore tenantStore,
    ITenantConnectionStringResolver connectionStringResolver,
    ILogger<TenantProvisioningService> logger) : ITenantProvisioningService
{
    public async Task<TenantConfig> CreateTenantAsync(string tenantKey, TenantIsolationMode isolationMode, CancellationToken cancellationToken = default)
    {
        var normalized = tenantKey.Trim().ToLowerInvariant();
        var nextId = await hostDbContext.Tenants.AnyAsync(cancellationToken)
            ? await hostDbContext.Tenants.MaxAsync(x => x.TenantId, cancellationToken) + 1
            : 1;

        var tenant = new TenantConfig
        {
            TenantId = nextId,
            TenantKey = normalized,
            IsolationMode = isolationMode,
            SchemaName = isolationMode == TenantIsolationMode.SchemaPerTenant ? $"tenant_{normalized}" : null,
            ConnectionString = isolationMode == TenantIsolationMode.DedicatedDb
                ? $"Server=localhost,1433;Database={normalized}_db;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=True;Encrypt=False"
                : null,
            IsActive = true
        };

        await tenantStore.UpsertAsync(tenant, cancellationToken);

        if (isolationMode == TenantIsolationMode.SchemaPerTenant)
        {
            var sharedConn = connectionStringResolver.ResolveAppConnection(new TenantConfig { IsolationMode = TenantIsolationMode.SharedDb });
            await using var connection = new SqlConnection(sharedConn);
            await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""
                IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{tenant.SchemaName}')
                    EXEC('CREATE SCHEMA [{tenant.SchemaName}]');
                """;
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            logger.LogInformation("Schema {SchemaName} created for tenant {TenantKey}.", tenant.SchemaName, tenant.TenantKey);
        }

        logger.LogInformation("Tenant {TenantKey} provisioned using {IsolationMode}.", tenant.TenantKey, tenant.IsolationMode);
        return tenant;
    }
}
