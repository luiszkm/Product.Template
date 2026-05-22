using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Core.Identity.Application.Validators;

namespace UnitTests.Validators;

public class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(
            new UpdateUserCommand(Guid.NewGuid(), "John", "Doe"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenUserIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new UpdateUserCommand(Guid.Empty, "John", "Doe"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateUserCommand.UserId));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenFirstNameIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new UpdateUserCommand(Guid.NewGuid(), "", "Doe"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateUserCommand.FirstName));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenLastNameIsTooShort()
    {
        var result = await _validator.ValidateAsync(
            new UpdateUserCommand(Guid.NewGuid(), "John", "D"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateUserCommand.LastName));
    }
}
