using Product.Template.Kernel.Application.Security;

namespace Product.Template.Core.Authorization.Application.Permissions;

public static class AuthorizationPermissions
{
    public const string RoleRead = "authorization.role.read";
    public const string RoleManage = "authorization.role.manage";
    public const string PermissionRead = "authorization.permission.read";
    public const string PermissionManage = "authorization.permission.manage";

    public static IReadOnlyCollection<PermissionDescriptor> All { get; } = new[]
    {
        new PermissionDescriptor(RoleRead, "authorization", "role", "read", "Read roles and their permission assignments"),
        new PermissionDescriptor(RoleManage, "authorization", "role", "manage", "Create, update, delete roles and manage permission assignments"),
        new PermissionDescriptor(PermissionRead, "authorization", "permission", "read", "Read permissions catalog"),
        new PermissionDescriptor(PermissionManage, "authorization", "permission", "manage", "Create, update, delete permissions")
    };
}
