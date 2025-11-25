namespace Neuraptor.ERP.Core.Identity.Application.DTOs;

public record AuthTokenDto(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    UserDto User
);
