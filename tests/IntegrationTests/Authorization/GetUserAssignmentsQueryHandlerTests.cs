using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Authorization.Application.Handlers.UserAssignment;
using Product.Template.Core.Authorization.Application.Handlers.UserAssignment.Commands;
using Product.Template.Core.Authorization.Application.Queries.UserAssignment;
using Product.Template.Kernel.Application.Security;

namespace IntegrationTests.Authorization;

public class GetUserAssignmentsQueryHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    private AssignUserToRoleCommandHandler CreateAssignHandler() => new(
        _fixture.UserAssignmentRepository(),
        _fixture.RoleRepository(),
        _fixture.UnitOfWork(),
        _fixture.TenantContext,
        new FakeSecurityStampService(),
        NullLogger<AssignUserToRoleCommandHandler>.Instance);

    private GetUserAssignmentsQueryHandler CreateHandler() => new(
        _fixture.UserAssignmentRepository(),
        _fixture.RoleRepository(),
        NullLogger<GetUserAssignmentsQueryHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldReturnAssignedRoles_WhenUserHasAssignments()
    {
        var roleA = await _fixture.SeedRoleAsync("AssignmentRoleA");
        var roleB = await _fixture.SeedRoleAsync("AssignmentRoleB");
        var userId = Guid.NewGuid();
        await CreateAssignHandler().Handle(new AssignUserToRoleCommand(userId, roleA.Id), CancellationToken.None);
        await CreateAssignHandler().Handle(new AssignUserToRoleCommand(userId, roleB.Id), CancellationToken.None);
        _fixture.DbContext.ChangeTracker.Clear();

        var result = await CreateHandler().Handle(new GetUserAssignmentsQuery(userId), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Id == roleA.Id);
        Assert.Contains(result, r => r.Id == roleB.Id);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenUserHasNoAssignments()
    {
        var result = await CreateHandler().Handle(new GetUserAssignmentsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(result);
    }

    public void Dispose() => _fixture.Dispose();

    private sealed class FakeSecurityStampService : ISecurityStampService
    {
        public Task RegenerateAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> ValidateAsync(Guid tenantId, Guid userId, string stamp, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
