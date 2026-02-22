using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Core.Identity.Application.Security;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Application.Security;

namespace Product.Template.Core.Identity.Application.Handlers.User;

public class RemoveUserRoleCommandHandler : ICommandHandler<RemoveUserRoleCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RemoveUserRoleCommandHandler> _logger;

    public RemoveUserRoleCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<RemoveUserRoleCommandHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task Handle(RemoveUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User not Found");

        if (_currentUserService.UserId is Guid actorId)
        {
            var actor = await _userRepository.GetByIdAsync(actorId, cancellationToken)
                ?? throw new NotFoundException("Current user not found");

            if (!actor.HasRole("Admin"))
            {
                RbacMetrics.RoleChangesDenied.Add(1);
                throw new BusinessRuleException("Only Admin can manage user roles.");
            }

            if (actor.TenantId != user.TenantId)
            {
                RbacMetrics.RoleChangesDenied.Add(1);
                throw new BusinessRuleException("Cannot manage roles across different tenants.");
            }
        }

        var roleName = request.RoleName.Trim();
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new BusinessRuleException("Role name is required.");
        }

        var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken)
            ?? throw new NotFoundException($"Role '{roleName}' not found");

        if (!user.HasRole(role.Name))
        {
            _logger.LogInformation("User {UserId} does not have role {RoleName}.", request.UserId, role.Name);
            RbacMetrics.RoleChangesDenied.Add(1);
            return;
        }

        if (user.UserRoles.Count == 1)
        {
            RbacMetrics.RoleChangesDenied.Add(1);
            throw new BusinessRuleException("Cannot remove the last role from a user.");
        }

        user.RemoveRole(role.Id);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation("Role {RoleName} removed from user {UserId} by {ActorId}", role.Name, request.UserId, _currentUserService.UserId);
        RbacMetrics.RoleRevocations.Add(1);
    }
}
