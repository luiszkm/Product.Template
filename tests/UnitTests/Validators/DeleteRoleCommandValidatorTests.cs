using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Core.Authorization.Application.Validators;

namespace UnitTests.Validators;

public class DeleteRoleCommandValidatorTests
{
    private readonly DeleteRoleCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(new DeleteRoleCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenRoleIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(new DeleteRoleCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteRoleCommand.RoleId));
    }
}
