namespace Product.Template.Kernel.Domain.MultiTenancy;

public interface ITenantProvisioningService
{
    Task<TenantConfig> CreateTenantAsync(string tenantKey, TenantIsolationMode isolationMode, CancellationToken cancellationToken = default);
}
