namespace Product.Template.Kernel.Domain.MultiTenancy;

public interface ITenantContext
{
    long? TenantId { get; }
    string? TenantKey { get; }
    TenantConfig? Tenant { get; }
    bool IsResolved { get; }
    void SetTenant(TenantConfig tenant);
}
