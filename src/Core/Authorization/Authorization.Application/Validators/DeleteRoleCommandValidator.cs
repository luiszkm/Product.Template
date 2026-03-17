using FluentValidation;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;

namespace Product.Template.Core.Authorization.Application.Validators;

public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty().WithMessage("RoleId is required.");
    }
}
