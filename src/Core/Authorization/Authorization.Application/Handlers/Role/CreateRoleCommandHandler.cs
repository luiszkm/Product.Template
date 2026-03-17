using Microsoft.Extensions.Logging;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Core.Authorization.Application.Mappers;
using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Core.Authorization.Application.Handlers.Role;

public class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, RoleOutput>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreateRoleCommandHandler> _logger;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<CreateRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<RoleOutput> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var existing = await _roleRepository.GetByNameAsync(request.Name.Trim(), cancellationToken);
        if (existing is not null)
            throw new BusinessRuleException($"Role '{request.Name}' already exists.");

        var tenantId = _tenantContext.TenantId ?? 0;
        if (tenantId <= 0)
            throw new BusinessRuleException("Tenant must be resolved before creating roles.");

        var role = Domain.Entities.Role.Create(tenantId, request.Name, request.Description);

        await _roleRepository.AddAsync(role, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation("Role {RoleName} created: {RoleId}", role.Name, role.Id);

        return role.ToOutput();
    }
}
