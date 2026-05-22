using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Core.Authorization.Application.Validators;

namespace UnitTests.Validators;

public class CreateRoleCommandValidatorTests
{
    private readonly CreateRoleCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(new CreateRoleCommand("Manager", "Manages users"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenNameIsEmpty()
    {
        var result = await _validator.ValidateAsync(new CreateRoleCommand("", "Description"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateRoleCommand.Name));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenNameHasInvalidFormat()
    {
        var result = await _validator.ValidateAsync(new CreateRoleCommand("1invalid", "Description"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateRoleCommand.Name));
    }
}
