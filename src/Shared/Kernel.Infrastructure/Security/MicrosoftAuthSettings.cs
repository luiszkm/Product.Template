namespace Kernel.Infrastructure.Security;

/// <summary>
/// Configurações para autenticação com Microsoft (Azure AD / Entra ID)
/// </summary>
public class MicrosoftAuthSettings
{
    /// <summary>
    /// Habilita/desabilita autenticação Microsoft
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Client ID da aplicação registrada no Azure AD
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client Secret da aplicação (usar User Secrets em dev, Key Vault em prod)
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Tenant ID do Azure AD (ou "common" para multi-tenant)
    /// </summary>
    public string TenantId { get; set; } = "common";

    /// <summary>
    /// URI de redirecionamento após autenticação
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Escopos solicitados (separados por espaço)
    /// </summary>
    public string Scopes { get; set; } = "openid profile email";

    /// <summary>
    /// Authority URL (gerada automaticamente se não especificada)
    /// </summary>
    public string Authority => $"https://login.microsoftonline.com/{TenantId}";
}
