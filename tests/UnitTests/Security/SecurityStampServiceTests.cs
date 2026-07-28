using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Core.Identity.Infrastructure.Security;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Kernel.Domain.SeedWorks;
using Product.Template.Kernel.Domain.SeedWorks;

namespace UnitTests.Security;

public class SecurityStampServiceTests
{
    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public int UpdateCallCount { get; private set; }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(user is not null && user.Id == id ? user : null);

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task AddAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(User entity, CancellationToken cancellationToken = default)
        {
            UpdateCallCount++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<PaginatedListOutput<User>> ListAllAsync(ListInput listInput, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task Commit(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task Rollback(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static SecurityStampService CreateSut(User? user, IMemoryCache cache) =>
        new(new FakeUserRepository(user), new FakeUnitOfWork(), cache, NullLogger<SecurityStampService>.Instance);

    [Fact]
    public async Task RegenerateAsync_ShouldThrowNotFound_WhenUserDoesNotExist()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(null, cache);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.RegenerateAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task RegenerateAsync_ShouldRotateStamp_AndEvictCache()
    {
        var tenantId = Guid.NewGuid();
        var user = User.Create(tenantId, "user@acme.com", "hash", "Jane", "Doe");
        var originalStamp = user.SecurityStamp;
        using var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set($"security_stamp_{tenantId}_{user.Id}", originalStamp);
        var sut = CreateSut(user, cache);

        await sut.RegenerateAsync(tenantId, user.Id, CancellationToken.None);

        Assert.NotEqual(originalStamp, user.SecurityStamp);
        Assert.False(cache.TryGetValue($"security_stamp_{tenantId}_{user.Id}", out string? _));
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnTrue_WhenStampMatchesCachedValue()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set($"security_stamp_{tenantId}_{userId}", "cached-stamp");
        var sut = CreateSut(null, cache);

        var result = await sut.ValidateAsync(tenantId, userId, "cached-stamp", CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(null, cache);

        var result = await sut.ValidateAsync(Guid.NewGuid(), Guid.NewGuid(), "any-stamp", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFalse_WhenUserBelongsToDifferentTenant()
    {
        var tenantId = Guid.NewGuid();
        var user = User.Create(tenantId, "user@acme.com", "hash", "Jane", "Doe");
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(user, cache);

        var result = await sut.ValidateAsync(Guid.NewGuid(), user.Id, user.SecurityStamp, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnTrue_AndCacheStamp_WhenUserExistsAndStampMatches()
    {
        var tenantId = Guid.NewGuid();
        var user = User.Create(tenantId, "user@acme.com", "hash", "Jane", "Doe");
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(user, cache);

        var result = await sut.ValidateAsync(tenantId, user.Id, user.SecurityStamp, CancellationToken.None);

        Assert.True(result);
        Assert.True(cache.TryGetValue($"security_stamp_{tenantId}_{user.Id}", out string? cached));
        Assert.Equal(user.SecurityStamp, cached);
    }
}
