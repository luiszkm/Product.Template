using CommonTests.Builders;
using IntegrationTests.Common;
using Product.Template.Core.Authorization.Domain.Entities;
using Product.Template.Core.Authorization.Infrastructure.Data.Persistence;
using Product.Template.Core.Identity.Application.Queries.Users;
using Product.Template.Core.Identity.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace IntegrationTests.Identity;

public class GetUserRolesQueryHandlerTests : IDisposable
{
    private readonly AuthorizationHandlerTestFixture _fixture = new();

    private GetUserRolesQueryHandler CreateHandler() => new(
        new UserRepository(_fixture.DbContext),
        new UserRolesProvider(_fixture.DbContext));

    [Fact]
    public async Task Handle_ShouldReturnRoles_WhenUserHasAssignments()
    {
        var user = new UserBuilder().WithEmail("roles@test.com").WithConfirmedEmail().Build();
        await _fixture.DbContext.Users.AddAsync(user);
        var role = await _fixture.SeedRoleAsync("Editor");
        var assignment = UserAssignment.Create(user.Id, role.Id, WellKnownTenants.Public, role.Name);
        await _fixture.DbContext.Set<UserAssignment>().AddAsync(assignment);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var result = await CreateHandler().Handle(new GetUserRolesQuery(user.Id), CancellationToken.None);

        Assert.Single(result);
        Assert.Contains("Editor", result);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetUserRolesQuery(Guid.NewGuid()), CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
