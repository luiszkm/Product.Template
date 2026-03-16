using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Core.Identity.Infrastructure.Data.Seeders;

internal static class PermissionSeeder
{
    public static readonly Guid UsersReadPermissionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid UsersManagePermissionId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid RolesManagePermissionId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    public static async Task SeedAsync(AppDbContext context)
    {
        var tenantId = context.TenantIdForQueryFilter;

        if (!await context.Permissions.AnyAsync())
        {
            var permissions = new[]
            {
                CreatePermission(UsersReadPermissionId, tenantId, "users.read", "Read users data"),
                CreatePermission(UsersManagePermissionId, tenantId, "users.manage", "Manage users data"),
                CreatePermission(RolesManagePermissionId, tenantId, "roles.manage", "Manage user roles")
            };

            await context.Permissions.AddRangeAsync(permissions);
            await context.SaveChangesAsync();
        }

        var adminRole = await context.Roles.FirstOrDefaultAsync(x => x.Id == RoleSeeder.AdminRoleId);
        var managerRole = await context.Roles.FirstOrDefaultAsync(x => x.Id == RoleSeeder.ManagerRoleId);

        if (adminRole is null)
            return;

        await EnsureRolePermission(context, adminRole.Id, UsersReadPermissionId);
        await EnsureRolePermission(context, adminRole.Id, UsersManagePermissionId);
        await EnsureRolePermission(context, adminRole.Id, RolesManagePermissionId);

        if (managerRole is not null)
        {
            await EnsureRolePermission(context, managerRole.Id, UsersReadPermissionId);
        }

        await context.SaveChangesAsync();
    }

    private static Permission CreatePermission(Guid id, long tenantId, string name, string description)
    {
        var permission = Permission.Create(tenantId, name, description);
        var idProperty = typeof(Permission).BaseType!.GetProperty("Id");
        idProperty!.SetValue(permission, id);
        return permission;
    }

    private static async Task EnsureRolePermission(AppDbContext context, Guid roleId, Guid permissionId)
    {
        var exists = await context.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permissionId);
        if (exists)
            return;

        var tenantId = context.TenantIdForQueryFilter;
        context.RolePermissions.Add(RolePermission.Create(roleId, permissionId, tenantId));
    }
}
