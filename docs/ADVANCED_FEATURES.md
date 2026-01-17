# 🚀 Recursos Avançados Implementados

> **Versão:** 1.1.0  
> **Data:** 2026-01-17  
> **Compatibilidade:** .NET 10.0+

Este documento descreve os recursos avançados implementados no template para melhorar performance, segurança e auditoria.

## 📦 Recursos Implementados

### 1. **Response Compression** ✅

Comprime automaticamente respostas HTTP usando Brotli e Gzip para reduzir o tráfego de rede.

**Arquivo:** `src/Api/Configurations/CompressionConfiguration.cs`

**Benefícios:**
- Reduz tamanho das respostas em até 70-80%
- Melhora performance em redes lentas
- Suporta Brotli (melhor compressão) e Gzip (mais compatível)

**Uso:**
```csharp
// Já configurado automaticamente no Program.cs
builder.Services.AddCompressionConfiguration();
app.UseResponseCompression();
```

**Tipos MIME comprimidos:**
- `application/json`
- `application/xml`
- `text/plain`, `text/css`, `text/html`
- `application/javascript`, `text/javascript`

---

### 2. **Output Caching** ✅

Sistema de cache de respostas HTTP (substitui Response Caching no .NET 8+).

**Arquivo:** `src/Api/Configurations/CachingConfiguration.cs`

**Políticas Disponíveis:**
- **UserCache**: Cache de 5 minutos para endpoints de usuários
- **PublicCache**: Cache de 15 minutos para dados públicos
- **ReferenceDataCache**: Cache de 30 minutos para dados de referência
- **NoCache**: Desabilita cache

**Uso no Controller:**
```csharp
[HttpGet]
[OutputCache(PolicyName = "UserCache")]
public async Task<ActionResult<PaginatedListOutput<UserOutput>>> ListUsers()
{
    // ...
}
```

**Configuração (appsettings.json):**
```json
"Caching": {
  "Enabled": true,
  "DefaultExpirationMinutes": 10
}
```

**Suporte a Redis:**
```json
"ConnectionStrings": {
  "Redis": "localhost:6379"
}
```

---

### 3. **Request Deduplication** ✅

Previne requisições duplicadas (idempotência) usando chaves de deduplicação.

**Arquivo:** `src/Api/Middleware/RequestDeduplicationMiddleware.cs`

**Como funciona:**
1. Verifica requisições POST/PUT/PATCH
2. Usa header `X-Idempotency-Key` (ou gera hash do corpo)
3. Bloqueia requisições duplicadas por 5 minutos

**Uso:**
```bash
# Cliente envia chave de idempotência
curl -X POST /api/v1/identity/register \
  -H "X-Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000" \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Pass123!"}'
```

**Resposta em caso de duplicação:**
```json
{
  "error": "Duplicate request detected",
  "message": "Esta requisição já foi processada recentemente...",
  "idempotencyKey": "550e8400-e29b-41d4-a716-446655440000",
  "originalRequestTime": "2026-01-17T10:30:00Z"
}
```

---

### 4. **Feature Flags** ✅

Sistema de controle de features em produção sem necessidade de deploy.

**Arquivo:** `src/Api/Configurations/FeatureFlagsConfiguration.cs`

**Configuração (appsettings.json):**
```json
"FeatureFlags": {
  "EnableCaching": true,
  "EnableAuditTrail": true,
  "EnableRequestDeduplication": true,
  "EnableAdvancedLogging": true,
  "EnableExperimentalFeatures": false
}
```

**Uso no Controller:**
```csharp
using Microsoft.FeatureManagement;

public class MyController : ControllerBase
{
    private readonly IFeatureManager _featureManager;

    [HttpGet]
    public async Task<IActionResult> GetData()
    {
        if (await _featureManager.IsEnabledAsync("EnableExperimentalFeatures"))
        {
            // Nova funcionalidade
            return Ok(await GetDataV2());
        }
        
        // Funcionalidade antiga
        return Ok(await GetDataV1());
    }
}
```

**Uso com Atributos:**
```csharp
using Microsoft.FeatureManagement.Mvc;

[FeatureGate("EnableExperimentalFeatures")]
[HttpGet("experimental")]
public IActionResult ExperimentalEndpoint()
{
    return Ok("This is experimental!");
}
```

---

### 5. **Audit Trail** ✅

Sistema automático de auditoria para rastrear criação e modificação de entidades.

**Arquivos:**
- `src/Shared/Kernel.Domain/SeedWorks/IAuditableEntity.cs`
- `src/Shared/Kernel.Domain/SeedWorks/AuditableEntity.cs`
- `src/Shared/Kernel.Infrastructure/Persistence/Interceptors/AuditableEntityInterceptor.cs`
- `src/Shared/Kernel.Infrastructure/Security/CurrentUserService.cs`

**Como usar:**

1. **Herdar de AuditableAggregateRoot:**
```csharp
public class Product : AuditableAggregateRoot<Guid>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    private Product(Guid id) : base(id) { }

    public static Product Create(string name, decimal price)
    {
        return new Product(Guid.NewGuid())
        {
            Name = name,
            Price = price
            // CreatedAt e CreatedBy preenchidos automaticamente
        };
    }

    public void UpdatePrice(decimal newPrice)
    {
        Price = newPrice;
        // UpdatedAt e UpdatedBy preenchidos automaticamente ao salvar
    }
}
```

