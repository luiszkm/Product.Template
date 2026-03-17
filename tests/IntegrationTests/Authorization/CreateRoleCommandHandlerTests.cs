using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Handlers.Role;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Authorization;

public class CreateRoleCommandHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    private CreateRoleCommandHandler CreateHandler() => new(
        _fixture.RoleRepository(),
        _fixture.UnitOfWork(),
        _fixture.TenantContext,
        NullLogger<CreateRoleCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldCreateRole_WhenNameIsUnique()
    {
        var command = new CreateRoleCommand("Manager", "Manages team resources");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Manager", result.Name);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenNameAlreadyExists()
    {
        await CreateHandler().Handle(new CreateRoleCommand("Duplicate", ""), CancellationToken.None);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateHandler().Handle(new CreateRoleCommand("Duplicate", ""), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldPersistRole_WhenCreationSucceeds()
    {
        var command = new CreateRoleCommand("Auditor", "Read-only access");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        var persisted = await _fixture.RoleRepository().GetByIdAsync(result.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Auditor", persisted.Name);
    }

    public void Dispose() => _fixture.Dispose();
}
