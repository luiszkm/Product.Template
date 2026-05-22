using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Handlers.Role;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Core.Authorization.Application.Queries.Role;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Authorization;

public class GetRoleByIdQueryHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    private AssignPermissionToRoleCommandHandler CreateAssignHandler() => new(
        _fixture.RoleRepository(),
        _fixture.PermissionRepository(),
        _fixture.UnitOfWork(),
        NullLogger<AssignPermissionToRoleCommandHandler>.Instance);

    private GetRoleByIdQueryHandler CreateHandler() => new(
        _fixture.RoleRepository(),
        NullLogger<GetRoleByIdQueryHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldReturnRoleWithPermissions_WhenRoleExists()
    {
        var role = await _fixture.SeedRoleAsync("QueryRole");
        var permission = await _fixture.SeedPermissionAsync("query.perm");
        await CreateAssignHandler().Handle(
            new AssignPermissionToRoleCommand(role.Id, permission.Id),
            CancellationToken.None);
        _fixture.DbContext.ChangeTracker.Clear();

        var result = await CreateHandler().Handle(new GetRoleByIdQuery(role.Id), CancellationToken.None);

        Assert.Equal(role.Id, result.Id);
        Assert.Equal("QueryRole", result.Name);
        Assert.Single(result.Permissions);
        Assert.Equal(permission.Id, result.Permissions.First().Id);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRoleDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetRoleByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
