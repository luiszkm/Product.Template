using Product.Template.Core.Tenants.Application.Handlers.Commands;
using Product.Template.Core.Tenants.Application.Validators;

namespace UnitTests.Validators;

public class UpdateTenantCommandValidatorTests
{
    private readonly UpdateTenantCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(
            new UpdateTenantCommand(Guid.NewGuid(), "Acme Corp", "admin@acme.com"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenTenantIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new UpdateTenantCommand(Guid.Empty, "Acme Corp", "admin@acme.com"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantCommand.TenantId));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenDisplayNameIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new UpdateTenantCommand(Guid.NewGuid(), "", "admin@acme.com"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantCommand.DisplayName));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenContactEmailIsInvalid()
    {
        var result = await _validator.ValidateAsync(
            new UpdateTenantCommand(Guid.NewGuid(), "Acme Corp", "not-an-email"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantCommand.ContactEmail));
    }
}
