using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Product.Template.Core.Authorization.Domain.Entities;
using Product.Template.Core.Authorization.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace UnitTests.Authorization;

public class UserRolesProviderTests
{
    private sealed class FakeTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid? TenantId => tenantId;
        public string? TenantKey => "acme";
        public TenantConfig? Tenant { get; } = new TenantConfig { TenantId = tenantId, TenantKey = "acme", IsolationMode = TenantIsolationMode.SharedDb };
        public bool IsResolved => true;
        public void SetTenant(TenantConfig tenant) { }
    }

    private static AppDbContext CreateContext(Guid tenantId)
    {
        var registry = new EfModelAssemblyRegistry();
        registry.Register(typeof(RoleRepository).Assembly);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>()
            .Options;

        return new AppDbContext(options, new FakeTenantContext(tenantId), registry);
    }

    [Fact]
    public async Task GetUserRolesAndPermissionsAsync_ShouldReturnRolesAndPermissions_ForAssignedUser()
    {
        var tenantId = Guid.NewGuid();
        using var context = CreateContext(tenantId);
        var userId = Guid.NewGuid();

        var permission = Permission.Create(tenantId, "users:read", "Read users");
        var role = Role.Create(tenantId, "Admin", "Administrator");
        role.AssignPermission(permission.Id);
        var assignment = UserAssignment.Create(userId, role.Id, tenantId, role.Name);

        context.Set<Permission>().Add(permission);
        context.Set<Role>().Add(role);
        context.Set<UserAssignment>().Add(assignment);
        await context.SaveChangesAsync();

        var sut = new UserRolesProvider(context);
        var result = await sut.GetUserRolesAndPermissionsAsync(userId, CancellationToken.None);

        Assert.Contains("Admin", result.Roles);
        Assert.Contains("users:read", result.Permissions);
    }

    [Fact]
    public async Task GetUserRolesAndPermissionsAsync_ShouldReturnEmpty_WhenUserHasNoAssignments()
    {
        using var context = CreateContext(Guid.NewGuid());

        var sut = new UserRolesProvider(context);
        var result = await sut.GetUserRolesAndPermissionsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(result.Roles);
        Assert.Empty(result.Permissions);
    }

    [Fact]
    public async Task GetUserRolesAndPermissionsAsync_ShouldDeduplicateRoleAndPermissionNames()
    {
        var tenantId = Guid.NewGuid();
        using var context = CreateContext(tenantId);
        var userId = Guid.NewGuid();

        var permission = Permission.Create(tenantId, "users:read", "Read users");
        var roleA = Role.Create(tenantId, "Admin", "Administrator");
        roleA.AssignPermission(permission.Id);
        var roleB = Role.Create(tenantId, "admin", "Duplicate-cased admin");
        roleB.AssignPermission(permission.Id);

        context.Set<Permission>().Add(permission);
        context.Set<Role>().AddRange(roleA, roleB);
        context.Set<UserAssignment>().AddRange(
            UserAssignment.Create(userId, roleA.Id, tenantId, roleA.Name),
            UserAssignment.Create(userId, roleB.Id, tenantId, roleB.Name));
        await context.SaveChangesAsync();

        var sut = new UserRolesProvider(context);
        var result = await sut.GetUserRolesAndPermissionsAsync(userId, CancellationToken.None);

        Assert.Single(result.Roles);
        Assert.Single(result.Permissions);
    }
}
