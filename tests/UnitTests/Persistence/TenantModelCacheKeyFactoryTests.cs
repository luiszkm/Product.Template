using Microsoft.EntityFrameworkCore;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace UnitTests.Persistence;

public class TenantModelCacheKeyFactoryTests
{
    private sealed class FakeTenantContext(TenantConfig? tenant) : ITenantContext
    {
        public Guid? TenantId => tenant?.TenantId;
        public string? TenantKey => tenant?.TenantKey;
        public TenantConfig? Tenant => tenant;
        public bool IsResolved => tenant is not null;
        public void SetTenant(TenantConfig newTenant) { }
    }

    private static AppDbContext CreateContext(ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, tenantContext);
    }

    [Fact]
    public void Create_ShouldReturnDifferentKeys_ForDifferentSharedDbTenants()
    {
        var factory = new TenantModelCacheKeyFactory();
        var tenantA = new TenantConfig { TenantId = Guid.NewGuid(), TenantKey = "a", IsolationMode = TenantIsolationMode.SharedDb };
        var tenantB = new TenantConfig { TenantId = Guid.NewGuid(), TenantKey = "b", IsolationMode = TenantIsolationMode.SharedDb };

        var keyA = factory.Create(CreateContext(new FakeTenantContext(tenantA)), designTime: false);
        var keyB = factory.Create(CreateContext(new FakeTenantContext(tenantB)), designTime: false);

        Assert.NotEqual(keyA, keyB);
    }

    [Fact]
    public void Create_ShouldReturnSameKey_ForDedicatedDbTenants_RegardlessOfTenantId()
    {
        var factory = new TenantModelCacheKeyFactory();
        var tenantA = new TenantConfig { TenantId = Guid.NewGuid(), TenantKey = "a", IsolationMode = TenantIsolationMode.DedicatedDb };
        var tenantB = new TenantConfig { TenantId = Guid.NewGuid(), TenantKey = "b", IsolationMode = TenantIsolationMode.DedicatedDb };

        var keyA = factory.Create(CreateContext(new FakeTenantContext(tenantA)), designTime: false);
        var keyB = factory.Create(CreateContext(new FakeTenantContext(tenantB)), designTime: false);

        Assert.Equal(keyA, keyB);
    }

    [Fact]
    public void Create_ShouldFallBackToTypeAndDesignTime_ForNonAppDbContext()
    {
        var factory = new TenantModelCacheKeyFactory();
        var options = new DbContextOptionsBuilder<UnrelatedDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new UnrelatedDbContext(options);

        var key = factory.Create(context, designTime: true);

        Assert.Equal((typeof(UnrelatedDbContext), true), key);
    }

    private sealed class UnrelatedDbContext(DbContextOptions<UnrelatedDbContext> options) : DbContext(options);
}
