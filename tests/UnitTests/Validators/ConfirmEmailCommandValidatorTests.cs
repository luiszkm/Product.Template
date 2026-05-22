using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Core.Identity.Application.Validators;

namespace UnitTests.Validators;

public class ConfirmEmailCommandValidatorTests
{
    private readonly ConfirmEmailCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(
            new ConfirmEmailCommand(Guid.NewGuid(), "confirmation-token"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenUserIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new ConfirmEmailCommand(Guid.Empty, "confirmation-token"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmEmailCommand.UserId));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenTokenIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new ConfirmEmailCommand(Guid.NewGuid(), ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmEmailCommand.Token));
    }
}
