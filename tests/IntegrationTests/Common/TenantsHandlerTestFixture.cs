using CommonTests.Builders;
using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Tenants.Domain.Entities;
using Product.Template.Core.Tenants.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Infrastructure.HostDb;

namespace IntegrationTests.Common;

public sealed class TenantsHandlerTestFixture : IDisposable
{
    public HostDbContext HostDbContext { get; }

    public TenantsHandlerTestFixture()
    {
        var options = new DbContextOptionsBuilder<HostDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        HostDbContext = new HostDbContext(options);
        HostDbContext.Database.EnsureCreated();
    }

    public TenantRepository TenantRepository() => new(HostDbContext);
    public HostUnitOfWork HostUnitOfWork() => new(HostDbContext, NoopPublisher.Instance);

    public async Task<Tenant> SeedTenantAsync(string? key = null)
    {
        var tenant = new TenantBuilder()
            .WithTenantKey(key ?? $"tenant-{Guid.NewGuid():N}")
            .Build();
        await TenantRepository().AddAsync(tenant);
        await HostDbContext.SaveChangesAsync();
        HostDbContext.ChangeTracker.Clear();
        return tenant;
    }

    public async Task<List<Tenant>> SeedManyTenantsAsync(int count = 5)
    {
        var repo = TenantRepository();
        var tenants = new TenantBuilder().BuildMany(count);
        foreach (var tenant in tenants)
            await repo.AddAsync(tenant);
        await HostDbContext.SaveChangesAsync();
        HostDbContext.ChangeTracker.Clear();
        return tenants;
    }

    public void Dispose() => HostDbContext.Dispose();
}
