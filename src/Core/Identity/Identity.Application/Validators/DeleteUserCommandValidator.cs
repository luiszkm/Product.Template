using FluentValidation;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;

namespace Product.Template.Core.Identity.Application.Validators;

public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId é obrigatório");
    }
}

