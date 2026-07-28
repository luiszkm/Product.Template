using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.HostDb;
using Product.Template.Kernel.Infrastructure.MultiTenancy;

namespace UnitTests.MultiTenancy;

public class TenantProvisioningServiceTests
{
    private sealed class FakeConnectionStringResolver : ITenantConnectionStringResolver
    {
        public string ResolveAppConnection(TenantConfig tenant) => "unused";
    }

    private static HostDbContext CreateHostDbContext()
    {
        var options = new DbContextOptionsBuilder<HostDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new HostDbContext(options);
    }

    [Fact]
    public async Task CreateTenantAsync_ShouldPersistTenant_ForSharedDbIsolation()
    {
        using var context = CreateHostDbContext();
        var store = new CachedTenantStore(context, new Microsoft.Extensions.Caching.Memory.MemoryCache(Microsoft.Extensions.Options.Options.Create(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions())), NullLogger<CachedTenantStore>.Instance);
        var sut = new TenantProvisioningService(context, store, new FakeConnectionStringResolver(), NullLogger<TenantProvisioningService>.Instance);

        var tenant = await sut.CreateTenantAsync("  Acme ", TenantIsolationMode.SharedDb);

        Assert.Equal("acme", tenant.TenantKey);
        Assert.Equal(TenantIsolationMode.SharedDb, tenant.IsolationMode);
        Assert.Null(tenant.SchemaName);
        Assert.Null(tenant.ConnectionString);
        Assert.True(tenant.IsActive);
        Assert.Equal(1, await context.Tenants.CountAsync());
    }

    [Fact]
    public async Task CreateTenantAsync_ShouldBuildDedicatedConnectionString_ForDedicatedDbIsolation()
    {
        using var context = CreateHostDbContext();
        var store = new CachedTenantStore(context, new Microsoft.Extensions.Caching.Memory.MemoryCache(Microsoft.Extensions.Options.Options.Create(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions())), NullLogger<CachedTenantStore>.Instance);
        var sut = new TenantProvisioningService(context, store, new FakeConnectionStringResolver(), NullLogger<TenantProvisioningService>.Instance);

        var tenant = await sut.CreateTenantAsync("Enterprise", TenantIsolationMode.DedicatedDb);

        Assert.Equal("enterprise", tenant.TenantKey);
        Assert.Null(tenant.SchemaName);
        Assert.Contains("enterprise_db", tenant.ConnectionString);
    }

    [Fact]
    public async Task CreateTenantAsync_ShouldNormalizeTenantKey_ToLowerInvariantAndTrimmed()
    {
        using var context = CreateHostDbContext();
        var store = new CachedTenantStore(context, new Microsoft.Extensions.Caching.Memory.MemoryCache(Microsoft.Extensions.Options.Options.Create(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions())), NullLogger<CachedTenantStore>.Instance);
        var sut = new TenantProvisioningService(context, store, new FakeConnectionStringResolver(), NullLogger<TenantProvisioningService>.Instance);

        var tenant = await sut.CreateTenantAsync("  MixedCase  ", TenantIsolationMode.SharedDb);

        Assert.Equal("mixedcase", tenant.TenantKey);
    }
}
