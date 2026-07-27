using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Kernel.Application.Behaviors;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace UnitTests.Behaviors;

public class TenantContextBehaviorTests
{
    private sealed record Ping;

    private sealed class FakeTenantContext : ITenantContext
    {
        public FakeTenantContext(bool isResolved)
        {
            IsResolved = isResolved;
            if (isResolved)
            {
                TenantId = Guid.NewGuid();
                TenantKey = "acme";
                Tenant = new TenantConfig { TenantId = TenantId.Value, TenantKey = TenantKey };
            }
        }

        public Guid? TenantId { get; private set; }
        public string? TenantKey { get; private set; }
        public TenantConfig? Tenant { get; private set; }
        public bool IsResolved { get; }

        public void SetTenant(TenantConfig tenant)
        {
            Tenant = tenant;
            TenantId = tenant.TenantId;
            TenantKey = tenant.TenantKey;
        }
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenTenantIsResolved()
    {
        var behavior = new TenantContextBehavior<Ping, string>(
            new FakeTenantContext(isResolved: true),
            NullLogger<TenantContextBehavior<Ping, string>>.Instance);

        var result = await behavior.Handle(new Ping(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenTenantIsNotResolved()
    {
        var behavior = new TenantContextBehavior<Ping, string>(
            new FakeTenantContext(isResolved: false),
            NullLogger<TenantContextBehavior<Ping, string>>.Instance);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => behavior.Handle(new Ping(), _ => throw new InvalidOperationException("next should not run"), CancellationToken.None));
    }
}
