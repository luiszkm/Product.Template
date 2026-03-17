using Microsoft.Extensions.Logging;
using Product.Template.Core.Authorization.Application.Mappers;
using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Authorization.Application.Queries.Role;

public class ListRolesQueryHandler : IQueryHandler<ListRolesQuery, PaginatedListOutput<RoleOutput>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<ListRolesQueryHandler> _logger;

    public ListRolesQueryHandler(IRoleRepository roleRepository, ILogger<ListRolesQueryHandler> logger)
    {
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<PaginatedListOutput<RoleOutput>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing roles — page {PageNumber}, size {PageSize}", request.PageNumber, request.PageSize);

        var roles = await _roleRepository.ListAllAsync(request, cancellationToken);

        return new PaginatedListOutput<RoleOutput>(
            PageNumber: roles.PageNumber,
            PageSize: roles.PageSize,
            TotalCount: roles.TotalCount,
            Data: roles.Data.ToOutputList().ToList());
    }
}
