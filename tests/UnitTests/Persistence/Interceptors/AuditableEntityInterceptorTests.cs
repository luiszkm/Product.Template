using Kernel.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Domain.SeedWorks;

namespace UnitTests.Persistence.Interceptors;

public class AuditableEntityInterceptorTests
{
    private sealed class TestEntity : Entity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestEntity> Items => Set<TestEntity>();
    }

    private sealed class FakeCurrentUserService(string? userName) : ICurrentUserService
    {
        public Guid? UserId => null;
        public string? Email => null;
        public string? UserName => userName;
        public bool IsAuthenticated => userName is not null;
        public IEnumerable<System.Security.Claims.Claim> Claims => [];
    }

    private static TestDbContext CreateContext(ICurrentUserService currentUserService)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor(currentUserService))
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldStampCreatedByAndCreatedAt_ForAddedEntity()
    {
        using var context = CreateContext(new FakeCurrentUserService("jane"));
        var entity = new TestEntity { Name = "acme" };
        context.Items.Add(entity);

        await context.SaveChangesAsync();

        Assert.Equal("jane", entity.CreatedBy);
        Assert.True(entity.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
        Assert.Null(entity.UpdatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldDefaultToSystem_WhenNoCurrentUser()
    {
        using var context = CreateContext(new FakeCurrentUserService(null));
        var entity = new TestEntity { Name = "acme" };
        context.Items.Add(entity);

        await context.SaveChangesAsync();

        Assert.Equal("System", entity.CreatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldStampUpdatedByAndUpdatedAt_ForModifiedEntity()
    {
        using var context = CreateContext(new FakeCurrentUserService("jane"));
        var entity = new TestEntity { Name = "acme" };
        context.Items.Add(entity);
        await context.SaveChangesAsync();

        entity.Name = "acme-updated";
        await context.SaveChangesAsync();

        Assert.Equal("jane", entity.UpdatedBy);
        Assert.NotNull(entity.UpdatedAt);
    }
}
