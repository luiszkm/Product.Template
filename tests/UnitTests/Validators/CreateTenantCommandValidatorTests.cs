using Product.Template.Core.Tenants.Application.Handlers.Commands;
using Product.Template.Core.Tenants.Application.Validators;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace UnitTests.Validators;

public class CreateTenantCommandValidatorTests
{
    private readonly CreateTenantCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(
            new CreateTenantCommand("acme-corp", "Acme Corp", "admin@acme.com", TenantIsolationMode.SharedDb));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenTenantKeyIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new CreateTenantCommand("", "Acme Corp", null, TenantIsolationMode.SharedDb));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantCommand.TenantKey));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenTenantKeyHasInvalidFormat()
    {
        var result = await _validator.ValidateAsync(
            new CreateTenantCommand("Invalid_Key", "Acme Corp", null, TenantIsolationMode.SharedDb));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantCommand.TenantKey));
    }
}
