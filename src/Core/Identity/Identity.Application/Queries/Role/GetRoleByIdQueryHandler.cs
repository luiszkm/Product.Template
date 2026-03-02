using Product.Template.Core.Identity.Application.Queries.Role.Commands;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Queries.Role;

public class GetRoleByIdQueryHandler : IQueryHandler<GetRoleByIdQuery, RoleOutput>
{
    private readonly IRoleRepository _roleRepository;

    public GetRoleByIdQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<RoleOutput> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role not found");

        return new RoleOutput(role.Id, role.Name, role.Description, role.CreatedAt);
    }
}
