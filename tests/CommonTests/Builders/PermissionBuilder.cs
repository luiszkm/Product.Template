using Bogus;
using Product.Template.Core.Authorization.Domain.Entities;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace CommonTests.Builders;

public sealed class PermissionBuilder
{
    private Guid _tenantId = WellKnownTenants.Public;
    private string _name;
    private string _description;

    public PermissionBuilder()
    {
        var faker = new Faker();
        _name = faker.Commerce.ProductName().Replace(" ", ".").ToLowerInvariant();
        _description = faker.Lorem.Sentence();
    }

    public PermissionBuilder WithTenantId(Guid tenantId) { _tenantId = tenantId; return this; }
    public PermissionBuilder WithName(string name) { _name = name; return this; }
    public PermissionBuilder WithDescription(string description) { _description = description; return this; }

    public Permission Build() => Permission.Create(_tenantId, _name, _description);

    public List<Permission> BuildMany(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => new PermissionBuilder().WithTenantId(_tenantId).Build())
            .ToList();
}
