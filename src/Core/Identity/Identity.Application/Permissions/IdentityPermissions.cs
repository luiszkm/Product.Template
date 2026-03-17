using Product.Template.Kernel.Application.Security;

namespace Product.Template.Core.Identity.Application.Permissions;

public static class IdentityPermissions
{
    public const string UserRead = "identity.user.read";
    public const string UserManage = "identity.user.manage";

    public static IReadOnlyCollection<PermissionDescriptor> All { get; } = new[]
    {
        new PermissionDescriptor(UserRead, "identity", "user", "read", "Read users and basic details"),
        new PermissionDescriptor(UserManage, "identity", "user", "manage", "Manage users (create/update/delete)")
    };
}
