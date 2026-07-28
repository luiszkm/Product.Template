using Product.Template.Core.Tenants.Application.Mappers;
using Product.Template.Core.Tenants.Domain.Entities;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace UnitTests.Mappers;

public class TenantMapperTests
{
    [Fact]
    public void ToOutput_ShouldMapAllFields()
    {
        var tenant = Tenant.Create("acme", "Acme Corp", "contact@acme.com", TenantIsolationMode.SharedDb);

        var output = tenant.ToOutput();

        Assert.Equal(tenant.Id, output.TenantId);
        Assert.Equal("acme", output.TenantKey);
        Assert.Equal("Acme Corp", output.DisplayName);
        Assert.Equal("contact@acme.com", output.ContactEmail);
        Assert.True(output.IsActive);
        Assert.Equal(TenantIsolationMode.SharedDb, output.IsolationMode);
    }

    [Fact]
    public void ToOutput_ShouldThrow_WhenTenantIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((Tenant)null!).ToOutput());
    }

    [Fact]
    public void ToOutputList_ShouldMapEachTenant()
    {
        var tenants = new[]
        {
            Tenant.Create("acme", "Acme", null, TenantIsolationMode.SharedDb),
            Tenant.Create("globex", "Globex", null, TenantIsolationMode.DedicatedDb)
        };

        var outputs = tenants.ToOutputList().ToList();

        Assert.Equal(2, outputs.Count);
        Assert.Contains(outputs, o => o.TenantKey == "acme");
        Assert.Contains(outputs, o => o.TenantKey == "globex");
    }

    [Fact]
    public void ToOutputList_ShouldThrow_WhenTenantsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<Tenant>)null!).ToOutputList());
    }
}
