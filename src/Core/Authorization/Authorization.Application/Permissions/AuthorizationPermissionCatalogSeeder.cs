using Product.Template.Kernel.Application.Security;

namespace Product.Template.Core.Authorization.Application.Permissions;

public sealed class AuthorizationPermissionCatalogSeeder : IPermissionCatalogSeeder
{
    public void Register(IPermissionCatalog catalog)
        => catalog.Register(AuthorizationPermissions.All);
}
