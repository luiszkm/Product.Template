using CommonTests.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Product.Template.Core.Authorization.Domain.Entities;
using Product.Template.Core.Authorization.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace IntegrationTests.Common;

/// <summary>
/// Fixture for Authorization handler integration tests.
/// Uses EF InMemory — isolated per fixture instance via a unique database name.
/// Registers Authorization EF configurations via <see cref="EfModelAssemblyRegistry"/>
/// so that Role, Permission, RolePermission and UserAssignment entity types are configured.
/// </summary>
public sealed class AuthorizationHandlerTestFixture : IDisposable
{
    public AppDbContext DbContext { get; }
    public TenantContext TenantContext { get; }

    public AuthorizationHandlerTestFixture()
    {
        TenantContext = new TenantContext();
        TenantContext.SetTenant(new TenantConfig
        {
            TenantId = 1,
            TenantKey = "test",
            IsolationMode = TenantIsolationMode.SharedDb,
            IsActive = true
        });

        var registry = new EfModelAssemblyRegistry();
        registry.Register(typeof(RoleRepository).Assembly); // Authorization.Infrastructure configs

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // Distinct cache key so EF builds a separate model that includes Authorization entity types.
            // Without this, EF reuses the model cached by HandlerTestFixture (which lacks the registry).
            .ReplaceService<IModelCacheKeyFactory, AuthorizationTestModelCacheKeyFactory>()
            .Options;

        DbContext = new AppDbContext(options, TenantContext, registry);
        DbContext.Database.EnsureCreated();
    }

    public RoleRepository RoleRepository() => new(DbContext);
    public PermissionRepository PermissionRepository() => new(DbContext);
    public UserAssignmentRepository UserAssignmentRepository() => new(DbContext);
    public UnitOfWork UnitOfWork() => new(DbContext, NoopPublisher.Instance);

    public async Task<Role> SeedRoleAsync(string? name = null)
    {
        var role = new RoleBuilder()
            .WithName(name ?? $"role-{Guid.NewGuid():N}")
            .Build();
        await DbContext.Set<Role>().AddAsync(role);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        return role;
    }

    public async Task<List<Role>> SeedManyRolesAsync(int count = 5)
    {
        var roles = new RoleBuilder().BuildMany(count);
        await DbContext.Set<Role>().AddRangeAsync(roles);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        return roles;
    }

    public async Task<Permission> SeedPermissionAsync(string? name = null)
    {
        var permission = new PermissionBuilder()
            .WithName(name ?? $"perm.{Guid.NewGuid():N}")
            .Build();
        await DbContext.Set<Permission>().AddAsync(permission);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        return permission;
    }

    public void Dispose() => DbContext.Dispose();
}

/// <summary>
/// Ensures a distinct compiled model cache entry for Authorization fixture contexts.
/// EF InMemory caches models per context type; using a different cache key
/// prevents collisions with HandlerTestFixture (which builds a model without Authorization entities).
/// </summary>
internal sealed class AuthorizationTestModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        (context.GetType(), designTime, "authorization");
}
