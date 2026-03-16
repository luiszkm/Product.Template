using FluentValidation;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;

namespace Product.Template.Core.Identity.Application.Validators;

public sealed class ExternalLoginCommandValidator : AbstractValidator<ExternalLoginCommand>
{
    public ExternalLoginCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Code)
            .NotEmpty();

        RuleFor(x => x.RedirectUri)
            .MaximumLength(2048)
            .When(x => !string.IsNullOrWhiteSpace(x.RedirectUri));
    }
}
