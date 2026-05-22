using CommonTests.Builders;
using IntegrationTests.Common;
using Kernel.Domain.SeedWorks;

namespace IntegrationTests.Authorization;

public class PermissionRepositoryListTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    [Fact]
    public async Task ListAllAsync_ShouldSortByNameDescending_WhenSortByNameAndDesc()
    {
        await _fixture.SeedPermissionAsync(new PermissionBuilder().WithName("perm.adams").Build());
        await _fixture.SeedPermissionAsync(new PermissionBuilder().WithName("perm.young").Build());

        var result = await _fixture.PermissionRepository().ListAllAsync(
            new ListInput(SortBy: "name", SortDirection: "desc", PageSize: 50),
            CancellationToken.None);

        Assert.Equal("perm.young", result.Data[0].Name);
        Assert.Equal("perm.adams", result.Data[1].Name);
    }

    [Fact]
    public async Task ListAllAsync_ShouldUseCreatedAtDescending_WhenSortByIsNotProvided()
    {
        var older = new PermissionBuilder().WithName("perm.older").Build();
        older.CreatedAt = DateTime.UtcNow.AddDays(-2);
        var newer = new PermissionBuilder().WithName("perm.newer").Build();
        newer.CreatedAt = DateTime.UtcNow.AddDays(-1);

        await _fixture.SeedPermissionAsync(older);
        await _fixture.SeedPermissionAsync(newer);

        var result = await _fixture.PermissionRepository().ListAllAsync(new ListInput(PageSize: 50), CancellationToken.None);

        Assert.Equal("perm.newer", result.Data[0].Name);
        Assert.Equal("perm.older", result.Data[1].Name);
    }

    public void Dispose() => _fixture.Dispose();
}
