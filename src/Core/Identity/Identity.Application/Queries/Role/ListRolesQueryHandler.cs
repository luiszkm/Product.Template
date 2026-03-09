using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Application.Mappers;
using Product.Template.Core.Identity.Application.Queries.Role.Commands;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Identity.Application.Queries.Role;

public class ListRolesQueryHandler : IQueryHandler<ListRolesQuery, PaginatedListOutput<RoleOutput>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<ListRolesQueryHandler> _logger;

    public ListRolesQueryHandler(
        IRoleRepository roleRepository,
        ILogger<ListRolesQueryHandler> logger)
    {
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<PaginatedListOutput<RoleOutput>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listando roles — página {PageNumber}, tamanho {PageSize}", request.PageNumber, request.PageSize);

        var roles = await _roleRepository.ListAllAsync(request, cancellationToken);

        return new PaginatedListOutput<RoleOutput>(
            PageNumber: roles.PageNumber,
            PageSize: roles.PageSize,
            TotalCount: roles.TotalCount,
            Data: roles.Data.ToOutputList().ToList());
    }
}
