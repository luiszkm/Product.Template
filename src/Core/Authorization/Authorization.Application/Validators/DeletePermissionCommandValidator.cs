using FluentValidation;
using Product.Template.Core.Authorization.Application.Handlers.Permission.Commands;

namespace Product.Template.Core.Authorization.Application.Validators;

public class DeletePermissionCommandValidator : AbstractValidator<DeletePermissionCommand>
{
    public DeletePermissionCommandValidator()
    {
        RuleFor(x => x.PermissionId).NotEmpty().WithMessage("PermissionId is required.");
    }
}
