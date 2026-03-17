using Product.Template.Kernel.Application.Security;

namespace Product.Template.Core.Tenants.Application.Permissions;

public sealed class TenantsPermissionCatalogSeeder : IPermissionCatalogSeeder
{
    public void Register(IPermissionCatalog catalog) => catalog.Register(TenantsPermissions.All);
}
