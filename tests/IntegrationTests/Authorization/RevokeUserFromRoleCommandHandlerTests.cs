using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Handlers.UserAssignment;
using Product.Template.Core.Authorization.Application.Handlers.UserAssignment.Commands;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Security;

namespace IntegrationTests.Authorization;

public class RevokeUserFromRoleCommandHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();
    private readonly FakeSecurityStampService _securityStampService = new();

    private AssignUserToRoleCommandHandler CreateAssignHandler() => new(
        _fixture.UserAssignmentRepository(),
        _fixture.RoleRepository(),
        _fixture.UnitOfWork(),
        _fixture.TenantContext,
        new FakeSecurityStampService(),
        NullLogger<AssignUserToRoleCommandHandler>.Instance);

    private RevokeUserFromRoleCommandHandler CreateHandler() => new(
        _fixture.UserAssignmentRepository(),
        _fixture.UnitOfWork(),
        _fixture.TenantContext,
        _securityStampService,
        NullLogger<RevokeUserFromRoleCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldRevokeUser_WhenAssignmentExists()
    {
        var role = await _fixture.SeedRoleAsync("RevokeTarget");
        var userId = Guid.NewGuid();
        await CreateAssignHandler().Handle(new AssignUserToRoleCommand(userId, role.Id), CancellationToken.None);
        _fixture.DbContext.ChangeTracker.Clear();

        await CreateHandler().Handle(new RevokeUserFromRoleCommand(userId, role.Id), CancellationToken.None);

        var assignment = await _fixture.UserAssignmentRepository().GetByUserAndRoleAsync(userId, role.Id);
        Assert.Null(assignment);
        Assert.Contains(userId, _securityStampService.RegeneratedUserIds);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenAssignmentDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new RevokeUserFromRoleCommand(Guid.NewGuid(), Guid.NewGuid()),
                CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();

    private sealed class FakeSecurityStampService : ISecurityStampService
    {
        public List<Guid> RegeneratedUserIds { get; } = [];

        public Task RegenerateAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
        {
            RegeneratedUserIds.Add(userId);
            return Task.CompletedTask;
        }

        public Task<bool> ValidateAsync(Guid tenantId, Guid userId, string stamp, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
