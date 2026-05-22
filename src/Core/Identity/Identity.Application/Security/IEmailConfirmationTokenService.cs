namespace Product.Template.Core.Identity.Application.Security;

public interface IEmailConfirmationTokenService
{
    string GenerateToken(Guid userId, string securityStamp);

    bool ValidateToken(Guid userId, string securityStamp, string token);
}
