using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Core.Identity.Domain.Events;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace UnitTests.Persistence;

public class UnitOfWorkTests
{
    private sealed class FakeTenantContext(TenantConfig tenant) : ITenantContext
    {
        public Guid? TenantId => tenant.TenantId;
        public string? TenantKey => tenant.TenantKey;
        public TenantConfig? Tenant => tenant;
        public bool IsResolved => true;
        public void SetTenant(TenantConfig newTenant) { }
    }

    private sealed class FakePublisher : IPublisher
    {
        public List<object> Published { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            Published.Add(notification!);
            return Task.CompletedTask;
        }
    }

    private static AppDbContext CreateContext(ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>()
            .Options;

        return new AppDbContext(options, tenantContext);
    }

    [Fact]
    public async Task Commit_ShouldPersistChanges_AndDispatchDomainEvents()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new TenantConfig { TenantId = tenantId, TenantKey = "acme", IsolationMode = TenantIsolationMode.SharedDb };
        var context = CreateContext(new FakeTenantContext(tenant));
        var publisher = new FakePublisher();
        var sut = new UnitOfWork(context, publisher);

        var user = User.Create(tenantId, "user@acme.com", "hash", "Jane", "Doe");
        context.Users.Add(user);

        await sut.Commit(CancellationToken.None);

        Assert.Single(publisher.Published.OfType<UserRegisteredEvent>());
        Assert.Empty(user.DomainEvents);
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Fact]
    public async Task Commit_ShouldNotPublishAnything_WhenNoDomainEventsRaised()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new TenantConfig { TenantId = tenantId, TenantKey = "acme", IsolationMode = TenantIsolationMode.SharedDb };
        var context = CreateContext(new FakeTenantContext(tenant));
        var publisher = new FakePublisher();
        var sut = new UnitOfWork(context, publisher);

        var user = User.Create(tenantId, "user@acme.com", "hash", "Jane", "Doe");
        context.Users.Add(user);
        await sut.Commit(CancellationToken.None);
        publisher.Published.Clear();

        user.UpdateProfile("Jane", "Smith");
        await sut.Commit(CancellationToken.None);

        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task Rollback_ShouldCompleteWithoutTouchingContext()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new TenantConfig { TenantId = tenantId, TenantKey = "acme", IsolationMode = TenantIsolationMode.SharedDb };
        var context = CreateContext(new FakeTenantContext(tenant));
        var sut = new UnitOfWork(context, new FakePublisher());

        await sut.Rollback(CancellationToken.None);

        Assert.Equal(0, await context.Users.CountAsync());
    }
}
