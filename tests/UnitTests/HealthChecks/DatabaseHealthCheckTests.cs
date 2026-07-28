using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Api.HealthChecks;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace UnitTests.HealthChecks;

public class DatabaseHealthCheckTests
{
    private sealed class FakeTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
        public string? TenantKey => null;
        public TenantConfig? Tenant => null;
        public bool IsResolved => false;
        public void SetTenant(TenantConfig tenant) { }
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenDatabaseCallFails()
    {
        // The InMemory provider doesn't support raw SQL, so ExecuteSqlRawAsync("SELECT 1")
        // always throws here — this exercises the check's failure/catch path without a real DB.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options, new FakeTenantContext());
        var sut = new DatabaseHealthCheck(context, NullLogger<DatabaseHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("ErrorType", result.Data.Keys);
    }
}
