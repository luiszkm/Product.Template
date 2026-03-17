using Kernel.Domain.SeedWorks;
using Product.Template.Core.Tenants.Domain.Entities;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Tenants.Domain.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByTenantIdAsync(long tenantId, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByKeyAsync(string tenantKey, CancellationToken cancellationToken = default);
    Task<PaginatedListOutput<Tenant>> ListAllAsync(ListInput listInput, CancellationToken cancellationToken = default);
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);
}
