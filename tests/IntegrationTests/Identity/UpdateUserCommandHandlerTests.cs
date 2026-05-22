using CommonTests.Builders;
using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Identity.Application.Handlers.User;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Identity;

public class UpdateUserCommandHandlerTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();

    private UpdateUserCommandHandler CreateHandler() => new(
        _fixture.UserRepository(),
        _fixture.UnitOfWork(),
        NullLogger<UpdateUserCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldUpdateProfile_WhenUserExists()
    {
        var user = await _fixture.SeedUserAsync(
            new UserBuilder()
                .WithEmail("update@test.com")
                .WithFirstName("Old")
                .WithLastName("Name")
                .WithConfirmedEmail()
                .Build());

        var result = await CreateHandler().Handle(
            new UpdateUserCommand(user.Id, "New", "Profile"),
            CancellationToken.None);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal("New", result.FirstName);
        Assert.Equal("Profile", result.LastName);

        var persisted = await _fixture.UserRepository().GetByIdAsync(user.Id);
        Assert.NotNull(persisted);
        Assert.Equal("New", persisted.FirstName);
        Assert.Equal("Profile", persisted.LastName);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new UpdateUserCommand(Guid.NewGuid(), "New", "Profile"),
                CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
