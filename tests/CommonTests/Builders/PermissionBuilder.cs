using Bogus;
using Product.Template.Core.Authorization.Domain.Entities;

namespace CommonTests.Builders;

public sealed class PermissionBuilder
{
    private readonly Faker _faker = new();
    private long _tenantId = 1L;
    private string _name;
    private string _description;

    public PermissionBuilder()
    {
        _name = $"{_faker.Hacker.Noun()}.{_faker.Hacker.Verb()}".ToLowerInvariant().Replace(" ", "_");
        _description = _faker.Lorem.Sentence();
    }

    public PermissionBuilder WithTenantId(long tenantId) { _tenantId = tenantId; return this; }
    public PermissionBuilder WithName(string name) { _name = name; return this; }
    public PermissionBuilder WithDescription(string description) { _description = description; return this; }

    public Permission Build() => Permission.Create(_tenantId, _name, _description);

    public List<Permission> BuildMany(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => new PermissionBuilder().WithTenantId(_tenantId).Build())
            .ToList();
}
