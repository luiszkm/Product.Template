namespace Kernel.Application.Security;

/// <summary>
/// Factory para gerenciar e criar instâncias de provedores de autenticação
/// </summary>
public interface IAuthenticationProviderFactory
{
    /// <summary>
    /// Obtém um provedor de autenticação pelo nome
    /// </summary>
    /// <param name="providerName">Nome do provedor (ex: "jwt", "microsoft")</param>
    /// <returns>Instância do provedor</returns>
    /// <exception cref="InvalidOperationException">Quando o provedor não é encontrado</exception>
    IAuthenticationProvider GetProvider(string providerName);

    /// <summary>
    /// Retorna a lista de provedores disponíveis
    /// </summary>
    IEnumerable<string> GetAvailableProviders();

    /// <summary>
    /// Verifica se um provedor está disponível
    /// </summary>
    bool IsProviderAvailable(string providerName);
}
