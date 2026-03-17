using Microsoft.Extensions.Logging;
using Product.Template.Core.Authorization.Application.Mappers;
using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Authorization.Application.Queries.Permission;

public class ListPermissionsQueryHandler : IQueryHandler<ListPermissionsQuery, PaginatedListOutput<PermissionOutput>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogger<ListPermissionsQueryHandler> _logger;

    public ListPermissionsQueryHandler(IPermissionRepository permissionRepository, ILogger<ListPermissionsQueryHandler> logger)
    {
        _permissionRepository = permissionRepository;
        _logger = logger;
    }

    public async Task<PaginatedListOutput<PermissionOutput>> Handle(ListPermissionsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing permissions — page {PageNumber}, size {PageSize}", request.PageNumber, request.PageSize);

        var permissions = await _permissionRepository.ListAllAsync(request, cancellationToken);

        return new PaginatedListOutput<PermissionOutput>(
            PageNumber: permissions.PageNumber,
            PageSize: permissions.PageSize,
            TotalCount: permissions.TotalCount,
            Data: permissions.Data.ToOutputList().ToList());
    }
}
