using Product.Template.Core.Ai.Application.Handlers;

namespace UnitTests.Validators;

public class ChatCommandValidatorTests
{
    private readonly ChatCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldFail_WhenMessageIsEmpty()
    {
        var result = await _validator.ValidateAsync(new ChatCommand(""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChatCommand.Message));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenMessageExceedsMaxLength()
    {
        var result = await _validator.ValidateAsync(new ChatCommand(new string('a', 4001)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChatCommand.Message));
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenMessageIsValid()
    {
        var result = await _validator.ValidateAsync(new ChatCommand("How many users are active?"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
