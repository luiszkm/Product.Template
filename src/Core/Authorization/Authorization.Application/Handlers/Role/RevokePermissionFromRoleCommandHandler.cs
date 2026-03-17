using Microsoft.Extensions.Logging;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Handlers.Role;

public class RevokePermissionFromRoleCommandHandler : ICommandHandler<RevokePermissionFromRoleCommand>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevokePermissionFromRoleCommandHandler> _logger;

    public RevokePermissionFromRoleCommandHandler(
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ILogger<RevokePermissionFromRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(RevokePermissionFromRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetWithPermissionsAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException($"Role with ID {request.RoleId} not found.");

        role.RevokePermission(request.PermissionId);

        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation("Permission {PermissionId} revoked from role {RoleId}", request.PermissionId, role.Id);
    }
}
