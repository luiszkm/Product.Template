using FluentValidation;
using Product.Template.Core.Tenants.Application.Handlers.Commands;

namespace Product.Template.Core.Tenants.Application.Validators;

public sealed class DeactivateTenantCommandValidator : AbstractValidator<DeactivateTenantCommand>
{
    public DeactivateTenantCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId must be provided.");
    }
}
