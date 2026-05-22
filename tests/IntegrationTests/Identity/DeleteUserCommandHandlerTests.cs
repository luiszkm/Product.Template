using IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
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
        new StubCurrentUserService(),
        NullLogger<DeleteUserCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldSoftDeleteUser_WhenUserExists()
    {
        var user = await _fixture.SeedUserAsync("delete@test.com");

        await CreateHandler().Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        var notVisible = await _fixture.UserRepository().GetByIdAsync(user.Id);
        Assert.Null(notVisible);

        var softDeleted = await _fixture.DbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        Assert.NotNull(softDeleted);
        Assert.NotNull(softDeleted.DeletedAt);
        Assert.False(softDeleted.IsActive);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeleteUserCommand(Guid.NewGuid()), CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}

internal sealed class StubCurrentUserService : Product.Template.Kernel.Application.Security.ICurrentUserService
{
    public Guid? UserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
    public string? Email => "admin@test.com";
    public string? UserName => Email;
    public bool IsAuthenticated => true;
    public IEnumerable<System.Security.Claims.Claim> Claims => [];
}
