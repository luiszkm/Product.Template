# Arquitetura de Multi-Tenancy

> Cobre resolução de tenant por request, cache do tenant store, provisionamento de novos tenants e isolamento por modelo EF. Para o formato do identificador (`Guid` vs `long`), ver [Tenant identifiers](./tenant-identifiers.md).

---

## Visão geral do pipeline

```
Request (header X-Tenant: <tenantKey>)
        │
        ▼
TenantResolutionMiddleware
        │  resolve tenantKey (ITenantResolver) → fallback público (opcional)
        ▼
ITenantStore.GetByKeyAsync(tenantKey)   ──▶  CachedTenantStore (IMemoryCache, TTL 5 min)
        │                                        │ miss → HostDbContext.Tenants (AsNoTracking)
        ▼
ITenantContext.SetTenant(tenant)  → disponível via DI no resto do request
        │
        ▼
AppDbContext (filtros EF por TenantId) + TenantModelCacheKeyFactory (cache do modelo por tenant)
```

Componentes vivem em `src/Shared/Kernel.Infrastructure/MultiTenancy` (implementação) e
`src/Shared/Kernel.Domain/MultiTenancy` (contratos: `ITenantStore`, `ITenantResolver`, `ITenantProvisioningService`).

## TenantResolutionMiddleware

Arquivo: [`TenantResolutionMiddleware.cs`](../../src/Shared/Kernel.Infrastructure/MultiTenancy/TenantResolutionMiddleware.cs)

1. `ITenantResolver.ResolveTenantKey(context)` extrai a `tenantKey` do request (por padrão, header configurado em `TenantResolutionOptions.HeaderName`, default `X-Tenant`).
2. Se vazio e `AllowPublicFallback = true`, usa `PublicTenantKey` (default `"public"`).
3. Se ainda vazio, segue o pipeline sem tenant resolvido (rotas que não exigem tenant, ex.: health checks).
4. Busca o tenant via `ITenantStore.GetByKeyAsync`. Se `null` ou `IsActive == false`, responde `400 Bad Request` imediatamente — **não** deixa o request prosseguir.
5. Em caso de sucesso, popula `ITenantContext` (via DI, resolvido no request atual) e abre um `logger.BeginScope` com `TenantId`/`TenantKey`, garantindo que logs subsequentes carreguem o contexto de tenant.

**Configuração** (`appsettings`, seção `MultiTenancy`):

```json
{
  "MultiTenancy": {
    "HeaderName": "X-Tenant",
    "AllowPublicFallback": false,
    "PublicTenantKey": "public"
  }
}
```

> Cuidado: ativar `AllowPublicFallback` em produção mistura requests sem header no tenant público — usar só em dev/demo.

## CachedTenantStore

Arquivo: [`CachedTenantStore.cs`](../../src/Shared/Kernel.Infrastructure/MultiTenancy/CachedTenantStore.cs)

Implementação de `ITenantStore` sobre `HostDbContext` (banco host, fora do `AppDbContext` multi-tenant) com cache em `IMemoryCache`:

- `GetByKeyAsync`: lê do cache (`tenant:{tenantKey}`); em miss, consulta `Tenants` com `AsNoTracking` e grava no cache com TTL fixo de **5 minutos**.
- `ListActiveAsync`: sempre bate no banco (sem cache) — usado por telas administrativas onde consistência importa mais que latência.
- `UpsertAsync`: grava/atualiza no `HostDbContext`, faz `SaveChangesAsync` e **invalida** a entrada do cache (`memoryCache.Remove`) para o `tenantKey` afetado.

Efeito colateral a saber: como o TTL é de 5 minutos, uma alteração feita diretamente no banco (fora de `UpsertAsync`, ex.: script SQL manual) só reflete em `GetByKeyAsync` após expirar o cache ou reiniciar o processo. Alterações via `TenantProvisioningService`/API de gestão passam por `UpsertAsync` e invalidam corretamente.

## TenantProvisioningService

Arquivo: [`TenantProvisioningService.cs`](../../src/Shared/Kernel.Infrastructure/MultiTenancy/TenantProvisioningService.cs)

Cria um novo `TenantConfig` (`Guid` gerado no servidor — ver [Tenant identifiers](./tenant-identifiers.md)) e aplica o `TenantIsolationMode` escolhido:

