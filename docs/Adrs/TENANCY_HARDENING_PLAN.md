Tenancy Hardening — Plano de Execução
Status: Draft
Data: 2026-03-16
Base: ADR-002, ADR-003

## Objetivo
Preparar a fundação para tenancy forte sem alterar a estrutura atual, listando ações por camada.

## Inventário de entidades multi-tenant (Identity)
- `User`, `Role`, `Permission`, `RolePermission`, `UserRole`, `RefreshToken`.
- Ação: `TenantId` com setter privado; atribuição em factories; validar ausência de TenantId.

## Plano por camada
- Dominio: reforçar invariantes (factory exige TenantId, setters privados, validação de tenant nulo).
- Infra/EF:
  - Filtro global para `IMultiTenantEntity` no `AppDbContext`.
  - Auditoria de TenantId em inserts/updates; impedir salvar sem TenantId resolvido.
  - Seeds: tenants + vinculação inicial a módulos/features/permissões.
- Aplicação:
  - Pipeline (behavior) pode validar TenantId presente no `ITenantContext` antes de handlers.
  - Commands/queries recebem TenantId implicitamente via contexto; evitar parâmetros diretos salvo quando necessário.
- API/Middleware:
  - `ITenantResolver` (header `X-Tenant` + subdomínio) → `ITenantContext` injetado.
  - Bloquear requests sem tenant resolvido (exceto endpoints públicos permitidos por regra explícita).

## Observabilidade e Operação
- Métricas por tenant: throughput, erros, latência p95/p99, falhas de login/authz.
- Logs/trace: enriquecer com TenantId obrigatório.
- Health check: validar resolução de tenant + acesso ao catálogo de tenant.

## Próximos passos
- Listar por entidade os pontos de factory a ajustar (documentar antes de alterar código).
- Definir a interface do filtro global e pontos de teste (unit/integration) para bloqueio sem TenantId.
- Criar teste de arquitetura para garantir setter privado em `TenantId` e implementação de `IMultiTenantEntity` com filtro ativo.

## Pontos de ajuste por entidade (Identity)
- `User.Create(...)`: receber `tenantId` e atribuir no factory; `TenantId` setter privado.
- `Role.Create(...)`: idem.
- `Permission.Create(...)`: idem.
- `RolePermission.Create(...)`: idem.
- `UserRole.Create(...)`: idem.
- `RefreshToken.Create(...)`: idem.
- Checklist adicional:
  - Verificar repositórios para garantir que consultas e mutações sempre passam pelo filtro de tenant.
  - Garantir que seeds de roles/permissões/usuários atribuem `TenantId` consistentemente.

## Filtro global e testes
- Filtro EF: aplicar `HasQueryFilter(e => e.TenantId == _tenantContext.TenantId)` para todo `IMultiTenantEntity` no `OnModelCreating`.
- Guard rails:
  - Tests unitários para verificar que o filtro é aplicado em cada entity type multi-tenant.
  - Testes de integração: consulta sem tenant deve falhar; com tenant deve aplicar o filtro.
- Arquitetura/testes automatizados:
  - Verificar via reflexão que `TenantId` tem setter não público.
  - Verificar que toda entity multi-tenant implementa `IMultiTenantEntity`.
  - Opcional: validar presença do filtro global configurado no DbContext (ex.: inspecionando `Model.GetEntityTypes()`).

