using Product.Template.Core.Authorization.Application.Handlers.UserAssignment.Commands;
using Product.Template.Core.Authorization.Application.Validators;

namespace UnitTests.Validators;

public class RevokeUserFromRoleCommandValidatorTests
{
    private readonly RevokeUserFromRoleCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(
            new RevokeUserFromRoleCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenUserIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new RevokeUserFromRoleCommand(Guid.Empty, Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RevokeUserFromRoleCommand.UserId));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenRoleIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new RevokeUserFromRoleCommand(Guid.NewGuid(), Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RevokeUserFromRoleCommand.RoleId));
    }
}
