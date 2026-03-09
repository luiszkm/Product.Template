using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Application.Mappers;
using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Queries.Users;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserOutput>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserByIdQueryHandler> _logger;

    public GetUserByIdQueryHandler(
        IUserRepository userRepository,
        ILogger<GetUserByIdQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserOutput> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Buscando usuário por ID: {UserId}", request.UserId);

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException($"User with ID {request.UserId} not found.");

        _logger.LogInformation("Usuário encontrado: {UserId}", user.Id);

        return user.ToOutput();
    }
}
