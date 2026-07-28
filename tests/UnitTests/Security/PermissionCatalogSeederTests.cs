using Product.Template.Core.Identity.Application.Permissions;
using Product.Template.Core.Tenants.Application.Permissions;
using Product.Template.Kernel.Application.Security;

namespace UnitTests.Security;

public class PermissionCatalogSeederTests
{
    [Fact]
    public void IdentityPermissionCatalogSeeder_ShouldRegisterAllIdentityPermissions()
    {
        var catalog = new PermissionCatalog();

        new IdentityPermissionCatalogSeeder().Register(catalog);

        foreach (var descriptor in IdentityPermissions.All)
        {
            Assert.True(catalog.Contains(descriptor.Code));
        }
    }

    [Fact]
    public void TenantsPermissionCatalogSeeder_ShouldRegisterAllTenantsPermissions()
    {
        var catalog = new PermissionCatalog();

        new TenantsPermissionCatalogSeeder().Register(catalog);

        foreach (var descriptor in TenantsPermissions.All)
        {
            Assert.True(catalog.Contains(descriptor.Code));
        }
    }
}
