using Bogus;
using Product.Template.Core.Authorization.Domain.Entities;

namespace CommonTests.Builders;

public sealed class RoleBuilder
{
    private readonly Faker _faker = new();
    private long _tenantId = 1L;
    private string _name;
    private string _description;

    public RoleBuilder()
    {
        _name = _faker.Name.JobTitle();
        _description = _faker.Lorem.Sentence();
    }

    public RoleBuilder WithTenantId(long tenantId) { _tenantId = tenantId; return this; }
    public RoleBuilder WithName(string name) { _name = name; return this; }
    public RoleBuilder WithDescription(string description) { _description = description; return this; }

    public Role Build() => Role.Create(_tenantId, _name, _description);

    public List<Role> BuildMany(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => new RoleBuilder().WithTenantId(_tenantId).Build())
            .ToList();
}
