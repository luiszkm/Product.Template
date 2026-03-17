using Microsoft.Extensions.Logging;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Handlers.Role;

public class AssignPermissionToRoleCommandHandler : ICommandHandler<AssignPermissionToRoleCommand>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignPermissionToRoleCommandHandler> _logger;

    public AssignPermissionToRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        ILogger<AssignPermissionToRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(AssignPermissionToRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetWithPermissionsAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException($"Role with ID {request.RoleId} not found.");

        var permission = await _permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken)
            ?? throw new NotFoundException($"Permission with ID {request.PermissionId} not found.");

        role.AssignPermission(permission.Id);

        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation("Permission {PermissionId} assigned to role {RoleId}", permission.Id, role.Id);
    }
}
