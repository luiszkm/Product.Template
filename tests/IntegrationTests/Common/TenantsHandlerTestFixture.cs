using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Tenants.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Infrastructure.HostDb;

namespace IntegrationTests.Common;

/// <summary>
/// Fixture for Tenants handler integration tests.
/// Uses an EF InMemory HostDbContext — Tenants do not use owned value objects
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

    public void Dispose() => HostDbContext.Dispose();
}
