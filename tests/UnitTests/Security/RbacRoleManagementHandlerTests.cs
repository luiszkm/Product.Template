using Kernel.Domain.SeedWorks;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Identity.Application.Handlers.User;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Domain.SeedWorks;

namespace UnitTests.Security;

public class RbacRoleManagementHandlerTests
{
    [Fact]
    public async Task AddUserRole_ShouldBeIdempotent_WhenUserAlreadyHasRole()
    {
        var role = Role.Create("Admin", "Administrator");
        var actor = User.Create("actor@test.com", "hash", "Actor", "Admin");
        actor.AddRole(role);
        actor.TenantId = 1;

        var user = User.Create("admin@test.com", "hash", "Admin", "User");
        user.AddRole(role);
        user.TenantId = 1;

        var userRepository = new FakeUserRepository(actor, user);
        var roleRepository = new FakeRoleRepository(role);
        var unitOfWork = new FakeUnitOfWork();

        var handler = new AddUserRoleCommandHandler(
            userRepository,
            roleRepository,
            unitOfWork,
            new FakeCurrentUserService(actor.Id, "Admin"),
            NullLogger<AddUserRoleCommandHandler>.Instance);

        await handler.Handle(new AddUserRoleCommand(user.Id, "Admin"), CancellationToken.None);

        Assert.Equal(0, unitOfWork.CommitCalls);
    }

    [Fact]
    public async Task RemoveUserRole_ShouldThrowBusinessRule_WhenRemovingLastRole()
    {
        var adminRole = Role.Create("Admin", "Administrator");
        var userRole = Role.Create("User", "Default role");

        var actor = User.Create("actor@test.com", "hash", "Actor", "Admin");
        actor.AddRole(adminRole);
        actor.TenantId = 1;

        var user = User.Create("user@test.com", "hash", "Normal", "User");
        user.AddRole(userRole);
        user.TenantId = 1;

        var handler = new RemoveUserRoleCommandHandler(
            new FakeUserRepository(actor, user),
            new FakeRoleRepository(adminRole, userRole),
            new FakeUnitOfWork(),
            new FakeCurrentUserService(actor.Id, "Admin"),
            NullLogger<RemoveUserRoleCommandHandler>.Instance);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new RemoveUserRoleCommand(user.Id, "User"), CancellationToken.None));
    }

    private sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(users.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(users.FirstOrDefault(u => string.Equals(u.Email.Value, email, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<PaginatedListOutput<User>> ListAllAsync(ListInput listInput, CancellationToken cancellationToken = default)
            => Task.FromResult(new PaginatedListOutput<User>(1, 10, users.Length, users));
    }

    private sealed class FakeRoleRepository(params Role[] roles) : IRoleRepository
    {
        public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult(roles.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)));

        public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(roles.FirstOrDefault(r => r.Id == id));

        public Task AddAsync(Role entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Role entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Role entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<PaginatedListOutput<Role>> ListAllAsync(ListInput listInput, CancellationToken cancellationToken = default)
            => Task.FromResult(new PaginatedListOutput<Role>(1, 10, roles.Length, roles));
    }

    private sealed class FakeCurrentUserService(Guid? userId, params string[] roles) : ICurrentUserService
    {
        public Guid? UserId => userId;
        public string? Email => "test@user";
        public string? UserName => "tester";
        public bool IsAuthenticated => userId.HasValue;
        public IEnumerable<System.Security.Claims.Claim> Claims
            => roles.Select(r => new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, r));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int CommitCalls { get; private set; }

        public Task Commit(CancellationToken cancellationToken)
        {
            CommitCalls++;
            return Task.CompletedTask;
        }

        public Task Rollback(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
