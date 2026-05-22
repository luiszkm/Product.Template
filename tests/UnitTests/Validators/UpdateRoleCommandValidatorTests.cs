using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Core.Authorization.Application.Validators;

namespace UnitTests.Validators;

public class UpdateRoleCommandValidatorTests
{
    private readonly UpdateRoleCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(
            new UpdateRoleCommand(Guid.NewGuid(), "Manager", "Manages users"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenRoleIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new UpdateRoleCommand(Guid.Empty, "Manager", "Manages users"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateRoleCommand.RoleId));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenNameIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new UpdateRoleCommand(Guid.NewGuid(), "", "Manages users"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateRoleCommand.Name));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenNameIsTooShort()
    {
        var result = await _validator.ValidateAsync(
            new UpdateRoleCommand(Guid.NewGuid(), "A", "Manages users"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateRoleCommand.Name));
    }
}
