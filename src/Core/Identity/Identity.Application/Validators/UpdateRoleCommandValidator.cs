using FluentValidation;
using Product.Template.Core.Identity.Application.Handlers.Role.Commands;

namespace Product.Template.Core.Identity.Application.Validators;

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("RoleId é obrigatório");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome da role é obrigatório")
            .MinimumLength(2).WithMessage("Nome deve ter no mínimo 2 caracteres")
            .MaximumLength(50).WithMessage("Nome deve ter no máximo 50 caracteres")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$").WithMessage("Nome deve começar com letra e conter apenas letras, números, hífens ou underscores");

        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("Descrição deve ter no máximo 255 caracteres");
    }
}

