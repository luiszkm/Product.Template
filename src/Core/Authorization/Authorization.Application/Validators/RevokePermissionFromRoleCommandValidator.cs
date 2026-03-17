using FluentValidation;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;

namespace Product.Template.Core.Authorization.Application.Validators;

public class RevokePermissionFromRoleCommandValidator : AbstractValidator<RevokePermissionFromRoleCommand>
{
    public RevokePermissionFromRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty().WithMessage("RoleId is required.");
        RuleFor(x => x.PermissionId).NotEmpty().WithMessage("PermissionId is required.");
    }
}
