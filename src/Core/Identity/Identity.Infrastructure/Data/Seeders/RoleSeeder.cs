using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Core.Identity.Infrastructure.Data.Seeders;

internal static class RoleSeeder
{
    public static readonly Guid AdminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid UserRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ManagerRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Roles.AnyAsync())
            return;

        var roles = new[]
        {
            CreateRole(AdminRoleId, "Admin", "Administrator with full system access"),
            CreateRole(UserRoleId, "User", "Standard user with basic access"),
            CreateRole(ManagerRoleId, "Manager", "Manager with elevated access")
        };

        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();
    }

    private static Role CreateRole(Guid id, string name, string description)
    {
        var role = Role.Create(name, description);

        // Use reflection to set the Id
        var idProperty = typeof(Role).BaseType!.GetProperty("Id");
        idProperty!.SetValue(role, id);

        return role;
    }
}
