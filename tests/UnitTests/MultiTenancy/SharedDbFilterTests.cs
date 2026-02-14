using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace UnitTests.MultiTenancy;

public class SharedDbFilterTests
{
    [Fact]
    public async Task Query_ShouldReturnOnlyCurrentTenantData_WhenSharedDb()
    {
        var dbName = $"tenant_filter_{Guid.NewGuid()}";

        await SeedAsync(dbName, 1, "tenant1@x.com");
        await SeedAsync(dbName, 2, "tenant2@x.com");

        var tenantContext = new TenantContext();
        tenantContext.SetTenant(new TenantConfig { TenantId = 1, TenantKey = "t1", IsolationMode = TenantIsolationMode.SharedDb });

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using var sut = new AppDbContext(options, tenantContext);
        var users = await sut.Users.ToListAsync();

        Assert.Single(users);
        Assert.Equal(1, users[0].TenantId);
    }

    private static async Task SeedAsync(string dbName, long tenantId, string email)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(new TenantConfig { TenantId = tenantId, TenantKey = $"t{tenantId}", IsolationMode = TenantIsolationMode.SharedDb });

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using var context = new AppDbContext(options, tenantContext);
        if (!await context.Users.AnyAsync(x => x.TenantId == tenantId))
        {
            context.Users.Add(User.Create(email, "hash", "n", "l"));
            await context.SaveChangesAsync();
        }
    }
}
