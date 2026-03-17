using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Identity.Application.Handlers.User;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Identity;

public class DeleteUserCommandHandlerTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();

    private DeleteUserCommandHandler CreateHandler() => new(
        _fixture.UserRepository(),
        _fixture.UnitOfWork(),
        NullLogger<DeleteUserCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldDeleteUser_WhenUserExists()
    {
        var user = await _fixture.SeedUserAsync("delete@test.com");

        await CreateHandler().Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        var deleted = await _fixture.UserRepository().GetByIdAsync(user.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeleteUserCommand(Guid.NewGuid()), CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
