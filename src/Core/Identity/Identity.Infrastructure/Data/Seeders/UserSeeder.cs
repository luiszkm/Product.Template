using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Core.Identity.Domain.ValueObjects;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Core.Identity.Infrastructure.Data.Seeders;

internal static class UserSeeder
{
    public static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid TestUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync())
            return;

        // Note: In production, use a proper password hashing service
        // This is a hashed version of "Admin@123" - replace with actual hashed password
        var adminPasswordHash = "$2a$11$XYZ..."; // Replace with actual BCrypt hash
        var userPasswordHash = "$2a$11$ABC..."; // Replace with actual BCrypt hash

        var adminRole = await context.Roles.FindAsync(RoleSeeder.AdminRoleId);
        var userRole = await context.Roles.FindAsync(RoleSeeder.UserRoleId);

        var users = new[]
        {
            CreateUser(
                AdminUserId, 
                "admin@producttemplate.com", 
                adminPasswordHash, 
                "System", 
                "Administrator"
            ),
            CreateUser(
                TestUserId, 
                "user@producttemplate.com", 
                userPasswordHash, 
                "Test", 
                "User"
            )
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        // Assign roles
        if (adminRole != null)
            users[0].AddRole(adminRole);

        if (userRole != null)
            users[1].AddRole(userRole);

        await context.SaveChangesAsync();
    }

    private static User CreateUser(
        Guid id, 
        string email, 
        string passwordHash, 
        string firstName, 
        string lastName)
    {
        var user = User.Create(
            Email.Create(email),
            passwordHash,
            firstName,
            lastName
        );

        // Use reflection to set the Id
        var idProperty = typeof(User).BaseType!.GetProperty("Id");
        idProperty!.SetValue(user, id);

        // Confirm email for seed users
        user.ConfirmEmail();

        return user;
    }
}
