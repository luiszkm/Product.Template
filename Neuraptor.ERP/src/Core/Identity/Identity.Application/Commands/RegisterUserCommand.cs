using MediatR;
using Neuraptor.ERP.Core.Identity.Application.DTOs;

namespace Neuraptor.ERP.Core.Identity.Application.Commands;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName
) : IRequest<UserDto>;
