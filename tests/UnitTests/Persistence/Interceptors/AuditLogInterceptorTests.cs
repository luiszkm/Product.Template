using Kernel.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Domain.Audit;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace UnitTests.Persistence.Interceptors;

public class AuditLogInterceptorTests
{
    private sealed class TestEntity : Entity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestEntity> Items => Set<TestEntity>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    }

    private sealed class FakeCurrentUserService(string? userName) : ICurrentUserService
    {
        public Guid? UserId => null;
        public string? Email => null;
        public string? UserName => userName;
        public bool IsAuthenticated => userName is not null;
        public IEnumerable<System.Security.Claims.Claim> Claims => [];
    }

    private sealed class FakeTenantContext(Guid? tenantId) : ITenantContext
    {
        public Guid? TenantId => tenantId;
        public string? TenantKey => "acme";
        public TenantConfig? Tenant => null;
        public bool IsResolved => tenantId is not null;
        public void SetTenant(TenantConfig tenant) { }
    }

    private static TestDbContext CreateContext(Guid? tenantId, string? userName = "jane")
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditLogInterceptor(new FakeCurrentUserService(userName), new FakeTenantContext(tenantId)))
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldWriteAuditLog_ForAddedEntity()
    {
        var tenantId = Guid.NewGuid();
        using var context = CreateContext(tenantId);

        context.Items.Add(new TestEntity { Name = "acme" });
        await context.SaveChangesAsync();

        var log = Assert.Single(context.AuditLogs);
        Assert.Equal("Created", log.Action);
        Assert.Equal("TestEntity", log.EntityType);
        Assert.Equal(tenantId, log.TenantId);
        Assert.Equal("jane", log.Actor);
        Assert.Null(log.Changes);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldWriteAuditLogWithChanges_ForModifiedEntity()
    {
        using var context = CreateContext(Guid.NewGuid());
        var entity = new TestEntity { Name = "before" };
        context.Items.Add(entity);
        await context.SaveChangesAsync();

        entity.Name = "after";
        await context.SaveChangesAsync();

        var log = context.AuditLogs.OrderByDescending(x => x.OccurredAt).First();
        Assert.Equal("Updated", log.Action);
        Assert.Contains("after", log.Changes);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldNotWriteAuditLog_WhenNoAuditableEntitiesChanged()
    {
        using var context = CreateContext(Guid.NewGuid());

        await context.SaveChangesAsync();

        Assert.Empty(context.AuditLogs);
    }
}
