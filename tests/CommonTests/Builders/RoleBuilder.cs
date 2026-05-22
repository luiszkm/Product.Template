using Bogus;
using Product.Template.Core.Authorization.Domain.Entities;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace CommonTests.Builders;

public sealed class RoleBuilder
{
    private Guid _tenantId = WellKnownTenants.Public;
    private string _name;
    private string _description;

    public RoleBuilder()
    {
        var faker = new Faker();
        _name = faker.Commerce.Department();
        _description = faker.Lorem.Sentence();
    }

    public RoleBuilder WithTenantId(Guid tenantId) { _tenantId = tenantId; return this; }
    public RoleBuilder WithName(string name) { _name = name; return this; }
    public RoleBuilder WithDescription(string description) { _description = description; return this; }

    public Role Build() => Role.Create(_tenantId, _name, _description);

    public List<Role> BuildMany(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => new RoleBuilder().WithTenantId(_tenantId).Build())
            .ToList();
}
