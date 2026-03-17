using Microsoft.Extensions.Logging;
using Product.Template.Core.Authorization.Application.Handlers.Permission.Commands;
using Product.Template.Core.Authorization.Application.Mappers;
using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Core.Authorization.Application.Handlers.Permission;

public class CreatePermissionCommandHandler : ICommandHandler<CreatePermissionCommand, PermissionOutput>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreatePermissionCommandHandler> _logger;

    public CreatePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<CreatePermissionCommandHandler> logger)
    {
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<PermissionOutput> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        var existing = await _permissionRepository.GetByNameAsync(request.Name.Trim(), cancellationToken);
        if (existing is not null)
            throw new BusinessRuleException($"Permission '{request.Name}' already exists.");

        var tenantId = _tenantContext.TenantId ?? 0;
        if (tenantId <= 0)
            throw new BusinessRuleException("Tenant must be resolved before creating permissions.");

        var permission = Domain.Entities.Permission.Create(tenantId, request.Name, request.Description);

        await _permissionRepository.AddAsync(permission, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation("Permission {PermissionName} created: {PermissionId}", permission.Name, permission.Id);

        return permission.ToOutput();
    }
}
