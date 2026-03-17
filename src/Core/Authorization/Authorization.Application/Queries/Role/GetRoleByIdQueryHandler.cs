using Microsoft.Extensions.Logging;
using Product.Template.Core.Authorization.Application.Mappers;
using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Queries.Role;

public class GetRoleByIdQueryHandler : IQueryHandler<GetRoleByIdQuery, RoleWithPermissionsOutput>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<GetRoleByIdQueryHandler> _logger;

    public GetRoleByIdQueryHandler(IRoleRepository roleRepository, ILogger<GetRoleByIdQueryHandler> logger)
    {
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<RoleWithPermissionsOutput> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching role by ID: {RoleId}", request.RoleId);

        var role = await _roleRepository.GetWithPermissionsAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException($"Role with ID {request.RoleId} not found.");

        return role.ToOutputWithPermissions();
    }
}
