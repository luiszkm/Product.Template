using Product.Template.Core.Identity.Application.Queries.User;

namespace Product.Template.Core.Identity.Application.Handlers.Auth;

public record AuthTokenOutput(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    UserOutput User
);
