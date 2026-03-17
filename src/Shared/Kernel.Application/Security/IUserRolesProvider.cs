namespace Product.Template.Kernel.Application.Security;

public interface IUserRolesProvider
{
    Task<UserRolesData> GetUserRolesAndPermissionsAsync(Guid userId, CancellationToken cancellationToken);
}

public record UserRolesData(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);
