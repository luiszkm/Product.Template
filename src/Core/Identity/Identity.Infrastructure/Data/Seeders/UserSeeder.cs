using Kernel.Application.Security;
using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Core.Identity.Infrastructure.Data.Seeders;

internal static class UserSeeder
{
    public static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid TestUserId  = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static async Task SeedAsync(AppDbContext context, IHashServices hashServices)
    {
        if (await context.Users.AnyAsync())
            return;

        var adminPasswordHash = hashServices.GeneratePasswordHash("Admin@123");
        var userPasswordHash  = hashServices.GeneratePasswordHash("User@123");

        var adminRole = await context.Roles.FindAsync(RoleSeeder.AdminRoleId);
        var userRole  = await context.Roles.FindAsync(RoleSeeder.UserRoleId);

        var admin = User.Create("admin@producttemplate.com", adminPasswordHash, "System", "Administrator");
        admin.SetId(AdminUserId);
        admin.ConfirmEmail();

        var testUser = User.Create("user@producttemplate.com", userPasswordHash, "Test", "User");
        testUser.SetId(TestUserId);
        testUser.ConfirmEmail();

        await context.Users.AddRangeAsync(admin, testUser);
        await context.SaveChangesAsync();

        if (adminRole is not null) admin.AddRole(adminRole);
        if (userRole  is not null) testUser.AddRole(userRole);

        await context.SaveChangesAsync();
    }
}
