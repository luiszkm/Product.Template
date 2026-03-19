using CommonTests.Builders;
using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Tenants.Domain.Entities;
using Product.Template.Core.Tenants.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Infrastructure.HostDb;

namespace IntegrationTests.Common;

/// <summary>
/// Fixture for Tenants handler integration tests.
/// Uses EF InMemory — Tenants do not use owned value objects
/// or EF.Property queries, so the InMemory provider is sufficient.
/// </summary>
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

    public async Task<Tenant> SeedTenantAsync(string? key = null)
    {
        var tenant = new TenantBuilder()
            .WithTenantKey(key ?? $"tenant-{Guid.NewGuid():N}")
            .Build();
        await HostDbContext.Set<Tenant>().AddAsync(tenant);
        await HostDbContext.SaveChangesAsync();
        HostDbContext.ChangeTracker.Clear();
        return tenant;
    }

    public async Task<List<Tenant>> SeedManyTenantsAsync(int count = 5)
    {
        var tenants = new TenantBuilder().BuildMany(count);
        await HostDbContext.Set<Tenant>().AddRangeAsync(tenants);
        await HostDbContext.SaveChangesAsync();
        HostDbContext.ChangeTracker.Clear();
        return tenants;
    }

    public void Dispose() => HostDbContext.Dispose();
}
