using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Kernel.Infrastructure.MultiTenancy;

public interface ITenantConnectionStringResolver
{
    string ResolveAppConnection(TenantConfig tenant);
}
