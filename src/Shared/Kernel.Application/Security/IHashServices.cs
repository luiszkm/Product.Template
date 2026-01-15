

namespace Kernel.Application.Security;

public interface IHashServices
{

    string GeneratePasswordHash(string password);
    bool VerifyPassword(string password, string hash);

}
