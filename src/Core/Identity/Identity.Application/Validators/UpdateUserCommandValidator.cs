using FluentValidation;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;

namespace Product.Template.Core.Identity.Application.Validators;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId é obrigatório");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(2).WithMessage("Nome deve ter no mínimo 2 caracteres")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Sobrenome é obrigatório")
            .MinimumLength(2).WithMessage("Sobrenome deve ter no mínimo 2 caracteres")
            .MaximumLength(100).WithMessage("Sobrenome deve ter no máximo 100 caracteres");
    }
}

