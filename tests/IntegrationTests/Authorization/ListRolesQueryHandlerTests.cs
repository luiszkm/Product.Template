using CommonTests.Builders;
using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Queries.Role;

namespace IntegrationTests.Authorization;

public class ListRolesQueryHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    private ListRolesQueryHandler CreateHandler() => new(
        _fixture.RoleRepository(),
        NullLogger<ListRolesQueryHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldReturnPaginatedRoles_WhenRolesExist()
    {
        await _fixture.SeedManyRolesAsync(3);

        var result = await CreateHandler().Handle(new ListRolesQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Data.Count);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoRolesExist()
    {
        var result = await CreateHandler().Handle(new ListRolesQuery(), CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm_WhenNameMatches()
    {
        await _fixture.SeedRoleAsync(new RoleBuilder().WithName("UniqueManager").WithDescription("Team lead").Build());
        await _fixture.SeedRoleAsync(new RoleBuilder().WithName("Auditor").Build());

        var result = await CreateHandler().Handle(
            new ListRolesQuery { SearchTerm = "UniqueManager", PageSize = 50 },
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("UniqueManager", result.Data[0].Name);
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm_WhenDescriptionMatches()
    {
        await _fixture.SeedRoleAsync(new RoleBuilder().WithName("RoleA").WithDescription("Special access scope").Build());
        await _fixture.SeedRoleAsync(new RoleBuilder().WithName("RoleB").WithDescription("Other").Build());

        var result = await CreateHandler().Handle(
            new ListRolesQuery { SearchTerm = "Special access", PageSize = 50 },
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("RoleA", result.Data[0].Name);
    }

    [Fact]
    public async Task Handle_ShouldPaginate_WhenPageSizeIsSmallerThanTotal()
    {
        await _fixture.SeedManyRolesAsync(5);

        var page1 = await CreateHandler().Handle(new ListRolesQuery { PageNumber = 1, PageSize = 2 }, CancellationToken.None);
        var page2 = await CreateHandler().Handle(new ListRolesQuery { PageNumber = 2, PageSize = 2 }, CancellationToken.None);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Data.Count);
        Assert.Equal(2, page2.Data.Count);
        Assert.NotEqual(page1.Data[0].Id, page2.Data[0].Id);
    }

    [Fact]
    public async Task Handle_ShouldSortByNameAscending_WhenSortByNameAndAsc()
    {
        await _fixture.SeedRoleAsync(new RoleBuilder().WithName("ZuluRole").Build());
        await _fixture.SeedRoleAsync(new RoleBuilder().WithName("AlphaRole").Build());

        var result = await CreateHandler().Handle(
            new ListRolesQuery { SortBy = "name", SortDirection = "asc", PageSize = 50 },
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("AlphaRole", result.Data[0].Name);
        Assert.Equal("ZuluRole", result.Data[1].Name);
    }

    public void Dispose() => _fixture.Dispose();
}
