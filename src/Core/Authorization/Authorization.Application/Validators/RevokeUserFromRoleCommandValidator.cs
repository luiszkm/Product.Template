using FluentValidation;
using Product.Template.Core.Authorization.Application.Handlers.UserAssignment.Commands;

namespace Product.Template.Core.Authorization.Application.Validators;

public class RevokeUserFromRoleCommandValidator : AbstractValidator<RevokeUserFromRoleCommand>
{
    public RevokeUserFromRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
        RuleFor(x => x.RoleId).NotEmpty().WithMessage("RoleId is required.");
    }
}
