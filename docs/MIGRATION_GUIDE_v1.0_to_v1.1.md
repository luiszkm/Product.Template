# 🔄 Guia de Migração - v1.0.0 para v1.1.0

Este guia ajuda você a migrar um projeto existente (baseado na v1.0.0) para aproveitar os novos recursos da v1.1.0.

## 📋 Visão Geral

A versão 1.1.0 adiciona 5 recursos avançados sem quebrar compatibilidade com a v1.0.0:

- ✅ Response Compression
- ✅ Output Caching
- ✅ Request Deduplication
- ✅ Feature Flags
- ✅ Audit Trail

**Tempo estimado de migração:** 30-45 minutos

---

## 🚀 Migração Passo a Passo

### Passo 1: Atualizar Pacotes NuGet

#### 1.1. Api.csproj

Adicionar no arquivo `src/Api/Api.csproj`:

```xml
<PackageReference Include="Microsoft.FeatureManagement.AspNetCore" Version="4.4.0" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.*" />
```

#### 1.2. Kernel.Infrastructure.csproj

Adicionar no arquivo `src/Shared/Kernel.Infrastructure/Kernel.Infrastructure.csproj`:

```xml
<PackageReference Include="Microsoft.AspNetCore.Http.Abstractions" Version="2.2.0" />
```

#### 1.3. Restaurar pacotes

```bash
dotnet restore
```

---

### Passo 2: Copiar Arquivos de Configuração

#### 2.1. Response Compression

Criar arquivo `src/Api/Configurations/CompressionConfiguration.cs`:

[Copiar conteúdo do template ou usar o arquivo fornecido na documentação]

#### 2.2. Output Caching

Criar arquivo `src/Api/Configurations/CachingConfiguration.cs`:

[Copiar conteúdo do template]

#### 2.3. Feature Flags

Criar arquivo `src/Api/Configurations/FeatureFlagsConfiguration.cs`:

[Copiar conteúdo do template]

#### 2.4. Request Deduplication

Criar arquivo `src/Api/Middleware/RequestDeduplicationMiddleware.cs`:

[Copiar conteúdo do template]

---

### Passo 3: Implementar Audit Trail

#### 3.1. Criar interfaces e classes base

**Criar arquivo:** `src/Shared/Kernel.Domain/SeedWorks/IAuditableEntity.cs`

```csharp
namespace Product.Template.Kernel.Domain.SeedWorks;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
```

**Criar arquivo:** `src/Shared/Kernel.Domain/SeedWorks/AuditableEntity.cs`

```csharp
namespace Product.Template.Kernel.Domain.SeedWorks;

public abstract class AuditableEntity : IAuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    protected AuditableEntity()
    {
        CreatedAt = DateTime.UtcNow;
    }
}

public abstract class AuditableAggregateRoot<TId> : AggregateRoot<TId>, IAuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    protected AuditableAggregateRoot(TId id) : base(id)
    {
        CreatedAt = DateTime.UtcNow;
    }
}
```

#### 3.2. Criar CurrentUserService

**Criar arquivo:** `src/Shared/Kernel.Application/Security/ICurrentUserService.cs`

```csharp
using System.Security.Claims;

namespace Product.Template.Kernel.Application.Security;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    IEnumerable<Claim> Claims { get; }
}
```

**Criar arquivo:** `src/Shared/Kernel.Infrastructure/Security/CurrentUserService.cs`

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Product.Template.Kernel.Application.Security;

namespace Kernel.Infrastructure.Security;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return null;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User
        .FindFirst(ClaimTypes.Email)?.Value;

    public string? UserName => _httpContextAccessor.HttpContext?.User
        .FindFirst(ClaimTypes.Name)?.Value ?? Email;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<Claim> Claims => _httpContextAccessor.HttpContext?.User?.Claims ?? Enumerable.Empty<Claim>();
}
```

#### 3.3. Criar Interceptor

**Criar arquivo:** `src/Shared/Kernel.Infrastructure/Persistence/Interceptors/AuditableEntityInterceptor.cs`

[Copiar conteúdo completo do template]

---

### Passo 4: Atualizar Dependency Injection

#### 4.1. Kernel.Infrastructure/DependencyInjection.cs

Adicionar registros:

```csharp
// Serviços de segurança
services.AddScoped<ICurrentUserService, CurrentUserService>();

// Interceptors para auditoria
services.AddScoped<AuditableEntityInterceptor>();

// HttpContextAccessor para CurrentUserService
services.AddHttpContextAccessor();
```

#### 4.2. Identity.Infrastructure/Data/DatabaseConfiguration.cs

Modificar configuração do DbContext:

```csharp
services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseInMemoryDatabase(databaseName);
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
    
    // Adicionar interceptor de auditoria
    var auditInterceptor = sp.GetService<AuditableEntityInterceptor>();
    if (auditInterceptor != null)
    {
        options.AddInterceptors(auditInterceptor);
    }
});
```

---

### Passo 5: Atualizar appsettings.json

Adicionar novas seções:

```json
{
  "Caching": {
    "Enabled": true,
    "DefaultExpirationMinutes": 10
  },
  "FeatureFlags": {
    "EnableCaching": true,
    "EnableAuditTrail": true,
    "EnableRequestDeduplication": true,
    "EnableAdvancedLogging": true,
    "EnableExperimentalFeatures": false
  }
}
```

---

### Passo 6: Atualizar Program.cs

#### 6.1. Adicionar registros de serviços

Após os registros existentes, adicionar:

```csharp
// Response Compression (Brotli + Gzip)
builder.Services.AddCompressionConfiguration();

