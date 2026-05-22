using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Handlers.UserAssignment;
using Product.Template.Core.Authorization.Application.Handlers.UserAssignment.Commands;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Security;

namespace IntegrationTests.Authorization;

public class AssignUserToRoleCommandHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();
    private readonly FakeSecurityStampService _securityStampService = new();

    private AssignUserToRoleCommandHandler CreateHandler() => new(
        _fixture.UserAssignmentRepository(),
        _fixture.RoleRepository(),
        _fixture.UnitOfWork(),
        _fixture.TenantContext,
        _securityStampService,
        NullLogger<AssignUserToRoleCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldAssignUser_WhenRoleExists()
    {
        var role = await _fixture.SeedRoleAsync("AssignTarget");
        var userId = Guid.NewGuid();

        await CreateHandler().Handle(new AssignUserToRoleCommand(userId, role.Id), CancellationToken.None);

        var assignment = await _fixture.UserAssignmentRepository().GetByUserAndRoleAsync(userId, role.Id);
        Assert.NotNull(assignment);
        Assert.Contains(userId, _securityStampService.RegeneratedUserIds);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRoleDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new AssignUserToRoleCommand(Guid.NewGuid(), Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldBeIdempotent_WhenUserAlreadyAssigned()
    {
        var role = await _fixture.SeedRoleAsync("IdempotentRole");
        var userId = Guid.NewGuid();
        var command = new AssignUserToRoleCommand(userId, role.Id);

        await CreateHandler().Handle(command, CancellationToken.None);
        _securityStampService.RegeneratedUserIds.Clear();
        await CreateHandler().Handle(command, CancellationToken.None);

        var assignments = await _fixture.UserAssignmentRepository().GetByUserIdAsync(userId);
        Assert.Single(assignments);
        Assert.Empty(_securityStampService.RegeneratedUserIds);
    }

    public void Dispose() => _fixture.Dispose();

    private sealed class FakeSecurityStampService : ISecurityStampService
    {
        public List<Guid> RegeneratedUserIds { get; } = [];

        public Task RegenerateAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            RegeneratedUserIds.Add(userId);
            return Task.CompletedTask;
        }

        public Task<bool> ValidateAsync(Guid userId, string stamp, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
