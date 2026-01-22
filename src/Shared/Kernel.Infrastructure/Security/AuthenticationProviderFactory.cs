using Kernel.Application.Security;

namespace Kernel.Infrastructure.Security;

/// <summary>
/// Factory que gerencia múltiplos provedores de autenticação
/// </summary>
public sealed class AuthenticationProviderFactory : IAuthenticationProviderFactory
{
    private readonly IEnumerable<IAuthenticationProvider> _providers;

    public AuthenticationProviderFactory(IEnumerable<IAuthenticationProvider> providers)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
    }

    public IAuthenticationProvider GetProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be empty", nameof(providerName));

        var provider = _providers.FirstOrDefault(p =>
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
            throw new InvalidOperationException(
                $"Authentication provider '{providerName}' not found. Available providers: {string.Join(", ", GetAvailableProviders())}");

        return provider;
    }

    public IEnumerable<string> GetAvailableProviders()
        => _providers.Select(p => p.ProviderName).OrderBy(n => n);

    public bool IsProviderAvailable(string providerName)
        => _providers.Any(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
}
