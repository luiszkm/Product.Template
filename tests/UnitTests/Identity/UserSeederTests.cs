using Kernel.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Product.Template.Core.Identity.Infrastructure.Data.Seeders;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace UnitTests.Identity;

public class UserSeederTests
{
    private sealed class FakeTenantContext : ITenantContext
    {
        public Guid? TenantId => WellKnownTenants.Public;
        public string? TenantKey => "public";

        public TenantConfig? Tenant { get; } = new TenantConfig
        {
            TenantId = WellKnownTenants.Public,
            TenantKey = "public",
            IsolationMode = TenantIsolationMode.SharedDb
        };

        public bool IsResolved => true;
        public void SetTenant(TenantConfig tenant) { }
    }

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>()
            .Options, new FakeTenantContext());

    [Fact]
    public async Task SeedAsync_ShouldCreateAdminAndTestUsers_WhenNotYetSeeded()
    {
        using var context = CreateContext();

        await UserSeeder.SeedAsync(context, new HashServices());

        Assert.Equal(2, await context.Users.CountAsync());
        Assert.True(await context.Users.AnyAsync(u => u.Id == UserSeeder.AdminUserId));
        Assert.True(await context.Users.AnyAsync(u => u.Id == UserSeeder.TestUserId));
    }

    [Fact]
    public async Task SeedAsync_ShouldBeIdempotent_WhenUsersAlreadySeeded()
    {
        using var context = CreateContext();
        await UserSeeder.SeedAsync(context, new HashServices());

        await UserSeeder.SeedAsync(context, new HashServices());

        Assert.Equal(2, await context.Users.CountAsync());
    }
}
