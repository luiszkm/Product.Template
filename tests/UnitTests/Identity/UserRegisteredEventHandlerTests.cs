using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Identity.Application.Handlers.Events;
using Product.Template.Core.Identity.Application.Security;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Core.Identity.Domain.Events;
using Product.Template.Core.Identity.Domain.Repositories;
using Kernel.Domain.SeedWorks;
using Product.Template.Kernel.Domain.SeedWorks;

namespace UnitTests.Identity;

public class UserRegisteredEventHandlerTests
{
    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(user is not null && user.Id == id ? user : null);

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task AddAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<PaginatedListOutput<User>> ListAllAsync(ListInput listInput, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeEmailConfirmationTokenService : IEmailConfirmationTokenService
    {
        public int GenerateTokenCallCount { get; private set; }

        public string GenerateToken(Guid userId, string securityStamp)
        {
            GenerateTokenCallCount++;
            return "token";
        }

        public bool ValidateToken(Guid userId, string securityStamp, string token) => true;
    }

    [Fact]
    public async Task Handle_ShouldGenerateConfirmationToken_WhenUserExists()
    {
        var user = User.Create(Guid.NewGuid(), "user@acme.com", "hash", "Jane", "Doe");
        var tokenService = new FakeEmailConfirmationTokenService();
        var sut = new UserRegisteredEventHandler(
            new FakeUserRepository(user),
            tokenService,
            NullLogger<UserRegisteredEventHandler>.Instance);

        await sut.Handle(new UserRegisteredEvent(user.Id, user.Email.Value), CancellationToken.None);

        Assert.Equal(1, tokenService.GenerateTokenCallCount);
    }

    [Fact]
    public async Task Handle_ShouldDoNothing_WhenUserNoLongerExists()
    {
        var tokenService = new FakeEmailConfirmationTokenService();
        var sut = new UserRegisteredEventHandler(
            new FakeUserRepository(null),
            tokenService,
            NullLogger<UserRegisteredEventHandler>.Instance);

        await sut.Handle(new UserRegisteredEvent(Guid.NewGuid(), "user@acme.com"), CancellationToken.None);

        Assert.Equal(0, tokenService.GenerateTokenCallCount);
    }
}
