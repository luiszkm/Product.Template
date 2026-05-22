using CommonTests.Builders;
using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Tenants.Application.Queries;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Tenants;

public class GetTenantByIdQueryHandlerTests : IDisposable
{
    private readonly TenantsHandlerTestFixture _fixture = new();

    private GetTenantByIdQueryHandler CreateHandler() => new(
        _fixture.TenantRepository(),
        NullLogger<GetTenantByIdQueryHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldReturnTenant_WhenTenantExists()
    {
        var tenantId = Guid.NewGuid();
        await _fixture.TenantRepository().AddAsync(
            new TenantBuilder()
                .WithTenantId(tenantId)
                .WithTenantKey("found-tenant")
                .WithDisplayName("Found Corp")
                .WithContactEmail("found@test.com")
                .Build());
        await _fixture.HostDbContext.SaveChangesAsync();
        _fixture.HostDbContext.ChangeTracker.Clear();

        var result = await CreateHandler().Handle(new GetTenantByIdQuery(tenantId), CancellationToken.None);

        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal("found-tenant", result.TenantKey);
        Assert.Equal("Found Corp", result.DisplayName);
        Assert.Equal("found@test.com", result.ContactEmail);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenTenantDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetTenantByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
