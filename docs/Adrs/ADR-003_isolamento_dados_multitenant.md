ADR-003 — Isolamento de Dados Multi-Tenant
Status: Implementado
Data: 2026-03-16
Atualizado: 2026-03-17
Relacionada: ADR-002 — Plataforma Base SaaS Multi-Tenant

## 1. Contexto
- A foundation deve ser multi-tenant por padrão, com filtros e auditoria consistentes.
- Precisamos definir o modelo de isolamento mínimo (short-term) e a rota de evolução (mid/long-term).

## 2. Decisão proposta
- Adotar **DB compartilhado + filtro global de TenantId** como padrão inicial (simplicidade e custo).
- Manter o código pronto para evoluir para **schema por tenant** quando necessário (migração guiada por feature flag/config).
- Regras obrigatórias:
  - Todo agregado multi-tenant possui `TenantId` com setter privado, atribuído na factory.
  - `AppDbContext` aplica filtro global de TenantId em todas as entidades multi-tenant.
  - Auditar TenantId em inserts/updates; impedir operações sem TenantId resolvido.
  - Seeds obrigatórios para tenants, permissões, módulos/features por tenant.

## 3. Consequências
- (+) Simplicidade operacional inicial; compatível com foundation atual.
- (+) Rotas de evolução abertas para isolamento maior (schema-per-tenant) sem refatoração massiva.
- (-) Risco de noisy neighbors e necessidade de governança de performance.

## 4. Próximos passos
- Contratos/fluxo:
  - `ITenantResolver` (header `X-Tenant` + subdomínio) → `ITenantContext` (TenantId obrigatório).
  - `TenantContext` injetado em DbContext/UoW para filtros e auditoria.
  - Filtro global EF aplicado a todas as entidades que implementam `IMultiTenantEntity`.
- Invariantes planejados (hardening):
  - `TenantId` setter privado em entidades multi-tenant; atribuição em factories.
  - Seeds de tenants e associação inicial a módulos/features/permissões.
  - Bloquear operações sem TenantId resolvido (middleware/filtro antes do DbContext).
- Observabilidade/operabilidade:
  - Métricas/alertas por tenant: throughput, erros, latência p95/p99, login failures.
  - Health check inclui verificação de `TenantResolver` + conectividade ao catálogo de tenant.
- Inventário de entidades multi-tenant por módulo:
  - **Identity**: `User`, `RefreshToken`
  - **Authorization**: `Role`, `Permission`, `RolePermission`, `UserAssignment`
  - Todas têm `TenantId` com setter privado; atribuição obrigatória nas factories. ✅ Implementado.


