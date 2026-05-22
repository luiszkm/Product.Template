using CommonTests.Builders;
using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Tenants.Application.Handlers;
using Product.Template.Core.Tenants.Application.Handlers.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Tenants;

public class DeactivateTenantCommandHandlerTests : IDisposable
{
    private readonly TenantsHandlerTestFixture _fixture = new();

    private DeactivateTenantCommandHandler CreateHandler() => new(
        _fixture.TenantRepository(),
        _fixture.HostUnitOfWork(),
        NullLogger<DeactivateTenantCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldDeactivateTenant_WhenTenantExists()
    {
        var tenantId = Guid.NewGuid();
        await _fixture.TenantRepository().AddAsync(
            new TenantBuilder()
                .WithTenantId(tenantId)
                .WithTenantKey("deactivate-me")
                .Build());
        await _fixture.HostDbContext.SaveChangesAsync();
        _fixture.HostDbContext.ChangeTracker.Clear();

        await CreateHandler().Handle(new DeactivateTenantCommand(tenantId), CancellationToken.None);

        var persisted = await _fixture.TenantRepository().GetByTenantIdAsync(tenantId);
        Assert.NotNull(persisted);
        Assert.False(persisted.IsActive);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenTenantDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeactivateTenantCommand(Guid.NewGuid()), CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
