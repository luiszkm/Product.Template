using MediatR;
using Product.Template.Core.Identity.Application.DTOs;

namespace Product.Template.Core.Identity.Application.Commands;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<AuthTokenDto>;
