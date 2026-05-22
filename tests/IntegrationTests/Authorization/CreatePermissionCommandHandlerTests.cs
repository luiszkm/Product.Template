using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Handlers.Permission;
using Product.Template.Core.Authorization.Application.Handlers.Permission.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Authorization;

public class CreatePermissionCommandHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    private CreatePermissionCommandHandler CreateHandler() => new(
        _fixture.PermissionRepository(),
        _fixture.UnitOfWork(),
        _fixture.TenantContext,
        NullLogger<CreatePermissionCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldCreatePermission_WhenNameIsUnique()
    {
        var command = new CreatePermissionCommand("users.read", "Read users");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("users.read", result.Name);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenNameAlreadyExists()
    {
        await CreateHandler().Handle(new CreatePermissionCommand("duplicate.perm", ""), CancellationToken.None);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateHandler().Handle(new CreatePermissionCommand("duplicate.perm", ""), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldPersistPermission_WhenCreationSucceeds()
    {
        var command = new CreatePermissionCommand("roles.manage", "Manage roles");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        var persisted = await _fixture.PermissionRepository().GetByIdAsync(result.Id);
        Assert.NotNull(persisted);
        Assert.Equal("roles.manage", persisted.Name);
    }

    public void Dispose() => _fixture.Dispose();
}
