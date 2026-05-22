using Product.Template.Core.Tenants.Application.Handlers.Commands;
using Product.Template.Core.Tenants.Application.Validators;

namespace UnitTests.Validators;

public class DeactivateTenantCommandValidatorTests
{
    private readonly DeactivateTenantCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(new DeactivateTenantCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenTenantIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(new DeactivateTenantCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeactivateTenantCommand.TenantId));
    }
}
