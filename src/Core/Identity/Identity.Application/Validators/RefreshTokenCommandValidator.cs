using FluentValidation;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;

namespace Product.Template.Core.Identity.Application.Validators;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token é obrigatório.");
    }
}

