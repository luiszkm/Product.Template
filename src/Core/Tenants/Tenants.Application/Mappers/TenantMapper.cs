using Product.Template.Core.Tenants.Application.Outputs;
using Product.Template.Core.Tenants.Domain.Entities;

namespace Product.Template.Core.Tenants.Application.Mappers;

public static class TenantMapper
{
    public static TenantOutput ToOutput(this Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        return new TenantOutput(
            tenant.TenantId,
            tenant.TenantKey,
            tenant.DisplayName,
            tenant.ContactEmail,
            tenant.IsActive,
            tenant.IsolationMode,
            tenant.CreatedAt);
    }

    public static IEnumerable<TenantOutput> ToOutputList(this IEnumerable<Tenant> tenants)
    {
        ArgumentNullException.ThrowIfNull(tenants);
        return tenants.Select(t => t.ToOutput());
    }
}
