using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Handlers.Role;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Authorization;

public class RevokePermissionFromRoleCommandHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    private AssignPermissionToRoleCommandHandler CreateAssignHandler() => new(
        _fixture.RoleRepository(),
        _fixture.PermissionRepository(),
        _fixture.UnitOfWork(),
        NullLogger<AssignPermissionToRoleCommandHandler>.Instance);

    private RevokePermissionFromRoleCommandHandler CreateHandler() => new(
        _fixture.RoleRepository(),
        _fixture.UnitOfWork(),
        NullLogger<RevokePermissionFromRoleCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldRevokePermission_WhenAssignmentExists()
    {
        var role = await _fixture.SeedRoleAsync();
        var permission = await _fixture.SeedPermissionAsync();
        await CreateAssignHandler().Handle(
            new AssignPermissionToRoleCommand(role.Id, permission.Id),
            CancellationToken.None);
        _fixture.DbContext.ChangeTracker.Clear();

        await CreateHandler().Handle(
            new RevokePermissionFromRoleCommand(role.Id, permission.Id),
            CancellationToken.None);

        var updated = await _fixture.RoleRepository().GetWithPermissionsAsync(role.Id);
        Assert.NotNull(updated);
        Assert.DoesNotContain(updated.RolePermissions, rp => rp.PermissionId == permission.Id);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRoleDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new RevokePermissionFromRoleCommand(Guid.NewGuid(), Guid.NewGuid()),
                CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
