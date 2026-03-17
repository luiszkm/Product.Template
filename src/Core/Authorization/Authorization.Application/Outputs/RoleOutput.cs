namespace Product.Template.Core.Authorization.Application.Outputs;

public record RoleOutput(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt
);

public record RoleWithPermissionsOutput(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    IEnumerable<PermissionOutput> Permissions
);
