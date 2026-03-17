using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Application.Security;

namespace Product.Template.Core.Identity.Application.Queries.Users;

public class GetUserRolesQueryHandler : IQueryHandler<GetUserRolesQuery, IReadOnlyCollection<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRolesProvider _userRolesProvider;

    public GetUserRolesQueryHandler(IUserRepository userRepository, IUserRolesProvider userRolesProvider)
    {
        _userRepository = userRepository;
        _userRolesProvider = userRolesProvider;
    }

    public async Task<IReadOnlyCollection<string>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        _ = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException($"User with ID {request.UserId} not found.");

        var rolesData = await _userRolesProvider.GetUserRolesAndPermissionsAsync(request.UserId, cancellationToken);
        return rolesData.Roles.OrderBy(x => x).ToArray();
    }
}
