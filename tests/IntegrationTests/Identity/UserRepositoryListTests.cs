using CommonTests.Builders;
using IntegrationTests.Common;
using Kernel.Domain.SeedWorks;

namespace IntegrationTests.Identity;

public class UserRepositoryListTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();

    [Fact]
    public async Task ListAllAsync_ShouldFilterByLastName_WhenSearchTermMatchesLastName()
    {
        await _fixture.SeedUserAsync(new UserBuilder().WithLastName("Kowalski").WithConfirmedEmail().Build());
        await _fixture.SeedUserAsync(new UserBuilder().WithLastName("Silva").WithConfirmedEmail().Build());

        var result = await _fixture.UserRepository().ListAllAsync(
            new ListInput(SearchTerm: "Kowalski", PageSize: 50),
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Kowalski", result.Data[0].LastName);
    }

    [Fact]
    public async Task ListAllAsync_ShouldSortByLastNameDescending_WhenSortByLastNameAndDesc()
    {
        await _fixture.SeedUserAsync(new UserBuilder().WithLastName("Adams").WithConfirmedEmail().Build());
        await _fixture.SeedUserAsync(new UserBuilder().WithLastName("Young").WithConfirmedEmail().Build());

        var result = await _fixture.UserRepository().ListAllAsync(
            new ListInput(SortBy: "lastName", SortDirection: "desc", PageSize: 50),
            CancellationToken.None);

        Assert.Equal("Young", result.Data[0].LastName);
        Assert.Equal("Adams", result.Data[1].LastName);
    }

    [Fact]
    public async Task ListAllAsync_ShouldUseCreatedAtDescending_WhenSortByIsNotProvided()
    {
        var older = new UserBuilder().WithEmail("older@test.com").WithConfirmedEmail().Build();
        older.CreatedAt = DateTime.UtcNow.AddDays(-2);
        var newer = new UserBuilder().WithEmail("newer@test.com").WithConfirmedEmail().Build();
        newer.CreatedAt = DateTime.UtcNow.AddDays(-1);

        await _fixture.SeedUserAsync(older);
        await _fixture.SeedUserAsync(newer);

        var result = await _fixture.UserRepository().ListAllAsync(new ListInput(PageSize: 50), CancellationToken.None);

        Assert.Equal("newer@test.com", result.Data[0].Email.Value);
        Assert.Equal("older@test.com", result.Data[1].Email.Value);
    }

    public void Dispose() => _fixture.Dispose();
}
