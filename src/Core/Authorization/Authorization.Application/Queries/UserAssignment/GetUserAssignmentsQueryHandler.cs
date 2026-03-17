using Microsoft.Extensions.Logging;
using Product.Template.Core.Authorization.Application.Mappers;
using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Queries.UserAssignment;

public class GetUserAssignmentsQueryHandler : IQueryHandler<GetUserAssignmentsQuery, IReadOnlyList<RoleOutput>>
{
    private readonly IUserAssignmentRepository _assignmentRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<GetUserAssignmentsQueryHandler> _logger;

    public GetUserAssignmentsQueryHandler(
        IUserAssignmentRepository assignmentRepository,
        IRoleRepository roleRepository,
        ILogger<GetUserAssignmentsQueryHandler> logger)
    {
        _assignmentRepository = assignmentRepository;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RoleOutput>> Handle(GetUserAssignmentsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching role assignments for user {UserId}", request.UserId);

        var assignments = await _assignmentRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        var roleIds = assignments.Select(a => a.RoleId).Distinct().ToList();
        var roles = new List<Domain.Entities.Role>();

        foreach (var roleId in roleIds)
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role is not null)
                roles.Add(role);
        }

        return roles.ToOutputList().ToList();
    }
}
