using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.HostDb;
using Microsoft.EntityFrameworkCore;

namespace Product.Template.Kernel.Infrastructure.MultiTenancy;

public class CachedTenantStore(
    HostDbContext hostDbContext,
    IMemoryCache memoryCache,
    ILogger<CachedTenantStore> logger) : ITenantStore
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<TenantConfig?> GetByKeyAsync(string tenantKey, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(tenantKey);
        if (memoryCache.TryGetValue<TenantConfig>(cacheKey, out var cached))
        {
            return cached;
        }

        var tenant = await hostDbContext.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantKey == tenantKey, cancellationToken);

        if (tenant is not null)
        {
            memoryCache.Set(cacheKey, tenant, CacheTtl);
        }

        return tenant;
    }

    public async Task<IReadOnlyList<TenantConfig>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        return await hostDbContext.Tenants.AsNoTracking().Where(x => x.IsActive).ToListAsync(cancellationToken);
    }

    public async Task UpsertAsync(TenantConfig tenantConfig, CancellationToken cancellationToken = default)
    {
        var existing = await hostDbContext.Tenants.FirstOrDefaultAsync(x => x.TenantId == tenantConfig.TenantId, cancellationToken);
        if (existing is null)
        {
            hostDbContext.Tenants.Add(tenantConfig);
        }
        else
        {
            hostDbContext.Entry(existing).CurrentValues.SetValues(tenantConfig);
        }

        await hostDbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Tenant {TenantKey} saved on host store.", tenantConfig.TenantKey);
        memoryCache.Remove(BuildCacheKey(tenantConfig.TenantKey));
    }

    private static string BuildCacheKey(string tenantKey) => $"tenant:{tenantKey}";
}
