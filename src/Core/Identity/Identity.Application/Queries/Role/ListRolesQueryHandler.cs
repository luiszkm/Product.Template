using Product.Template.Core.Identity.Application.Queries.Role.Commands;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Identity.Application.Queries.Role;

public class ListRolesQueryHandler : IQueryHandler<ListRolesQuery, PaginatedListOutput<RoleOutput>>
{
    private readonly IRoleRepository _roleRepository;

    public ListRolesQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<PaginatedListOutput<RoleOutput>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.ListAllAsync(request, cancellationToken);

        return new PaginatedListOutput<RoleOutput>(
            PageNumber: roles.PageNumber,
            PageSize: roles.PageSize,
            TotalCount: roles.TotalCount,
            Data: roles.Data.Select(r => new RoleOutput(r.Id, r.Name, r.Description, r.CreatedAt)).ToList());
    }
}
