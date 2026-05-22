using CommonTests.Builders;
using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Identity.Application.Queries.User;

namespace IntegrationTests.Identity;

public class ListUserQueryHandlerTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();

    private ListUserQueryHandler CreateHandler() => new(
        _fixture.UserRepository(),
        NullLogger<ListUserQueryHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldReturnPaginatedUsers_WhenUsersExist()
    {
        await _fixture.SeedManyUsersAsync(3);

        var result = await CreateHandler().Handle(new ListUserQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Data.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoUsersExist()
    {
        var result = await CreateHandler().Handle(new ListUserQuery(), CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm_WhenEmailMatches()
    {
        await _fixture.SeedUserAsync(new UserBuilder().WithEmail("unique-find@test.com").WithConfirmedEmail().Build());
        await _fixture.SeedUserAsync(new UserBuilder().WithEmail("other@test.com").WithConfirmedEmail().Build());

        var result = await CreateHandler().Handle(
            new ListUserQuery { SearchTerm = "unique-find", PageSize = 50 },
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("unique-find@test.com", result.Data[0].Email);
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm_WhenFirstNameMatches()
    {
        await _fixture.SeedUserAsync(new UserBuilder().WithFirstName("Zelda").WithConfirmedEmail().Build());
        await _fixture.SeedUserAsync(new UserBuilder().WithFirstName("Mario").WithConfirmedEmail().Build());

        var result = await CreateHandler().Handle(
            new ListUserQuery { SearchTerm = "Zelda", PageSize = 50 },
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Zelda", result.Data[0].FirstName);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenSearchTermMatchesNothing()
    {
        await _fixture.SeedManyUsersAsync(2);

        var result = await CreateHandler().Handle(
            new ListUserQuery { SearchTerm = "no-match-xyz-999" },
            CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task Handle_ShouldPaginate_WhenPageSizeIsSmallerThanTotal()
    {
        await _fixture.SeedManyUsersAsync(5);

        var page1 = await CreateHandler().Handle(new ListUserQuery { PageNumber = 1, PageSize = 2 }, CancellationToken.None);
        var page2 = await CreateHandler().Handle(new ListUserQuery { PageNumber = 2, PageSize = 2 }, CancellationToken.None);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Data.Count);
        Assert.Equal(2, page2.Data.Count);
        Assert.NotEqual(page1.Data[0].Id, page2.Data[0].Id);
    }

    [Fact]
    public async Task Handle_ShouldSortByEmailAscending_WhenSortByEmailAndAsc()
    {
        await _fixture.SeedUserAsync(new UserBuilder().WithEmail("zulu@test.com").WithConfirmedEmail().Build());
        await _fixture.SeedUserAsync(new UserBuilder().WithEmail("alpha@test.com").WithConfirmedEmail().Build());

        var result = await CreateHandler().Handle(
            new ListUserQuery { SortBy = "email", SortDirection = "asc", PageSize = 50 },
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("alpha@test.com", result.Data[0].Email);
        Assert.Equal("zulu@test.com", result.Data[1].Email);
    }

    [Fact]
    public async Task Handle_ShouldExcludeSoftDeletedUsers_WhenUserWasDeleted()
    {
        var user = await _fixture.SeedUserAsync("deleted-list@test.com");
        user.SoftDelete("test");
        _fixture.DbContext.Users.Update(user);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var result = await CreateHandler().Handle(new ListUserQuery { PageSize = 50 }, CancellationToken.None);

        Assert.DoesNotContain(result.Data, u => u.Id == user.Id);
    }

    public void Dispose() => _fixture.Dispose();
}
