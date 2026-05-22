using CommonTests.Builders;
using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Queries.Permission;

namespace IntegrationTests.Authorization;

public class ListPermissionsQueryHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    private ListPermissionsQueryHandler CreateHandler() => new(
        _fixture.PermissionRepository(),
        NullLogger<ListPermissionsQueryHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldReturnPaginatedPermissions_WhenPermissionsExist()
    {
        await _fixture.SeedManyPermissionsAsync(3);

        var result = await CreateHandler().Handle(new ListPermissionsQuery { PageSize = 50 }, CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Data.Count);
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm_WhenNameMatches()
    {
        await _fixture.SeedPermissionAsync(new PermissionBuilder().WithName("users.read").WithDescription("Read users").Build());
        await _fixture.SeedPermissionAsync(new PermissionBuilder().WithName("roles.manage").Build());

        var result = await CreateHandler().Handle(
            new ListPermissionsQuery { SearchTerm = "users.read", PageSize = 50 },
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("users.read", result.Data[0].Name);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenSearchTermMatchesNothing()
    {
        await _fixture.SeedManyPermissionsAsync(2);

        var result = await CreateHandler().Handle(
            new ListPermissionsQuery { SearchTerm = "no-match-xyz-999" },
            CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task Handle_ShouldSortByNameDescending_WhenSortByNameAndDesc()
    {
        await _fixture.SeedPermissionAsync(new PermissionBuilder().WithName("alpha.perm").Build());
        await _fixture.SeedPermissionAsync(new PermissionBuilder().WithName("zulu.perm").Build());

        var result = await CreateHandler().Handle(
            new ListPermissionsQuery { SortBy = "name", SortDirection = "desc", PageSize = 50 },
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("zulu.perm", result.Data[0].Name);
        Assert.Equal("alpha.perm", result.Data[1].Name);
    }

    public void Dispose() => _fixture.Dispose();
}
