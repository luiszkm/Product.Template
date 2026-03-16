using System.Collections.Generic;
using Product.Template.Kernel.Application.Security;

namespace Product.Template.Core.Identity.Application.Permissions;

public static class IdentityPermissions
{
    public const string UserRead = "identity.user.read";
    public const string UserManage = "identity.user.manage";
    public const string RoleRead = "identity.role.read";
    public const string RoleManage = "identity.role.manage";

    public static IReadOnlyCollection<PermissionDescriptor> All { get; } = new[]
    {
        new PermissionDescriptor(UserRead, "identity", "user", "read", "Ler usuários e detalhes básicos"),
        new PermissionDescriptor(UserManage, "identity", "user", "manage", "Gerenciar usuários (criar/atualizar/excluir, roles)"),
        new PermissionDescriptor(RoleRead, "identity", "role", "read", "Ler roles e permissões"),
        new PermissionDescriptor(RoleManage, "identity", "role", "manage", "Gerenciar roles e permissões")
    };
}

