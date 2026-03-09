using FluentValidation;
using Product.Template.Core.Identity.Application.Handlers.Role.Commands;

namespace Product.Template.Core.Identity.Application.Validators;

public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("RoleId é obrigatório");
    }
}

