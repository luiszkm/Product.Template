using FluentValidation;
using Product.Template.Core.Tenants.Application.Handlers.Commands;

namespace Product.Template.Core.Tenants.Application.Validators;

public sealed class DeactivateTenantCommandValidator : AbstractValidator<DeactivateTenantCommand>
{
    public DeactivateTenantCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .GreaterThan(0).WithMessage("TenantId must be a positive number.");
    }
}
