using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Handlers.Role;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Authorization;

public class UpdateRoleCommandHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    private UpdateRoleCommandHandler CreateHandler() => new(
        _fixture.RoleRepository(),
        _fixture.UnitOfWork(),
        NullLogger<UpdateRoleCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldUpdateRole_WhenRoleExists()
    {
        var role = await _fixture.SeedRoleAsync("OriginalName");

        var result = await CreateHandler().Handle(
            new UpdateRoleCommand(role.Id, "UpdatedName", "Updated description"),
            CancellationToken.None);

        Assert.Equal("UpdatedName", result.Name);
        Assert.Equal("Updated description", result.Description);
    }

    [Fact]
    public async Task Handle_ShouldPersistChanges_WhenUpdateSucceeds()
    {
        var role = await _fixture.SeedRoleAsync("PersistRole");

        await CreateHandler().Handle(
            new UpdateRoleCommand(role.Id, "PersistedName", "Persisted description"),
            CancellationToken.None);

        var persisted = await _fixture.RoleRepository().GetByIdAsync(role.Id);
        Assert.NotNull(persisted);
        Assert.Equal("PersistedName", persisted.Name);
        Assert.Equal("Persisted description", persisted.Description);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRoleDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new UpdateRoleCommand(Guid.NewGuid(), "Name", "Description"),
                CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