| `TenantIsolationMode` | Efeito |
|---|---|
| `SharedDb` | Todos os tenants na mesma base/schema; isolamento via filtro EF por `TenantId`. `SchemaName` e `ConnectionString` ficam `null`. |
| `SchemaPerTenant` | Cria schema dedicado `tenant_{tenantKey}` via `CREATE SCHEMA IF NOT EXISTS` (conexão resolvida como `SharedDb` para acessar o servidor). |
| `DedicatedDb` | Gera uma connection string apontando para `{tenantKey}_db` (banco Postgres dedicado). A criação do banco em si **não** é feita por este serviço — precisa existir/ser provisionado separadamente. |

`tenantKey` é normalizado (`Trim().ToLowerInvariant()`) antes de qualquer persistência ou geração de nome de schema/banco.

> Nota: a connection string de exemplo para `DedicatedDb` no código usa credenciais de dev (`postgres`/`YourStrong!Pass123`) — em produção isso deve vir de um provider de segredos, não hardcoded.

## TenantModelCacheKeyFactory

Arquivo: [`TenantModelCacheKeyFactory.cs`](../../src/Shared/Kernel.Infrastructure/Persistence/TenantModelCacheKeyFactory.cs)

Implementa `IModelCacheKeyFactory` do EF Core. Por padrão, o EF Core cacheia o modelo compilado por tipo de `DbContext` — o que quebra em isolamento `SchemaPerTenant`, onde o mesmo `AppDbContext` precisa mapear para schemas diferentes por tenant.

A factory inclui `appDbContext.TenantIdForQueryFilter` na chave de cache, então cada tenant obtém seu próprio modelo compilado em vez de reaproveitar (incorretamente) o modelo de outro tenant. Sem isso, o primeiro tenant a "aquecer" o cache do modelo ditaria o schema para todos os demais.

## UnitOfWork e despacho de eventos de domínio

Arquivo: [`UnitOfWork.cs`](../../src/Shared/Kernel.Infrastructure/Persistence/UnitOfWork.cs.cs)

`Commit`:
1. `AppDbContext.SaveChangesAsync` — persiste as alterações primeiro.
2. Varre o `ChangeTracker` por `AggregateRoot` com `DomainEvents` pendentes.
3. Limpa (`ClearDomainEvents`) e então publica cada evento via MediatR `IPublisher`.

Ou seja, eventos de domínio são despachados **depois** do commit no banco (not transacional com o `SaveChanges` em si) — um handler de evento que falhe não desfaz a escrita já persistida. `Rollback` é um no-op (`Task.CompletedTask`): não há transação explícita aberta por fora do `SaveChangesAsync`.

## EmailConfirmationTokenService

Arquivo: [`EmailConfirmationTokenService.cs`](../../src/Core/Identity/Identity.Infrastructure/Security/EmailConfirmationTokenService.cs)

Token de confirmação de e-mail é um HMAC-SHA256 sobre `{userId:N}:{securityStamp}`, assinado com `Jwt:Secret` (mesmo segredo usado para JWT — reaproveitado, não é uma chave dedicada). Não é um token opaco/aleatório armazenado em banco: é **recomputável** — `ValidateToken` gera o hash esperado e compara em tempo constante (`CryptographicOperations.FixedTimeEquals`).

Implicação de design: como o token depende do `securityStamp` do usuário, invalidar todos os tokens de confirmação pendentes de um usuário é tão simples quanto rotacionar o `securityStamp` (mesmo mecanismo já usado para revogar sessões/JWTs — ver `290c57c` no histórico do repo). Não existe expiração própria do token além da mudança de `securityStamp`; se for necessário TTL explícito, precisa ser adicionado (ex.: embutir timestamp no payload).

## Testes de referência

- `tests/UnitTests/MultiTenancy/CachedTenantStoreTests.cs`
- `tests/UnitTests/MultiTenancy/TenantProvisioningServiceTests.cs`
- `tests/UnitTests/MultiTenancy/TenantResolutionMiddlewareTests.cs`
- `tests/UnitTests/Persistence/TenantModelCacheKeyFactoryTests.cs`
- `tests/UnitTests/Persistence/UnitOfWorkTests.cs`
- `tests/UnitTests/Security/EmailConfirmationTokenServiceTests.cs`
