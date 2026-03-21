namespace Kernel.Application.Security;

public interface IAuthenticationProviderFactory
{
    IAuthenticationProvider GetProvider(string providerName);
    IEnumerable<string> GetAvailableProviders();
    bool IsProviderAvailable(string providerName);
}
