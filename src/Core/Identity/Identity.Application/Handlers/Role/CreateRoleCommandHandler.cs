using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Application.Handlers.Role.Commands;
using Product.Template.Core.Identity.Application.Mappers;
using Product.Template.Core.Identity.Application.Queries.Role;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.Role;

public class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, RoleOutput>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateRoleCommandHandler> _logger;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RoleOutput> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var existing = await _roleRepository.GetByNameAsync(request.Name.Trim(), cancellationToken);
        if (existing is not null)
        {
            _logger.LogWarning("Tentativa de criação de role duplicada: {RoleName}", request.Name);
            throw new BusinessRuleException($"Role '{request.Name}' already exists.");
        }

        var role = Domain.Entities.Role.Create(request.Name, request.Description);

        await _roleRepository.AddAsync(role, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation("Role {RoleName} criada com sucesso: {RoleId}", role.Name, role.Id);

        return role.ToOutput();
    }
}
