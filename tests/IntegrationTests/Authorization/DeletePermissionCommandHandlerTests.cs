using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Handlers.Permission;
using Product.Template.Core.Authorization.Application.Handlers.Permission.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Authorization;

public class DeletePermissionCommandHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    private DeletePermissionCommandHandler CreateHandler() => new(
        _fixture.PermissionRepository(),
        _fixture.UnitOfWork(),
        NullLogger<DeletePermissionCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldDeletePermission_WhenPermissionExists()
    {
        var permission = await _fixture.SeedPermissionAsync();

        await CreateHandler().Handle(new DeletePermissionCommand(permission.Id), CancellationToken.None);

        var deleted = await _fixture.PermissionRepository().GetByIdAsync(permission.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenPermissionDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeletePermissionCommand(Guid.NewGuid()), CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
