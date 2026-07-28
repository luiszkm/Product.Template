using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.HostDb;
using Product.Template.Kernel.Infrastructure.MultiTenancy;

namespace UnitTests.MultiTenancy;

public class CachedTenantStoreTests
{
    private static HostDbContext CreateHostDbContext()
    {
        var options = new DbContextOptionsBuilder<HostDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new HostDbContext(options);
    }

    private static CachedTenantStore CreateStore(HostDbContext context, IMemoryCache cache) =>
        new(context, cache, NullLogger<CachedTenantStore>.Instance);

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnNull_WhenTenantDoesNotExist()
    {
        using var context = CreateHostDbContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateStore(context, cache);

        var result = await sut.GetByKeyAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnTenant_AndCacheIt()
    {
        using var context = CreateHostDbContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var tenant = new TenantConfig { TenantId = Guid.NewGuid(), TenantKey = "acme", IsolationMode = TenantIsolationMode.SharedDb };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        var sut = CreateStore(context, cache);

        var result = await sut.GetByKeyAsync("acme");

        Assert.NotNull(result);
        Assert.Equal(tenant.TenantId, result!.TenantId);
        Assert.True(cache.TryGetValue("tenant:acme", out TenantConfig? _));
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnCachedValue_WithoutHittingDatabase()
    {
        using var context = CreateHostDbContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new TenantConfig { TenantId = Guid.NewGuid(), TenantKey = "cached", IsolationMode = TenantIsolationMode.SharedDb };
        cache.Set("tenant:cached", cached);
        var sut = CreateStore(context, cache);

        var result = await sut.GetByKeyAsync("cached");

        Assert.Same(cached, result);
    }

    [Fact]
    public async Task ListActiveAsync_ShouldReturnOnlyActiveTenants()
    {
        using var context = CreateHostDbContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        context.Tenants.AddRange(
            new TenantConfig { TenantId = Guid.NewGuid(), TenantKey = "active", IsolationMode = TenantIsolationMode.SharedDb, IsActive = true },
            new TenantConfig { TenantId = Guid.NewGuid(), TenantKey = "inactive", IsolationMode = TenantIsolationMode.SharedDb, IsActive = false });
        await context.SaveChangesAsync();
        var sut = CreateStore(context, cache);

        var result = await sut.ListActiveAsync();

        Assert.Single(result);
        Assert.Equal("active", result[0].TenantKey);
    }

    [Fact]
    public async Task UpsertAsync_ShouldInsertNewTenant_AndEvictCache()
    {
        using var context = CreateHostDbContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("tenant:new-tenant", new TenantConfig());
        var sut = CreateStore(context, cache);
        var tenant = new TenantConfig { TenantId = Guid.NewGuid(), TenantKey = "new-tenant", IsolationMode = TenantIsolationMode.SharedDb };

        await sut.UpsertAsync(tenant);

        Assert.Equal(1, await context.Tenants.CountAsync());
        Assert.False(cache.TryGetValue("tenant:new-tenant", out TenantConfig? _));
    }

    [Fact]
    public async Task UpsertAsync_ShouldUpdateExistingTenant_WhenTenantIdAlreadyExists()
    {
        using var context = CreateHostDbContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var tenantId = Guid.NewGuid();
        context.Tenants.Add(new TenantConfig { TenantId = tenantId, TenantKey = "acme", IsolationMode = TenantIsolationMode.SharedDb, DisplayName = "Old" });
        await context.SaveChangesAsync();
        var sut = CreateStore(context, cache);

        await sut.UpsertAsync(new TenantConfig { TenantId = tenantId, TenantKey = "acme", IsolationMode = TenantIsolationMode.SharedDb, DisplayName = "New" });

        var updated = await context.Tenants.FirstAsync(x => x.TenantId == tenantId);
        Assert.Equal("New", updated.DisplayName);
        Assert.Equal(1, await context.Tenants.CountAsync());
    }
}
