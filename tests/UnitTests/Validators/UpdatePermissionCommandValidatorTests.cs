using Product.Template.Core.Authorization.Application.Handlers.Permission.Commands;
using Product.Template.Core.Authorization.Application.Validators;

namespace UnitTests.Validators;

public class UpdatePermissionCommandValidatorTests
{
    private readonly UpdatePermissionCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(
            new UpdatePermissionCommand(Guid.NewGuid(), "identity.user.read", "Read users"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPermissionIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new UpdatePermissionCommand(Guid.Empty, "identity.user.read", "Read users"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePermissionCommand.PermissionId));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenNameIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new UpdatePermissionCommand(Guid.NewGuid(), "", "Read users"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePermissionCommand.Name));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenNameHasInvalidFormat()
    {
        var result = await _validator.ValidateAsync(
            new UpdatePermissionCommand(Guid.NewGuid(), "InvalidPermission", "Read users"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePermissionCommand.Name));
    }
}
