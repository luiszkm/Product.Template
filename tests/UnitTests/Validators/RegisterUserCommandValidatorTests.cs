using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Core.Identity.Application.Validators;

namespace UnitTests.Validators;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(
            new RegisterUserCommand("user@test.com", "Password1!", "John", "Doe"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenEmailIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new RegisterUserCommand("", "Password1!", "John", "Doe"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.Email));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPasswordDoesNotMeetComplexity()
    {
        var result = await _validator.ValidateAsync(
            new RegisterUserCommand("user@test.com", "weak", "John", "Doe"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.Password));
    }
}
