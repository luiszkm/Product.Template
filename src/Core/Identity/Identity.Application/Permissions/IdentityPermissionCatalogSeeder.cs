using Product.Template.Kernel.Application.Security;

namespace Product.Template.Core.Identity.Application.Permissions;

public sealed class IdentityPermissionCatalogSeeder : IPermissionCatalogSeeder
{
    public void Register(IPermissionCatalog catalog)
        => catalog.Register(IdentityPermissions.All);
}

