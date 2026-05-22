using Bogus;
using Product.Template.Core.Tenants.Domain.Entities;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace CommonTests.Builders;

public sealed class TenantBuilder
{
    private readonly Faker _faker = new();
    private Guid _tenantId = Guid.Empty;
    private string _tenantKey;
    private string _displayName;
    private string? _contactEmail;
    private TenantIsolationMode _isolationMode = TenantIsolationMode.SharedDb;

    public TenantBuilder()
    {
        _tenantKey = _faker.Internet.DomainWord().ToLowerInvariant();
        _displayName = _faker.Company.CompanyName();
        _contactEmail = _faker.Internet.Email();
    }

    public TenantBuilder WithTenantId(Guid tenantId) { _tenantId = tenantId; return this; }
    public TenantBuilder WithTenantKey(string key) { _tenantKey = key; return this; }
    public TenantBuilder WithDisplayName(string name) { _displayName = name; return this; }
    public TenantBuilder WithContactEmail(string? email) { _contactEmail = email; return this; }
    public TenantBuilder WithIsolationMode(TenantIsolationMode mode) { _isolationMode = mode; return this; }

    public Tenant Build()
    {
        if (_tenantId != Guid.Empty)
        {
            return Tenant.Reconstitute(
                _tenantId,
                _tenantKey,
                _displayName,
                _contactEmail,
                true,
                _isolationMode,
                DateTime.UtcNow);
        }

        return Tenant.Create(_tenantKey, _displayName, _contactEmail, _isolationMode);
    }

    public List<Tenant> BuildMany(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => new TenantBuilder().Build())
            .ToList();
}
