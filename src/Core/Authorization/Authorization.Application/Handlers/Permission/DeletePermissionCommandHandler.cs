using Microsoft.Extensions.Logging;
using Product.Template.Core.Authorization.Application.Handlers.Permission.Commands;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Handlers.Permission;

public class DeletePermissionCommandHandler : ICommandHandler<DeletePermissionCommand>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeletePermissionCommandHandler> _logger;

    public DeletePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeletePermissionCommandHandler> logger)
    {
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DeletePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken)
            ?? throw new NotFoundException($"Permission with ID {request.PermissionId} not found.");

        await _permissionRepository.DeleteAsync(permission, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation("Permission {PermissionId} deleted", request.PermissionId);
    }
}