2. **Campos adicionados automaticamente:**
- `CreatedAt`: Data/hora de criação (UTC)
- `CreatedBy`: Usuário que criou (email ou "System")
- `UpdatedAt`: Data/hora da última atualização (UTC)
- `UpdatedBy`: Usuário que atualizou (email ou "System")

3. **Interceptor preenche automaticamente:**
```csharp
// Ao adicionar nova entidade
var product = Product.Create("iPhone", 999.99m);
await _repository.AddAsync(product);
await _unitOfWork.Commit(); // CreatedBy = "user@example.com", CreatedAt = agora

// Ao atualizar entidade
product.UpdatePrice(899.99m);
await _repository.UpdateAsync(product);
await _unitOfWork.Commit(); // UpdatedBy = "user@example.com", UpdatedAt = agora
```

**Obter usuário atual:**
```csharp
public class MyService
{
    private readonly ICurrentUserService _currentUser;

    public MyService(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task DoSomething()
    {
        var userId = _currentUser.UserId;
        var email = _currentUser.Email;
        var isAuthenticated = _currentUser.IsAuthenticated;
    }
}
```

---

## 📊 Monitoramento e Logs

Todos os recursos geram logs detalhados via Serilog:

```json
[16:20:11 INF] Requisição registrada para deduplicação. Key: abc123
[16:20:15 WRN] Requisição duplicada detectada. Idempotency-Key: abc123
```

---

## 🔧 Configuração por Ambiente

Use Feature Flags para controlar recursos por ambiente:

**appsettings.Development.json:**
```json
"FeatureFlags": {
  "EnableCaching": false,  // Desabilitar cache em dev
  "EnableAuditTrail": true
}
```

**appsettings.Production.json:**
```json
"FeatureFlags": {
  "EnableCaching": true,   // Habilitar cache em prod
  "EnableAuditTrail": true
}
```

---

## 🚀 Performance

### Antes vs Depois

| Recurso | Benefício |
|---------|-----------|
| **Response Compression** | -70% tamanho da resposta |
| **Output Caching** | -90% tempo de resposta (cache hit) |
| **Request Deduplication** | Evita processamento duplicado |
| **Audit Trail** | Rastreabilidade completa |
| **Feature Flags** | Deploy sem downtime |

---

## 📝 Próximos Passos

Recursos opcionais que podem ser adicionados:

1. **Background Jobs** (Hangfire/Quartz)
2. **SignalR** (Real-time)
3. **File Upload** (MinIO/S3)
4. **Multi-Tenancy**
5. **Localization/i18n**
6. **API Gateway** (YARP)
7. **GraphQL** (HotChocolate)

---

## 🐛 Troubleshooting

### Response Compression não está funcionando

**Problema:** Headers `Content-Encoding` não aparecem na resposta.

**Soluções:**
1. Verificar se o cliente envia header `Accept-Encoding: gzip, br`
2. Verificar se middleware está registrado: `app.UseResponseCompression()`
3. Verificar se está antes de `app.UseRouting()`
4. Testar com curl: `curl -H "Accept-Encoding: gzip" https://localhost:5001/api/v1/identity -v`

### Output Caching não funciona

**Problema:** Respostas não estão sendo cacheadas.

**Soluções:**
1. Verificar `"Caching:Enabled": true` no appsettings.json
2. Verificar se atributo `[OutputCache]` está no controller
3. Verificar se middleware está após `UseRouting()`: `app.UseOutputCache()`
4. Em desenvolvimento, testar sem autenticação primeiro
5. Verificar logs para cache hits/misses

### Request Deduplication retorna erro inesperado

**Problema:** Requisições válidas sendo bloqueadas como duplicadas.

**Soluções:**
1. Usar chaves únicas por requisição
2. Aumentar janela de deduplicação se necessário
3. Verificar se não há conflito de hash (improvável)
4. Limpar cache em memória reiniciando aplicação

### Feature Flags não respondem a mudanças

**Problema:** Alterar flag no appsettings.json não reflete na aplicação.

**Soluções:**
1. Reiniciar a aplicação após alterar appsettings.json
2. Em produção, usar Azure App Configuration para hot reload
3. Verificar se `IFeatureManager` está sendo injetado corretamente
4. Usar Feature Filters para condições mais complexas

### Audit Trail não preenche campos

**Problema:** `CreatedBy` e `UpdatedBy` ficam vazios ou "System".

**Soluções:**
1. Verificar se usuário está autenticado (JWT token válido)
2. Verificar se `ICurrentUserService` está registrado no DI
3. Verificar se `HttpContextAccessor` está registrado
4. Verificar se interceptor está sendo executado
5. Debugar `CurrentUserService.Email` para ver o valor retornado

### Erro: IHttpContextAccessor não encontrado

**Problema:** Erro de compilação no `CurrentUserService`.

**Solução:**
```bash
# Verificar se pacote está instalado
dotnet list package | findstr Http.Abstractions

# Se não estiver, adicionar
dotnet add src/Shared/Kernel.Infrastructure package Microsoft.AspNetCore.Http.Abstractions
```

### Erro: Namespace não corresponde ao arquivo

**Problema:** Warnings sobre namespaces.

**Solução:** Corrigir namespace para seguir convenção:
- Arquivos em `Kernel.Infrastructure` devem usar namespace `Kernel.Infrastructure.*`
- Não usar `Product.Template.Kernel.Infrastructure.*`

---

## 📚 Referências

- [Output Caching - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output)
- [Feature Management - Microsoft Docs](https://learn.microsoft.com/en-us/azure/azure-app-configuration/use-feature-flags-dotnet-core)
- [Response Compression - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/performance/response-compression)

