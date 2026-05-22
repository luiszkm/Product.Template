using CommonTests.Builders;
using IntegrationTests.Common;
using Kernel.Domain.SeedWorks;

namespace IntegrationTests.Tenants;

public class TenantRepositoryListTests : IDisposable
{
    private readonly TenantsHandlerTestFixture _fixture = new();

    [Fact]
    public async Task ListAllAsync_ShouldFilterByDisplayName_WhenSearchTermMatchesDisplayName()
    {
        await _fixture.TenantRepository().AddAsync(new TenantBuilder().WithTenantKey("corp-a").WithDisplayName("Acme Holdings").Build());
        await _fixture.TenantRepository().AddAsync(new TenantBuilder().WithTenantKey("corp-b").WithDisplayName("Beta LLC").Build());
        await _fixture.HostDbContext.SaveChangesAsync();
        _fixture.HostDbContext.ChangeTracker.Clear();

        var result = await _fixture.TenantRepository().ListAllAsync(
            new ListInput(SearchTerm: "Acme", PageSize: 50),
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Acme Holdings", result.Data[0].DisplayName);
    }

    public void Dispose() => _fixture.Dispose();
}
