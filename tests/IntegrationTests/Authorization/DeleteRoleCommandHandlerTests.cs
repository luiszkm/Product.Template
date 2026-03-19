using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Handlers.Role;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Authorization;

public class DeleteRoleCommandHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    private DeleteRoleCommandHandler CreateHandler() => new(
        _fixture.RoleRepository(),
        _fixture.UnitOfWork(),
        NullLogger<DeleteRoleCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldDeleteRole_WhenRoleExists()
    {
        var role = await _fixture.SeedRoleAsync();

        await CreateHandler().Handle(new DeleteRoleCommand(role.Id), CancellationToken.None);

        var deleted = await _fixture.RoleRepository().GetByIdAsync(role.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRoleDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeleteRoleCommand(Guid.NewGuid()), CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
