using Microsoft.Extensions.Logging;
using Product.Template.Core.Authorization.Application.Handlers.Permission.Commands;
using Product.Template.Core.Authorization.Application.Mappers;
using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Handlers.Permission;

public class UpdatePermissionCommandHandler : ICommandHandler<UpdatePermissionCommand, PermissionOutput>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePermissionCommandHandler> _logger;

    public UpdatePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdatePermissionCommandHandler> logger)
    {
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PermissionOutput> Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken)
            ?? throw new NotFoundException($"Permission with ID {request.PermissionId} not found.");

        permission.Update(request.Name, request.Description);

        await _permissionRepository.UpdateAsync(permission, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation("Permission {PermissionId} updated", permission.Id);

        return permission.ToOutput();
    }
}
