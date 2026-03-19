using CommonTests.Builders;
using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Core.Identity.Application.Queries.Users;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Identity;

public class GetUserByIdQueryHandlerTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();

    private GetUserByIdQueryHandler CreateHandler() => new(
        _fixture.UserRepository(),
        NullLogger<GetUserByIdQueryHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldReturnUser_WhenUserExists()
    {
        var user = await _fixture.SeedUserAsync(
            new UserBuilder()
                .WithEmail("found@test.com")
                .WithFirstName("John")
                .WithLastName("Doe")
                .WithConfirmedEmail()
                .Build());

        var result = await CreateHandler().Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal("found@test.com", result.Email);
        Assert.Equal("John", result.FirstName);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
