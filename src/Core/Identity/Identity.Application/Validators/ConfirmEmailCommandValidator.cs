using FluentValidation;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;

namespace Product.Template.Core.Identity.Application.Validators;

public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
    }
}
