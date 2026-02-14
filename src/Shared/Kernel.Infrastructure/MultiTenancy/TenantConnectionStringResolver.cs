using Microsoft.Extensions.Configuration;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Kernel.Infrastructure.MultiTenancy;

public class TenantConnectionStringResolver(IConfiguration configuration) : ITenantConnectionStringResolver
{
    public string ResolveAppConnection(TenantConfig tenant)
    {
        if (tenant.IsolationMode == TenantIsolationMode.DedicatedDb)
        {
            return tenant.ConnectionString
                   ?? throw new InvalidOperationException($"Tenant '{tenant.TenantKey}' requires dedicated connection string.");
        }

        return configuration.GetConnectionString("AppDb")
               ?? throw new InvalidOperationException("ConnectionStrings:AppDb is required.");
    }
}
