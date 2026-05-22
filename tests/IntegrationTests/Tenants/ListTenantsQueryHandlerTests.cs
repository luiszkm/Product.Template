using CommonTests.Builders;
using IntegrationTests.Common;
using Product.Template.Core.Tenants.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Tenants.Application.Queries;

namespace IntegrationTests.Tenants;

public class ListTenantsQueryHandlerTests : IDisposable
{
    private readonly TenantsHandlerTestFixture _fixture = new();

    private ListTenantsQueryHandler CreateHandler() => new(
        _fixture.TenantRepository(),
        NullLogger<ListTenantsQueryHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldReturnPaginatedTenants_WhenTenantsExist()
    {
        await _fixture.SeedManyTenantsAsync(3);

        var result = await CreateHandler().Handle(new ListTenantsQuery { PageSize = 50 }, CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Data.Count);
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm_WhenTenantKeyMatches()
    {
        await _fixture.SeedTenantAsync("unique-corp");
        await _fixture.SeedTenantAsync("other-corp");

        var result = await CreateHandler().Handle(
            new ListTenantsQuery { SearchTerm = "unique-corp", PageSize = 50 },
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("unique-corp", result.Data[0].TenantKey);
    }

    [Fact]
    public async Task Handle_ShouldSortByTenantKeyAscending_WhenSortByKeyAndAsc()
    {
        await _fixture.TenantRepository().AddAsync(new TenantBuilder().WithTenantKey("zulu").WithDisplayName("Z").Build());
        await _fixture.TenantRepository().AddAsync(new TenantBuilder().WithTenantKey("alpha").WithDisplayName("A").Build());
        await _fixture.HostDbContext.SaveChangesAsync();
        _fixture.HostDbContext.ChangeTracker.Clear();

        var result = await CreateHandler().Handle(
            new ListTenantsQuery { SortBy = "tenantKey", SortDirection = "asc", PageSize = 50 },
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("alpha", result.Data[0].TenantKey);
        Assert.Equal("zulu", result.Data[1].TenantKey);
    }

    public void Dispose() => _fixture.Dispose();
}
