using Product.Template.Core.Identity.Application.Queries.User;

namespace Product.Template.Core.Identity.Application.Handlers.Auth;

public record AuthTokenOutput(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    UserAuthOutput User
);


public record UserAuthOutput(
    Guid Id,
    string Email,
    string FirstName,
    DateTime? LastLoginAt,
    IEnumerable<string> Roles
);
