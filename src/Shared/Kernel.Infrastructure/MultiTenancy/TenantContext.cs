using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Kernel.Infrastructure.MultiTenancy;

public class TenantContext : ITenantContext
{
    public long? TenantId => Tenant?.TenantId;
    public string? TenantKey => Tenant?.TenantKey;
    public TenantConfig? Tenant { get; private set; }
    public bool IsResolved => Tenant is not null;

    public void SetTenant(TenantConfig tenant)
    {
        Tenant = tenant;
    }
}
