using CommonTests.Builders;
using IntegrationTests.Common;
using Kernel.Domain.SeedWorks;

namespace IntegrationTests.Authorization;

public class RoleRepositoryListTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    [Fact]
    public async Task ListAllAsync_ShouldSortByNameDescending_WhenSortByNameAndDesc()
    {
        await _fixture.SeedRoleAsync(new RoleBuilder().WithName("Adams").Build());
        await _fixture.SeedRoleAsync(new RoleBuilder().WithName("Young").Build());

        var result = await _fixture.RoleRepository().ListAllAsync(
            new ListInput(SortBy: "name", SortDirection: "desc", PageSize: 50),
            CancellationToken.None);

        Assert.Equal("Young", result.Data[0].Name);
        Assert.Equal("Adams", result.Data[1].Name);
    }

    [Fact]
    public async Task ListAllAsync_ShouldUseCreatedAtDescending_WhenSortByIsNotProvided()
    {
        var older = new RoleBuilder().WithName("OlderRole").Build();
        older.CreatedAt = DateTime.UtcNow.AddDays(-2);
        var newer = new RoleBuilder().WithName("NewerRole").Build();
        newer.CreatedAt = DateTime.UtcNow.AddDays(-1);

        await _fixture.SeedRoleAsync(older);
        await _fixture.SeedRoleAsync(newer);

        var result = await _fixture.RoleRepository().ListAllAsync(new ListInput(PageSize: 50), CancellationToken.None);

        Assert.Equal("NewerRole", result.Data[0].Name);
        Assert.Equal("OlderRole", result.Data[1].Name);
    }

    public void Dispose() => _fixture.Dispose();
}
