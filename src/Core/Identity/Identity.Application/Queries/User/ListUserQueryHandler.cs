

using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Identity.Application.Queries.User;

public class ListUserQueryHandler : IQueryHandler<ListUserQuery, PaginatedListOutput<UserOutput>>
{
    private readonly IUserRepository _userRepository;
    private ILogger<ListUserQueryHandler> _logger;

    public ListUserQueryHandler(
        IUserRepository userRepository,
        ILogger<ListUserQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<PaginatedListOutput<UserOutput>> Handle(ListUserQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.ListAllAsync(request, cancellationToken);

        var output = new PaginatedListOutput<UserOutput>
        (
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            TotalCount: users.TotalCount,
            Data: users.Data.ToOutputList().ToArray()
        );

        return output;

    }
}
