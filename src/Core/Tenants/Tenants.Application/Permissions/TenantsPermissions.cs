using Product.Template.Kernel.Application.Security;

namespace Product.Template.Core.Tenants.Application.Permissions;

public static class TenantsPermissions
{
    public const string Read = "tenants.read";
    public const string Manage = "tenants.manage";

    public static IReadOnlyCollection<PermissionDescriptor> All { get; } = new[]
    {
        new PermissionDescriptor(Read, "tenants", "tenant", "read", "Read tenants and their details"),
        new PermissionDescriptor(Manage, "tenants", "tenant", "manage", "Create, update, and deactivate tenants")
    };
}
