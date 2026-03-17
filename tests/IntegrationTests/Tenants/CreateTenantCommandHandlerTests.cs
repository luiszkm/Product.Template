using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Tenants.Application.Handlers;
using Product.Template.Core.Tenants.Application.Handlers.Commands;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace IntegrationTests.Tenants;

public class CreateTenantCommandHandlerTests : IDisposable
{
    private readonly TenantsHandlerTestFixture _fixture = new();

    private CreateTenantCommandHandler CreateHandler() => new(
        _fixture.TenantRepository(),
        NullLogger<CreateTenantCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldCreateTenant_WhenKeyIsUnique()
    {
        var command = new CreateTenantCommand(10, "acme", "Acme Corp", "admin@acme.com", TenantIsolationMode.SharedDb);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.Equal("acme", result.TenantKey);
        Assert.Equal("Acme Corp", result.DisplayName);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenKeyAlreadyExists()
    {
        var command = new CreateTenantCommand(20, "duplicate", "Dup", null, TenantIsolationMode.SharedDb);
        await CreateHandler().Handle(command, CancellationToken.None);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateHandler().Handle(
                new CreateTenantCommand(21, "duplicate", "Dup2", null, TenantIsolationMode.SharedDb),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldPersistTenant_WhenCreationSucceeds()
    {
        var command = new CreateTenantCommand(30, "persist-corp", "Persist Corp", null, TenantIsolationMode.SharedDb);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        var persisted = await _fixture.TenantRepository().GetByKeyAsync("persist-corp");
        Assert.NotNull(persisted);
        Assert.Equal("Persist Corp", persisted.DisplayName);
    }

    public void Dispose() => _fixture.Dispose();
}
