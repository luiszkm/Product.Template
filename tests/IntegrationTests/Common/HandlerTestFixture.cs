using CommonTests.Builders;
using Kernel.Application.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Core.Identity.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace IntegrationTests.Common;

/// <summary>
/// Shared fixture for Identity handler integration tests.
/// Uses EF InMemory — isolated per fixture instance via a unique database name.
/// </summary>
public sealed class HandlerTestFixture : IDisposable
{
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

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        DbContext = new AppDbContext(options, TenantContext);
        DbContext.Database.EnsureCreated();
    }

    public UserRepository UserRepository() => new(DbContext);
    public UnitOfWork UnitOfWork() => new(DbContext, NoopPublisher.Instance);

    public async Task<User> SeedUserAsync(string? email = null)
    {
        var user = new UserBuilder()
            .WithEmail(email ?? $"seed-{Guid.NewGuid():N}@test.com")
            .WithConfirmedEmail()
            .Build();
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        return user;
    }

    public async Task<User> SeedUserAsync(User user)
    {
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        return user;
    }

    public async Task<List<User>> SeedManyUsersAsync(int count = 5)
    {
        var users = new UserBuilder().BuildMany(count);
        await DbContext.Users.AddRangeAsync(users);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        return users;
    }

    public void Dispose() => DbContext.Dispose();
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
