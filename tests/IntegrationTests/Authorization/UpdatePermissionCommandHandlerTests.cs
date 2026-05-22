using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Handlers.Permission;
using Product.Template.Core.Authorization.Application.Handlers.Permission.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Authorization;

public class UpdatePermissionCommandHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    private UpdatePermissionCommandHandler CreateHandler() => new(
        _fixture.PermissionRepository(),
        _fixture.UnitOfWork(),
        NullLogger<UpdatePermissionCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldUpdatePermission_WhenPermissionExists()
    {
        var permission = await _fixture.SeedPermissionAsync("original.perm");

        var result = await CreateHandler().Handle(
            new UpdatePermissionCommand(permission.Id, "updated.perm", "Updated description"),
            CancellationToken.None);

        Assert.Equal("updated.perm", result.Name);
        Assert.Equal("Updated description", result.Description);
    }

    [Fact]
    public async Task Handle_ShouldPersistChanges_WhenUpdateSucceeds()
    {
        var permission = await _fixture.SeedPermissionAsync("persist.perm");

        await CreateHandler().Handle(
            new UpdatePermissionCommand(permission.Id, "persisted.perm", "Persisted description"),
            CancellationToken.None);

        var persisted = await _fixture.PermissionRepository().GetByIdAsync(permission.Id);
        Assert.NotNull(persisted);
        Assert.Equal("persisted.perm", persisted.Name);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenPermissionDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new UpdatePermissionCommand(Guid.NewGuid(), "missing.perm", "Description"),
                CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
