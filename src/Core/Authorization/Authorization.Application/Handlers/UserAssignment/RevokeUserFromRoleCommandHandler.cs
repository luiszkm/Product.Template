using Microsoft.Extensions.Logging;
using Product.Template.Core.Authorization.Application.Handlers.UserAssignment.Commands;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Handlers.UserAssignment;

public class RevokeUserFromRoleCommandHandler : ICommandHandler<RevokeUserFromRoleCommand>
{
    private readonly IUserAssignmentRepository _assignmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevokeUserFromRoleCommandHandler> _logger;

    public RevokeUserFromRoleCommandHandler(
        IUserAssignmentRepository assignmentRepository,
        IUnitOfWork unitOfWork,
        ILogger<RevokeUserFromRoleCommandHandler> logger)
    {
        _assignmentRepository = assignmentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(RevokeUserFromRoleCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _assignmentRepository.GetByUserAndRoleAsync(request.UserId, request.RoleId, cancellationToken)
            ?? throw new NotFoundException($"User {request.UserId} is not assigned to role {request.RoleId}.");

        await _assignmentRepository.DeleteAsync(assignment, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation("User {UserId} revoked from role {RoleId}", request.UserId, request.RoleId);
    }
}
