using FluentValidation;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;

namespace Product.Template.Core.Authorization.Application.Validators;

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MinimumLength(2).WithMessage("Role name must have at least 2 characters.")
            .MaximumLength(50).WithMessage("Role name must have at most 50 characters.");
        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("Description must have at most 255 characters.");
    }
}
