using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Handlers.Role;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Authorization;

public class AssignPermissionToRoleCommandHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    private AssignPermissionToRoleCommandHandler CreateHandler() => new(
        _fixture.RoleRepository(),
        _fixture.PermissionRepository(),
        _fixture.UnitOfWork(),
        NullLogger<AssignPermissionToRoleCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldAssignPermission_WhenRoleAndPermissionExist()
    {
        var role = await _fixture.SeedRoleAsync();
        var permission = await _fixture.SeedPermissionAsync();

        await CreateHandler().Handle(
            new AssignPermissionToRoleCommand(role.Id, permission.Id),
            CancellationToken.None);

        var updated = await _fixture.RoleRepository().GetWithPermissionsAsync(role.Id);
        Assert.NotNull(updated);
        Assert.Contains(updated.RolePermissions, rp => rp.PermissionId == permission.Id);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRoleDoesNotExist()
    {
        var permission = await _fixture.SeedPermissionAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new AssignPermissionToRoleCommand(Guid.NewGuid(), permission.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenPermissionDoesNotExist()
    {
        var role = await _fixture.SeedRoleAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new AssignPermissionToRoleCommand(role.Id, Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldBeIdempotent_WhenPermissionAlreadyAssigned()
    {
        var role = await _fixture.SeedRoleAsync();
        var permission = await _fixture.SeedPermissionAsync();
        var command = new AssignPermissionToRoleCommand(role.Id, permission.Id);

        await CreateHandler().Handle(command, CancellationToken.None);
        await CreateHandler().Handle(command, CancellationToken.None);

        var updated = await _fixture.RoleRepository().GetWithPermissionsAsync(role.Id);
        Assert.NotNull(updated);
        Assert.Single(updated.RolePermissions.Where(rp => rp.PermissionId == permission.Id));
    }

    public void Dispose() => _fixture.Dispose();
}
