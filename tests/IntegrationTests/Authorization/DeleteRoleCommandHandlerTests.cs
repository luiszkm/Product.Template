using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Handlers.Role;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Authorization;

public class DeleteRoleCommandHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    private CreateRoleCommandHandler CreateCreateHandler() => new(
        _fixture.RoleRepository(),
        _fixture.UnitOfWork(),
        _fixture.TenantContext,
        NullLogger<CreateRoleCommandHandler>.Instance);

    private DeleteRoleCommandHandler CreateDeleteHandler() => new(
        _fixture.RoleRepository(),
        _fixture.UnitOfWork(),
        NullLogger<DeleteRoleCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldDeleteRole_WhenRoleExists()
    {
        var created = await CreateCreateHandler().Handle(new CreateRoleCommand("ToDelete", ""), CancellationToken.None);

        await CreateDeleteHandler().Handle(new DeleteRoleCommand(created.Id), CancellationToken.None);

        var deleted = await _fixture.RoleRepository().GetByIdAsync(created.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRoleDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateDeleteHandler().Handle(new DeleteRoleCommand(Guid.NewGuid()), CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
