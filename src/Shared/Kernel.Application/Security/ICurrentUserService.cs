using System.Security.Claims;

namespace Product.Template.Kernel.Application.Security;

public interface ICurrentUserService
{

    Guid? UserId { get; }
    string? Email { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    IEnumerable<Claim> Claims { get; }
}

