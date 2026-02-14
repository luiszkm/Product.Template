using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Kernel.Infrastructure.MultiTenancy;

public class SearchPathConnectionInterceptor(
    ITenantContext tenantContext,
    ILogger<SearchPathConnectionInterceptor> logger) : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);

        var tenant = tenantContext.Tenant;
        if (tenant is null || tenant.IsolationMode != TenantIsolationMode.SchemaPerTenant || string.IsNullOrWhiteSpace(tenant.SchemaName))
        {
            return;
        }

        if (!IsNpgsql(connection))
        {
            logger.LogDebug("SchemaPerTenant active but provider is not PostgreSQL. search_path skipped.");
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"SET search_path TO \"{tenant.SchemaName}\", public;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool IsNpgsql(DbConnection connection)
        => connection.GetType().Name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
           || connection.GetType().Namespace?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
}
