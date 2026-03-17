using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Application.Security;

namespace Product.Template.Core.Identity.Application.Queries.Users;

public class GetUserRolesQueryHandler : IQueryHandler<GetUserRolesQuery, IReadOnlyCollection<string>>
{
    private readonly IUserRolesProvider _userRolesProvider;

    public GetUserRolesQueryHandler(IUserRolesProvider userRolesProvider)
    {
        _userRolesProvider = userRolesProvider;
    }

    public async Task<IReadOnlyCollection<string>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var rolesData = await _userRolesProvider.GetUserRolesAndPermissionsAsync(request.UserId, cancellationToken);
        return rolesData.Roles.OrderBy(x => x).ToArray();
    }
}
