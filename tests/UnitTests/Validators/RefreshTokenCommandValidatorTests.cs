using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Application.Validators;

namespace UnitTests.Validators;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(new RefreshTokenCommand("valid-refresh-token"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenRefreshTokenIsEmpty()
    {
        var result = await _validator.ValidateAsync(new RefreshTokenCommand(""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RefreshTokenCommand.RefreshToken));
    }
}
