using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Core.Identity.Domain.Entities;

namespace Product.Template.Core.Identity.Application.Mappers;

public static class UserMapper
{
    public static UserOutput ToOutput(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserOutput(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.EmailConfirmed,
            user.CreatedAt,
            user.LastLoginAt);
    }

    public static IEnumerable<UserOutput> ToOutputList(this IEnumerable<User> users)
    {
        ArgumentNullException.ThrowIfNull(users);

        return users.Select(u => u.ToOutput());
    }
}
