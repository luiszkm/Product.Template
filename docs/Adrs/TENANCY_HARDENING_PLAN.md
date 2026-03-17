Tenancy Hardening — Plano de Execução
Status: Implementado
Data: 2026-03-16
Atualizado: 2026-03-17
Base: ADR-002, ADR-003

## Objetivo
Preparar a fundação para tenancy forte sem alterar a estrutura atual, listando ações por camada.

## Inventário de entidades multi-tenant

| Módulo | Entidade | TenantId privado | Factory recebe tenantId |
|--------|----------|:----------------:|:----------------------:|
| Identity | `User` | ✅ | ✅ |
| Identity | `RefreshToken` | ✅ | ✅ |
| Authorization | `Role` | ✅ | ✅ |
| Authorization | `Permission` | ✅ | ✅ |
| Authorization | `RolePermission` | ✅ | ✅ |
| Authorization | `UserAssignment` | ✅ | ✅ |

> Nota: `Role`, `Permission`, `RolePermission` e `UserAssignment` foram movidos para o módulo `Authorization`. O módulo `Identity` mantém apenas `User` e `RefreshToken`.

## Status por camada

### Domínio ✅
- `TenantId` com setter privado em todas as entidades multi-tenant.
- Factories (`Create(...)`) exigem `tenantId` como parâmetro obrigatório.
- `AssignTenant(tenantId)` adicionado a `Entity` base para atribuição controlada.

### Infra/EF ✅
- Filtro global `HasQueryFilter(e => e.TenantId == TenantIdForQueryFilter)` em `ModelBuilderTenantExtensions`.
- `MultiTenantSaveChangesInterceptor` usa `AssignTenant()` para stampar TenantId em inserts.
- Seeds: tenants configuráveis em `appsettings` via seção `Tenants`; seed de permissões/roles/usuários propaga `TenantId`.
- `EfModelAssemblyRegistry` compõe configurações EF por módulo no `AppDbContext`.

### Aplicação ✅
- `TenantContextBehavior` (MediatR pipeline) valida que `ITenantContext` está resolvido antes dos handlers.
- Commands/queries recebem `TenantId` implicitamente via `ITenantContext`; handlers leem via injeção.

### API/Middleware ✅
- `TenantResolverMiddleware`: resolve tenant via header `X-Tenant` ou subdomínio → `ITenantContext`.
- `TenantGuardMiddleware`: bloqueia requests sem tenant resolvido (exceto endpoints públicos).

## Observabilidade e Operação
- ✅ Logs enriquecidos com `CorrelationId` via `RequestLoggingMiddleware`.
- 🔜 Enriquecer todos os traces/logs com `TenantId` obrigatório (ADR-007).
- 🔜 Métricas por tenant: throughput, erros, latência p95/p99, falhas de login/authz.
- 🔜 Health check com validação de `TenantResolver` + conectividade ao catálogo de tenant.

## Testes de arquitetura implementados
- ✅ Validação de dependências por camada (Domain ← Application ← Infrastructure ← Api).
- ✅ Invariantes de tenancy: `TenantId` setter privado, toda entidade multi-tenant implementa `IMultiTenantEntity`.
- ✅ Commands têm validators correspondentes.
- 🔜 Architecture test para garantir que `[Authorize(Policy=...)]` só referencia políticas registradas no catálogo.
