using Kernel.Application.Security;
using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.User;

public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, UserOutput>
{
    private readonly IUserRepository _userRepository;
    private readonly IHashServices _hashServices;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IHashServices hashServices,
        IUnitOfWork unitOfWork,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _hashServices = hashServices;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserOutput> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        
        var userExisits = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (userExisits != null)
        {
            _logger.LogWarning("Tentativa de registro com email já existente: {Email}", request.Email);
            throw new InvalidOperationException("Email já está em uso.");
        }

        var passwordHash = _hashServices.GeneratePasswordHash(request.Password);
        var user = Domain.Entities.User.Create(
            email: request.Email,
            passwordHash: passwordHash,
            firstName: request.FirstName,
            lastName: request.LastName
        );

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);
        _logger.LogInformation("Novo usuário registrado com sucesso: {UserId}", user.Id);
        var output = user.ToOutput();

        return output;
    }
}

