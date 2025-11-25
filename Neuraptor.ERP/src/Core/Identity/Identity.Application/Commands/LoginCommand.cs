using MediatR;
using Neuraptor.ERP.Core.Identity.Application.DTOs;

namespace Neuraptor.ERP.Core.Identity.Application.Commands;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<AuthTokenDto>;
