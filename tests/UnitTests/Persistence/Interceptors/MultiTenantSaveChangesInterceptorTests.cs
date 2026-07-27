using Kernel.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace UnitTests.Persistence.Interceptors;

public class MultiTenantSaveChangesInterceptorTests
{
    private sealed class TestEntity : Entity, IMultiTenantEntity
    {
        public Guid TenantId { get; private set; }
        public void AssignTenant(Guid tenantId) => TenantId = tenantId;
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestEntity> Items => Set<TestEntity>();
    }

    private sealed class FakeTenantContext(TenantConfig? tenant) : ITenantContext
    {
        public Guid? TenantId => tenant?.TenantId;
        public string? TenantKey => tenant?.TenantKey;
        public TenantConfig? Tenant => tenant;
        public bool IsResolved => tenant is not null;
        public void SetTenant(TenantConfig newTenant) { }
    }

    private static TestDbContext CreateContext(ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new MultiTenantSaveChangesInterceptor(tenantContext))
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldStampTenantId_WhenSharedDbIsolation()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new TenantConfig { TenantId = tenantId, TenantKey = "acme", IsolationMode = TenantIsolationMode.SharedDb };
        using var context = CreateContext(new FakeTenantContext(tenant));

        var entity = new TestEntity();
        context.Items.Add(entity);
        await context.SaveChangesAsync();

        Assert.Equal(tenantId, entity.TenantId);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldNotStampTenantId_WhenDedicatedDbIsolation()
    {
        var tenant = new TenantConfig { TenantId = Guid.NewGuid(), TenantKey = "acme", IsolationMode = TenantIsolationMode.DedicatedDb };
        using var context = CreateContext(new FakeTenantContext(tenant));

        var entity = new TestEntity();
        context.Items.Add(entity);
        await context.SaveChangesAsync();

        Assert.Equal(Guid.Empty, entity.TenantId);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldNotStampTenantId_WhenNoTenantResolved()
    {
        using var context = CreateContext(new FakeTenantContext(null));

        var entity = new TestEntity();
        context.Items.Add(entity);
        await context.SaveChangesAsync();

        Assert.Equal(Guid.Empty, entity.TenantId);
    }
}
