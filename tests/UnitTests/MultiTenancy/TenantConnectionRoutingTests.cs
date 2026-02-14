using Microsoft.Extensions.Configuration;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;

namespace UnitTests.MultiTenancy;

public class TenantConnectionRoutingTests
{
    [Fact]
    public void ResolveAppConnection_ShouldUseTenantConnection_WhenDedicatedDb()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:AppDb"] = "Data Source=shared.db" })
            .Build();

        var resolver = new TenantConnectionStringResolver(configuration);
        var tenant = new TenantConfig
        {
            TenantId = 10,
            TenantKey = "enterprise",
            IsolationMode = TenantIsolationMode.DedicatedDb,
            ConnectionString = "Host=localhost;Database=tenant_enterprise;Username=postgres;Password=postgres"
        };

        var conn = resolver.ResolveAppConnection(tenant);

        Assert.Equal(tenant.ConnectionString, conn);
    }
}
