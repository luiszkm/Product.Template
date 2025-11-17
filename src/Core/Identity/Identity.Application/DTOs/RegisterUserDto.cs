namespace Product.Template.Core.Identity.Application.DTOs;

public record RegisterUserDto(
    string Email,
    string Password,
    string FirstName,
    string LastName
);
