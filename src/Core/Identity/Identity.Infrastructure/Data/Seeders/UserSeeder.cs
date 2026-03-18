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
        if (await context.Users.IgnoreQueryFilters()
                .AnyAsync(u => u.Id == AdminUserId || u.Id == TestUserId))
            return;

        var tenantId = context.TenantIdForQueryFilter;

        var adminPasswordHash = hashServices.GeneratePasswordHash("Admin@123");
        var userPasswordHash  = hashServices.GeneratePasswordHash("User@123");

        var admin = User.Create(tenantId, "admin@producttemplate.com", adminPasswordHash, "System", "Administrator");
        typeof(User).BaseType!.GetProperty("Id")!.SetValue(admin, AdminUserId);
        admin.ConfirmEmail();

        var testUser = User.Create(tenantId, "user@producttemplate.com", userPasswordHash, "Test", "User");
        typeof(User).BaseType!.GetProperty("Id")!.SetValue(testUser, TestUserId);
        testUser.ConfirmEmail();

        await context.Users.AddRangeAsync(admin, testUser);
        await context.SaveChangesAsync();
    }
}
