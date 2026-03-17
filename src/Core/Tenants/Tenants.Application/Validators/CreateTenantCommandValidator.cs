using FluentValidation;
using Product.Template.Core.Tenants.Application.Handlers.Commands;

namespace Product.Template.Core.Tenants.Application.Validators;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.TenantKey)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-z0-9\-]+$")
            .WithMessage("TenantKey must contain only lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.ContactEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));
    }
}
