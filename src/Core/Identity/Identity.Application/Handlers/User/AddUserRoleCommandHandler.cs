using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Core.Identity.Application.Security;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Application.Security;

namespace Product.Template.Core.Identity.Application.Handlers.User;

public class AddUserRoleCommandHandler : ICommandHandler<AddUserRoleCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AddUserRoleCommandHandler> _logger;

    public AddUserRoleCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<AddUserRoleCommandHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task Handle(AddUserRoleCommand request, CancellationToken cancellationToken)
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

        if (user.HasRole(role.Name))
        {
            _logger.LogInformation("User {UserId} already has role {RoleName}.", request.UserId, role.Name);
            RbacMetrics.RoleChangesDenied.Add(1);
            return;
        }

        user.AddRole(role);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation("Role {RoleName} added to user {UserId} by {ActorId}", role.Name, request.UserId, _currentUserService.UserId);
        RbacMetrics.RoleAssignments.Add(1);
    }
}
