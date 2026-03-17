using FluentValidation;
using Product.Template.Core.Authorization.Application.Handlers.Permission.Commands;

namespace Product.Template.Core.Authorization.Application.Validators;

public class CreatePermissionCommandValidator : AbstractValidator<CreatePermissionCommand>
{
    public CreatePermissionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Permission name is required.")
            .MaximumLength(100).WithMessage("Permission name must have at most 100 characters.")
            .Matches(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)*$")
            .WithMessage("Permission name must follow the pattern {module}.{resource}.{action} (e.g. identity.user.read).");

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("Description must have at most 250 characters.");
    }
}
