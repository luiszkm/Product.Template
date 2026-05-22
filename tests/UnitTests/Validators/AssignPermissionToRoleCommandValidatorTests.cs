using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Core.Authorization.Application.Validators;

namespace UnitTests.Validators;

public class AssignPermissionToRoleCommandValidatorTests
{
    private readonly AssignPermissionToRoleCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(
            new AssignPermissionToRoleCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenRoleIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new AssignPermissionToRoleCommand(Guid.Empty, Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AssignPermissionToRoleCommand.RoleId));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPermissionIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new AssignPermissionToRoleCommand(Guid.NewGuid(), Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AssignPermissionToRoleCommand.PermissionId));
    }
}