// Output Caching
builder.Services.AddCachingConfiguration(builder.Configuration);

// Feature Flags
builder.Services.AddFeatureFlagsConfiguration(builder.Configuration);
```

#### 6.2. Adicionar middleware no pipeline

Na ordem correta:

```csharp
// Response Compression (ANTES de UseRouting)
app.UseResponseCompression();

// Output Caching (DEPOIS de UseRouting)
app.UseCachingConfiguration();

// Request Deduplication (DEPOIS de RequestLogging)
app.UseMiddleware<RequestDeduplicationMiddleware>();
```

**Ordem completa recomendada:**

```csharp
app.UseResponseCompression();           // 1. Compressão
app.UseCachingConfiguration();          // 2. Cache
app.UseSerilogConfiguration();          // 3. Logging
app.UseMiddleware<RequestLoggingMiddleware>();    // 4. Request logging
app.UseMiddleware<RequestDeduplicationMiddleware>(); // 5. Deduplicação
app.UseMiddleware<IpWhitelistMiddleware>();       // 6. IP filtering
app.UseHttpsRedirection();              // 7. HTTPS
app.UseRouting();                       // 8. Routing
app.UseSecurityConfiguration();         // 9. CORS
app.UseAuthentication();                // 10. Auth
app.UseAuthorization();                 // 11. Authz
app.UseRateLimiting();                  // 12. Rate limit
```

---

### Passo 7: Corrigir Issues Conhecidos (se aplicável)

#### 7.1. IUnitOfWork duplicado

Se você tem erro de `IUnitOfWork`, verificar em `Identity.Infrastructure/DependencyInjection.cs`:

```csharp
// CORRETO:
services.AddScoped<IUnitOfWork, UnitOfWork>();

// ERRADO (remover se existir):
// services.AddScoped<IUnitOfWork, IUnitOfWork>();
```

#### 7.2. Query do UserRepository

Se estiver usando Value Object `Email`, atualizar `UserRepository.cs`:

```csharp
public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
{
    return await _context.Users
        .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => EF.Property<string>(u.Email, "Value") == email, cancellationToken);
}
```

---

### Passo 8: Compilar e Testar

```bash
# Limpar build anterior
dotnet clean

# Compilar
dotnet build

# Executar
cd src/Api
dotnet run
```

**Verificações:**
- [ ] Build sem erros
- [ ] Aplicação inicia
- [ ] Swagger acessível
- [ ] Endpoints funcionando
- [ ] Logs aparecem no console

---

## 🧪 Testes de Validação

### Teste 1: Response Compression

```bash
curl -H "Accept-Encoding: gzip, br" https://localhost:5001/api/v1/identity -v | grep "Content-Encoding"
```

Deve retornar: `Content-Encoding: br` ou `gzip`

### Teste 2: Output Caching

Adicionar em um controller:

```csharp
[HttpGet("test-cache")]
[OutputCache(PolicyName = "UserCache")]
public IActionResult TestCache()
{
    return Ok(new { timestamp = DateTime.UtcNow });
}
```

Fazer 2 requisições - timestamp deve ser igual (cache hit).

### Teste 3: Request Deduplication

```bash
# Primeira requisição
curl -X POST https://localhost:5001/api/v1/identity/register \
  -H "X-Idempotency-Key: test-123" \
  -d '{"email":"test@test.com","password":"Pass123!"}'

# Segunda (duplicada) - deve retornar 409
curl -X POST https://localhost:5001/api/v1/identity/register \
  -H "X-Idempotency-Key: test-123" \
  -d '{"email":"test@test.com","password":"Pass123!"}'
```

### Teste 4: Feature Flags

Alterar no appsettings.json:

```json
"EnableExperimentalFeatures": true
```

Criar endpoint de teste e verificar mudança após reiniciar.

### Teste 5: Audit Trail

Criar entidade auditável e salvar no banco. Verificar se campos `CreatedAt`, `CreatedBy` foram preenchidos.

---

## 📚 Próximos Passos

Após migração concluída:

1. [ ] Ler documentação completa: `docs/ADVANCED_FEATURES.md`
2. [ ] Revisar checklist: `docs/VALIDATION_CHECKLIST.md`
3. [ ] Atualizar suas entidades para usar `AuditableAggregateRoot<T>` (opcional)
4. [ ] Adicionar `[OutputCache]` em endpoints públicos
5. [ ] Configurar Feature Flags por ambiente

---

## ❓ Precisa de Ajuda?

Consulte:
- `docs/ADVANCED_FEATURES.md` - Documentação completa
- `docs/IMPLEMENTATION_SUMMARY.md` - Resumo técnico
- `docs/VALIDATION_CHECKLIST.md` - Checklist de validação
- `docs/CHANGELOG.md` - Lista de mudanças

---

**Versão do Guia:** 1.0  
**Última atualização:** 2026-01-17

