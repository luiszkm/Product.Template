namespace Neuraptor.ERP.Core.Identity.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool EmailConfirmed,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    IEnumerable<string> Roles
);
