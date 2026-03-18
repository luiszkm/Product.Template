using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Authorization.Domain.Entities;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Infrastructure.Persistence;
using Product.Template.Kernel.Infrastructure.Seeders;

namespace Product.Template.Core.Authorization.Infrastructure.Data.Seeders;

internal sealed class AuthorizationSeeder : IAppSeeder
{
    // These GUIDs must match UserSeeder in Identity.Infrastructure
    private static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TestUserId  = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static readonly Guid AdminRoleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-cccc-aaaaaaaaaaaa");
    private static readonly Guid UserRoleId  = Guid.Parse("bbbbbbbb-bbbb-bbbb-cccc-bbbbbbbbbbbb");

    private readonly IPermissionCatalog _permissionCatalog;

    public AuthorizationSeeder(IPermissionCatalog permissionCatalog)
    {
        _permissionCatalog = permissionCatalog;
    }

    public async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Set<Role>().IgnoreQueryFilters()
                .AnyAsync(r => r.Id == AdminRoleId || r.Id == UserRoleId, cancellationToken))
            return;

        var tenantId = context.TenantIdForQueryFilter;

        await SeedPermissionsAsync(context, tenantId, cancellationToken);

        var permissions = await context.Set<Permission>()
            .ToListAsync(cancellationToken);

        await SeedRolesAsync(context, tenantId, permissions, cancellationToken);
        await SeedUserAssignmentsAsync(context, tenantId, cancellationToken);
    }

    private async Task SeedPermissionsAsync(AppDbContext context, long tenantId, CancellationToken cancellationToken)
    {
        var existingNames = (await context.Set<Permission>().IgnoreQueryFilters()
                .Select(p => p.Name)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = _permissionCatalog.GetAll()
            .Where(pd => !existingNames.Contains(pd.Code))
            .Select(pd => Permission.Create(tenantId, pd.Code, pd.Description))
            .ToList();

        if (toAdd.Count == 0)
            return;

        await context.Set<Permission>().AddRangeAsync(toAdd, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(
        AppDbContext context,
        long tenantId,
        List<Permission> permissions,
        CancellationToken cancellationToken)
    {
        var adminRole = Role.Create(tenantId, "Admin", "Full system access");
        typeof(Role).BaseType!.GetProperty("Id")!.SetValue(adminRole, AdminRoleId);
        foreach (var permission in permissions)
            adminRole.AssignPermission(permission.Id);

        var readPermissions = permissions
            .Where(p => p.Name.EndsWith(".read", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var userRole = Role.Create(tenantId, "User", "Standard read-only access");
        typeof(Role).BaseType!.GetProperty("Id")!.SetValue(userRole, UserRoleId);
        foreach (var permission in readPermissions)
            userRole.AssignPermission(permission.Id);

        await context.Set<Role>().AddRangeAsync(new[] { adminRole, userRole }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedUserAssignmentsAsync(
        AppDbContext context,
        long tenantId,
        CancellationToken cancellationToken)
    {
        var adminAssignment = UserAssignment.Create(AdminUserId, AdminRoleId, tenantId, "Admin");
        var userAssignment  = UserAssignment.Create(TestUserId,  UserRoleId,  tenantId, "User");

        await context.Set<UserAssignment>().AddRangeAsync(new[] { adminAssignment, userAssignment }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
