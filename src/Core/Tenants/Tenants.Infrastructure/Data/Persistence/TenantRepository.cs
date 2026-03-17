using Kernel.Domain.SeedWorks;
using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Tenants.Domain.Entities;
using Product.Template.Core.Tenants.Domain.Repositories;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;
using Product.Template.Kernel.Infrastructure.HostDb;

namespace Product.Template.Core.Tenants.Infrastructure.Data.Persistence;

public class TenantRepository : ITenantRepository
{
    private readonly HostDbContext _hostDbContext;

    public TenantRepository(HostDbContext hostDbContext)
    {
        _hostDbContext = hostDbContext;
    }

    public async Task<Tenant?> GetByTenantIdAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var config = await _hostDbContext.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);

        return config is null ? null : MapToTenant(config);
    }

    public async Task<Tenant?> GetByKeyAsync(string tenantKey, CancellationToken cancellationToken = default)
    {
        var config = await _hostDbContext.Tenants
            .FirstOrDefaultAsync(t => t.TenantKey == tenantKey, cancellationToken);

        return config is null ? null : MapToTenant(config);
    }

    public async Task<PaginatedListOutput<Tenant>> ListAllAsync(ListInput listInput, CancellationToken cancellationToken = default)
    {
        var query = _hostDbContext.Tenants.AsQueryable();
        var totalCount = await query.CountAsync(cancellationToken);

        var configs = await query
            .OrderBy(t => t.TenantId)
            .Skip((listInput.PageNumber - 1) * listInput.PageSize)
            .Take(listInput.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedListOutput<Tenant>(
            PageNumber: listInput.PageNumber,
            PageSize: listInput.PageSize,
            TotalCount: totalCount,
            Data: configs.Select(MapToTenant).ToList());
    }

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        var config = MapToConfig(tenant);
        await _hostDbContext.Tenants.AddAsync(config, cancellationToken);
        await _hostDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        var config = await _hostDbContext.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenant.TenantId, cancellationToken);

        if (config is null)
            return;

        config.DisplayName = tenant.DisplayName;
        config.ContactEmail = tenant.ContactEmail;
        config.IsActive = tenant.IsActive;

        await _hostDbContext.SaveChangesAsync(cancellationToken);
    }

    private static Tenant MapToTenant(TenantConfig config) =>
        Tenant.Reconstitute(
            config.TenantId,
            config.TenantKey,
            config.DisplayName,
            config.ContactEmail,
            config.IsActive,
            config.IsolationMode,
            config.CreatedAt);

    private static TenantConfig MapToConfig(Tenant tenant) =>
        new TenantConfig
        {
            TenantId = tenant.TenantId,
            TenantKey = tenant.TenantKey,
            DisplayName = tenant.DisplayName,
            ContactEmail = tenant.ContactEmail,
            IsActive = tenant.IsActive,
            IsolationMode = tenant.IsolationMode,
            CreatedAt = tenant.CreatedAt
        };
}
