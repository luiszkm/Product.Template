namespace Product.Template.Kernel.Domain.MultiTenancy;

public interface ITenantStore
{
    Task<TenantConfig?> GetByKeyAsync(string tenantKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantConfig>> ListActiveAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(TenantConfig tenantConfig, CancellationToken cancellationToken = default);
}
