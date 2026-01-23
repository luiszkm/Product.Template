# 🔐 Guia de Extensibilidade de Autenticação

Este documento explica como adicionar novos métodos de autenticação ao sistema de forma simples e extensível.

## 📋 Índice

1. [Arquitetura](#arquitetura)
2. [Provedores Existentes](#provedores-existentes)
3. [Como Adicionar um Novo Provider](#como-adicionar-um-novo-provider)
4. [Configuração Microsoft](#configuração-microsoft)
5. [Exemplos de Uso](#exemplos-de-uso)

---

## 🏗️ Arquitetura

O sistema de autenticação foi projetado para ser **extensível e plugável**, seguindo o padrão **Strategy** combinado com **Factory**.

```
┌─────────────────────────────────────────────┐
│   IAuthenticationProviderFactory           │
│   ┌───────────────────────────────────┐    │
│   │ GetProvider(name)                 │    │
│   │ GetAvailableProviders()           │    │
│   │ IsProviderAvailable(name)         │    │
│   └───────────────────────────────────┘    │
└─────────────────────────────────────────────┘
                    │
                    │ cria
                    ▼
┌─────────────────────────────────────────────┐
│      IAuthenticationProvider                │
│   ┌───────────────────────────────────┐    │
│   │ ProviderName: string              │    │
│   │ AuthenticateAsync(request)        │    │
│   │ ValidateTokenAsync(token)         │    │
│   └───────────────────────────────────┘    │
└─────────────────────────────────────────────┘
                    △
                    │ implementam
        ┌───────────┴───────────┐
        │                       │
┌───────────────┐      ┌────────────────────┐
│ JwtProvider   │      │ MicrosoftProvider  │
└───────────────┘      └────────────────────┘
```

---

## ✅ Provedores Existentes

| Provider       | Status | Descrição                                    |
|----------------|--------|----------------------------------------------|
| **jwt**        | ✅ Ativo | Autenticação tradicional username/password  |
| **microsoft**  | ✅ Ativo | OAuth2 via Microsoft / Azure AD / Entra ID  |
| **google**     | 🚧 Futuro | OAuth2 via Google                          |
| **github**     | 🚧 Futuro | OAuth2 via GitHub                          |

---

## 🚀 Como Adicionar um Novo Provider

### Passo 1: Criar a classe do Provider

Crie uma nova classe em `src/Shared/Kernel.Infrastructure/Security/Providers/`:

```csharp
using Kernel.Application.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kernel.Infrastructure.Security.Providers;

public sealed class GoogleAuthenticationProvider : IAuthenticationProvider
{
    private readonly GoogleAuthSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleAuthenticationProvider> _logger;

    // Nome único do provider
    public string ProviderName => "google";

    public GoogleAuthenticationProvider(
        IOptions<GoogleAuthSettings> options,
        HttpClient httpClient,
        ILogger<GoogleAuthenticationProvider> logger)
    {
        _settings = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Obter código de autorização
        if (!request.Credentials.TryGetValue("code", out var code))
        {
            return new AuthenticationResult(
                false,
                Error: "Authorization code é obrigatório");
        }

        // 2. Trocar código por access token
        var tokenResponse = await ExchangeCodeForTokenAsync(code, cancellationToken);

        // 3. Obter informações do usuário
        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, cancellationToken);

        // 4. Retornar resultado
        return new AuthenticationResult(
            Success: true,
            AccessToken: tokenResponse.AccessToken,
            UserInfo: new Dictionary<string, string>
            {
                ["email"] = userInfo.Email,
                ["name"] = userInfo.Name,
                ["provider"] = ProviderName
            });
    }

    public async Task<bool> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        // Validar token com API do Google
        return true;
    }

    // Métodos auxiliares...
}
```

### Passo 2: Criar Settings

Crie uma classe de configuração em `src/Shared/Kernel.Infrastructure/Security/`:

```csharp
namespace Kernel.Infrastructure.Security;

public class GoogleAuthSettings
{
    public bool Enabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scopes { get; set; } = "openid profile email";
}
```

### Passo 3: Registrar no DI

Adicione em `src/Shared/Kernel.Infrastructure/Configurations/AuthenticationConfiguration.cs`:

```csharp
// ==================== Google Provider ====================
var googleAuthEnabled = configuration.GetValue<bool>("GoogleAuth:Enabled");
if (googleAuthEnabled)
{
    services.AddOptions<GoogleAuthSettings>()
        .Bind(configuration.GetSection("GoogleAuth"))
        .ValidateOnStart();

    services.AddHttpClient<GoogleAuthenticationProvider>();
    services.AddScoped<IAuthenticationProvider, GoogleAuthenticationProvider>();
}
```

### Passo 4: Adicionar Configuração

Adicione em `appsettings.json`:

```json
{
  "GoogleAuth": {
    "Enabled": true,
    "ClientId": "your-google-client-id",
    "ClientSecret": "your-google-client-secret",
    "RedirectUri": "https://localhost:7254/api/v1/identity/external-callback",
    "Scopes": "openid profile email"
  }
}
```

### Passo 5: Configurar Scalar (Opcional)

Para adicionar botão de OAuth2 no Scalar, atualize `ControllersConfigurations.cs`:

```csharp
document.Components.SecuritySchemes["OAuth2-Google"] = new OpenApiSecurityScheme
{
    Type = SecuritySchemeType.OAuth2,
    Description = "🔐 Autenticação via Google",
    Flows = new OpenApiOAuthFlows
    {
        AuthorizationCode = new OpenApiOAuthFlow
        {
            AuthorizationUrl = new Uri("https://accounts.google.com/o/oauth2/v2/auth"),
            TokenUrl = new Uri("https://oauth2.googleapis.com/token"),
            Scopes = new Dictionary<string, string>
            {
                ["openid"] = "OpenID Connect",
                ["profile"] = "Informações do perfil",
                ["email"] = "Endereço de email"
            }
        }
    }
};
```

---

## 🔧 Configuração Microsoft

### 1. Registrar aplicação no Azure Portal

1. Acesse [Azure Portal](https://portal.azure.com/)
2. Vá em **Microsoft Entra ID** (antigo Azure AD)
3. Clique em **App registrations** → **New registration**
4. Preencha:
   - **Name**: `Product.Template.Api`
   - **Supported account types**: `Accounts in any organizational directory and personal Microsoft accounts`
   - **Redirect URI**: `Web` → `https://localhost:7254/api/v1/identity/external-callback`
5. Clique em **Register**

### 2. Obter credenciais

1. Copie o **Application (client) ID**
2. Vá em **Certificates & secrets** → **New client secret**
3. Crie um secret e **copie o valor imediatamente** (não será mostrado novamente)

### 3. Configurar User Secrets (Desenvolvimento)

```bash
cd src/Api
dotnet user-secrets init
dotnet user-secrets set "MicrosoftAuth:ClientId" "your-client-id"
dotnet user-secrets set "MicrosoftAuth:ClientSecret" "your-client-secret"
```

### 4. Habilitar no appsettings.json

```json
{
  "MicrosoftAuth": {
    "Enabled": true,
    "ClientId": "override-via-user-secrets",
    "ClientSecret": "override-via-user-secrets",
    "TenantId": "common",
    "RedirectUri": "https://localhost:7254/api/v1/identity/external-callback",
    "Scopes": "openid profile email"
  }
}
```

---

## 📝 Exemplos de Uso

### Listar Provedores Disponíveis

```http
GET /api/v1/identity/providers
```

**Resposta:**
```json
{
  "providers": ["jwt", "microsoft"],
  "count": 2
}
```

### Login Tradicional (JWT)

```http
POST /api/v1/identity/login
Content-Type: application/json

{
  "email": "usuario@exemplo.com",
  "password": "SenhaSegura123!"
}
```

### Login via Microsoft

**1. Redirecionar usuário para Microsoft:**

```
https://login.microsoftonline.com/common/oauth2/v2.0/authorize
  ?client_id={clientId}
  &response_type=code
  &redirect_uri=https://localhost:7254/api/v1/identity/external-callback
  &scope=openid%20profile%20email
```

**2. Enviar código recebido:**

```http
POST /api/v1/identity/external-login
Content-Type: application/json

{
  "provider": "microsoft",
  "code": "0.AX0A8q3...",
  "redirectUri": "https://localhost:7254/api/v1/identity/external-callback"
}
```

**3. Receber token JWT:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "usuario@outlook.com",
    "firstName": "João"
  }
}
```

---

## 🎯 Boas Práticas

### ✅ Faça

- Use **User Secrets** em desenvolvimento para armazenar Client Secrets
- Use **Azure Key Vault** em produção
- Implemente validação de token no método `ValidateTokenAsync`
- Adicione logs detalhados para debugging
- Teste com múltiplos providers habilitados simultaneamente

### ❌ Não Faça

- **Nunca** commite Client Secrets no código
- **Nunca** armazene tokens em plain text
- **Não** confie cegamente em tokens externos sem validação
- **Não** implemente autenticação sem HTTPS em produção

---

## 🔒 Segurança

### Validação de Tokens Externos

```csharp
public async Task<bool> ValidateTokenAsync(
    string token,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Validar com API do provedor externo
        var response = await _httpClient.GetAsync(
            $"https://provider.com/validate?token={token}",
            cancellationToken);

        return response.IsSuccessStatusCode;
    }
    catch
    {
        return false;
    }
}
```

### Proteção de Endpoints

```csharp
[HttpPost("sensitive-operation")]
[Authorize] // ✅ Requer autenticação
public async Task<IActionResult> SensitiveOperation()
{
    // Código protegido
}
```

---

## 📚 Referências

- [Microsoft Identity Platform](https://learn.microsoft.com/en-us/entra/identity-platform/)
- [Google OAuth2](https://developers.google.com/identity/protocols/oauth2)
- [OAuth 2.0 RFC 6749](https://datatracker.ietf.org/doc/html/rfc6749)
- [JWT RFC 7519](https://datatracker.ietf.org/doc/html/rfc7519)

---

## ❓ FAQ

### Como desabilitar um provider?

Defina `Enabled: false` no appsettings.json:

```json
{
  "MicrosoftAuth": {
    "Enabled": false
  }
}
```

### Posso ter múltiplos providers ativos?

✅ Sim! O sistema foi projetado para suportar múltiplos providers simultaneamente.

### Como adicionar claims customizados?

No `AuthenticateAsync`, retorne claims extras em `UserInfo`:

```csharp
UserInfo: new Dictionary<string, string>
{
    ["email"] = userInfo.Email,
    ["customClaim"] = "customValue"
}
```

---

**Última atualização:** 2026-01-14  
**Versão:** 1.0.0
