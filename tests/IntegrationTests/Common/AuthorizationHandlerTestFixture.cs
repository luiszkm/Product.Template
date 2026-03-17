using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Authorization.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace IntegrationTests.Common;

/// <summary>
/// Fixture for Authorization handler integration tests.
/// Registers Authorization EF configurations via <see cref="EfModelAssemblyRegistry"/>
/// so that Role, Permission, RolePermission and UserAssignment tables are created.
/// </summary>
public sealed class AuthorizationHandlerTestFixture : IDisposable
{
    private readonly SqliteConnection _connection;
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

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var registry = new EfModelAssemblyRegistry();
        registry.Register(typeof(RoleRepository).Assembly); // Authorization.Infrastructure configs

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        DbContext = new AppDbContext(options, TenantContext, registry);
        DbContext.Database.EnsureCreated();
    }

    public RoleRepository RoleRepository() => new(DbContext);
    public PermissionRepository PermissionRepository() => new(DbContext);
    public UserAssignmentRepository UserAssignmentRepository() => new(DbContext);
    public UnitOfWork UnitOfWork() => new(DbContext, NoopPublisher.Instance);

    public void Dispose()
    {
        DbContext.Dispose();
        _connection.Dispose();
    }
}
