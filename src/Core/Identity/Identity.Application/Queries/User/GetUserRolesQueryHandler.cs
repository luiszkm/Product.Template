using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Queries.Users;

public class GetUserRolesQueryHandler : IQueryHandler<GetUserRolesQuery, IReadOnlyCollection<string>>
{
    private readonly IUserRepository _userRepository;

    public GetUserRolesQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyCollection<string>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User not Found");

        return user.UserRoles
            .Where(x => x.Role is not null)
            .Select(x => x.Role!.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();
    }
}
