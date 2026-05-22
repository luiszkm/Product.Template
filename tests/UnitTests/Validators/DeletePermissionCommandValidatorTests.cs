using Product.Template.Core.Authorization.Application.Handlers.Permission.Commands;
using Product.Template.Core.Authorization.Application.Validators;

namespace UnitTests.Validators;

public class DeletePermissionCommandValidatorTests
{
    private readonly DeletePermissionCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(new DeletePermissionCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPermissionIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(new DeletePermissionCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeletePermissionCommand.PermissionId));
    }
}
