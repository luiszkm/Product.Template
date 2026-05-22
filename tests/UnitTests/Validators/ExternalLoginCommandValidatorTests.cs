using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Application.Validators;

namespace UnitTests.Validators;

public class ExternalLoginCommandValidatorTests
{
    private readonly ExternalLoginCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(
            new ExternalLoginCommand("Google", "auth-code", "https://localhost/callback"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenProviderIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new ExternalLoginCommand("", "auth-code"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ExternalLoginCommand.Provider));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenCodeIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new ExternalLoginCommand("Google", ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ExternalLoginCommand.Code));
    }
}
