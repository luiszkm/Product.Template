using Kernel.Application.Security;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Core.Identity.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace IntegrationTests.Common;

/// <summary>
/// Shared fixture for Identity handler integration tests.
/// Uses SQLite in-memory so relational queries (EF.Property, value conversions) work correctly.
/// </summary>
public sealed class HandlerTestFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    public AppDbContext DbContext { get; }
    public TenantContext TenantContext { get; }
    public StubHashServices HashServices { get; } = new();

    public HandlerTestFixture()
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

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        DbContext = new AppDbContext(options, TenantContext);
        DbContext.Database.EnsureCreated();
    }

    public UserRepository UserRepository() => new(DbContext);
    public UnitOfWork UnitOfWork() => new(DbContext, NoopPublisher.Instance);

    public async Task<User> SeedUserAsync(string email = "seed@test.com", string firstName = "Seed", string lastName = "User")
    {
        var user = User.Create(1L, email, HashServices.GeneratePasswordHash("Pass@123"), firstName, lastName);
        user.ConfirmEmail();
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        return user;
    }

    public void Dispose()
    {
        DbContext.Dispose();
        _connection.Dispose();
    }
}

public sealed class StubHashServices : IHashServices
{
    public string GeneratePasswordHash(string password) => $"hashed:{password}";
    public bool VerifyPassword(string password, string hash) => hash == $"hashed:{password}";
}

/// <summary>
/// No-op MediatR publisher — discards all domain events in tests that don't need them.
/// </summary>
public sealed class NoopPublisher : IPublisher
{
    public static readonly NoopPublisher Instance = new();
    private NoopPublisher() { }

    public Task Publish(object notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => Task.CompletedTask;
}
