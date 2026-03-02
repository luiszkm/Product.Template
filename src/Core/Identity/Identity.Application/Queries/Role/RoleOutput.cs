namespace Product.Template.Core.Identity.Application.Queries.Role;

public record RoleOutput(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt
);
