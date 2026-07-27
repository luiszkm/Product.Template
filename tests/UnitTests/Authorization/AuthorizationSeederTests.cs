using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Product.Template.Core.Authorization.Domain.Entities;
using Product.Template.Core.Authorization.Infrastructure.Data.Persistence;
using Product.Template.Core.Authorization.Infrastructure.Data.Seeders;
using Product.Template.Core.Tenants.Application.Permissions;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace UnitTests.Authorization;

public class AuthorizationSeederTests
{
    private const string OtherModuleRead = "identity.user.read";

    [Fact]
    public async Task SeedAsync_ShouldNotGrantTenantsRead_ToDefaultUserRole()
    {
        var context = CreateContext();
        var catalog = new PermissionCatalog();
        catalog.Register(
            new PermissionDescriptor(TenantsPermissions.Read, "tenants", "tenant", "read", "Read tenants"),
            new PermissionDescriptor(OtherModuleRead, "identity", "user", "read", "Read users"));

        await new AuthorizationSeeder(catalog).SeedAsync(context);

        var userRole = await context.Set<Role>()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .SingleAsync(r => r.Name == "User");

        var grantedNames = userRole.RolePermissions.Select(rp => rp.Permission!.Name).ToList();

        Assert.DoesNotContain(TenantsPermissions.Read, grantedNames);
        Assert.Contains(OtherModuleRead, grantedNames);
    }

    private static AppDbContext CreateContext()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(new TenantConfig
        {
            TenantId = WellKnownTenants.Public,
            TenantKey = "test",
            IsolationMode = TenantIsolationMode.SharedDb,
            IsActive = true
        });

        var registry = new EfModelAssemblyRegistry();
        registry.Register(typeof(RoleRepository).Assembly);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ReplaceService<IModelCacheKeyFactory, AuthorizationSeederTestModelCacheKeyFactory>()
            .Options;

        var context = new AppDbContext(options, tenantContext, registry);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class AuthorizationSeederTestModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime) =>
            (context.GetType(), designTime, "authorization-seeder-test");
    }
}
