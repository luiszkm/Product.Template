using FluentValidation;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;

namespace Product.Template.Core.Identity.Application.Validators;

public class RemoveUserRoleCommandValidator : AbstractValidator<RemoveUserRoleCommand>
{
    public RemoveUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId é obrigatório");

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Nome da role é obrigatório")
            .MaximumLength(50).WithMessage("Nome da role deve ter no máximo 50 caracteres");
    }
}

