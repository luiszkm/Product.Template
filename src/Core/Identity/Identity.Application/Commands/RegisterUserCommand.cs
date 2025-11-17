using MediatR;
using Product.Template.Core.Identity.Application.DTOs;

namespace Product.Template.Core.Identity.Application.Commands;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName
) : IRequest<UserDto>;
