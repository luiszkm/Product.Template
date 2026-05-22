using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace UnitTests.MultiTenancy;

public class SharedDbFilterTests
{
    private static readonly Guid Tenant1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Tenant2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Query_ShouldReturnOnlyCurrentTenantData_WhenSharedDb()
    {
        var dbName = $"tenant_filter_{Guid.NewGuid()}";

        await SeedAsync(dbName, Tenant1Id, "tenant1@x.com");
        await SeedAsync(dbName, Tenant2Id, "tenant2@x.com");

        var tenantContext = new TenantContext();
        tenantContext.SetTenant(new TenantConfig { TenantId = Tenant1Id, TenantKey = "t1", IsolationMode = TenantIsolationMode.SharedDb });

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using var sut = new AppDbContext(options, tenantContext);
        var users = await sut.Users.ToListAsync();

        Assert.Single(users);
        Assert.Equal(Tenant1Id, users[0].TenantId);
    }

    private static async Task SeedAsync(string dbName, Guid tenantId, string email)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(new TenantConfig { TenantId = tenantId, TenantKey = $"t{tenantId:N}", IsolationMode = TenantIsolationMode.SharedDb });

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using var context = new AppDbContext(options, tenantContext);
        if (!await context.Users.AnyAsync(x => x.TenantId == tenantId))
        {
            var user = User.Create(tenantId, email, "hash", "n", "l");
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
    }
}
