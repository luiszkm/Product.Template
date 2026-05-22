using CommonTests.Builders;
using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Tenants.Application.Handlers;
using Product.Template.Core.Tenants.Application.Handlers.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Tenants;

public class UpdateTenantCommandHandlerTests : IDisposable
{
    private readonly TenantsHandlerTestFixture _fixture = new();

    private UpdateTenantCommandHandler CreateHandler() => new(
        _fixture.TenantRepository(),
        _fixture.HostUnitOfWork(),
        NullLogger<UpdateTenantCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldUpdateTenant_WhenTenantExists()
    {
        var tenantId = Guid.NewGuid();
        await _fixture.TenantRepository().AddAsync(
            new TenantBuilder()
                .WithTenantId(tenantId)
                .WithTenantKey("update-me")
                .WithDisplayName("Original Name")
                .WithContactEmail("old@test.com")
                .Build());
        await _fixture.HostDbContext.SaveChangesAsync();
        _fixture.HostDbContext.ChangeTracker.Clear();

        var result = await CreateHandler().Handle(
            new UpdateTenantCommand(tenantId, "Updated Name", "new@test.com"),
            CancellationToken.None);

        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal("Updated Name", result.DisplayName);
        Assert.Equal("new@test.com", result.ContactEmail);

        var persisted = await _fixture.TenantRepository().GetByTenantIdAsync(tenantId);
        Assert.NotNull(persisted);
        Assert.Equal("Updated Name", persisted.DisplayName);
        Assert.Equal("new@test.com", persisted.ContactEmail);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenTenantDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new UpdateTenantCommand(Guid.NewGuid(), "Name", null),
                CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
