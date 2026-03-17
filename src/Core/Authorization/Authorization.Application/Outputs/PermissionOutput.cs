namespace Product.Template.Core.Authorization.Application.Outputs;

public record PermissionOutput(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt
);
