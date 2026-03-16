# Product.Template — Plano de Estado Atual e Lacunas

Status: Draft
Data: 2026-03-16
Base: .github/copilot-instructions.md, ADR-003, TENANCY_HARDENING_PLAN.md, ADR-004..007

## 1) Inventário rápido
- Módulos core: **Identity** (`src/Core/Identity/`) com Domain, Application, Infrastructure já estruturados.
- Kernel compartilhado: `src/Shared/Kernel.{Domain|Application|Infrastructure}/` contendo AppDbContext, behaviors, interceptors e seed cross-cutting.
- API: `src/Api/` com `IdentityController` v1; pipeline de middleware, Serilog, OpenTelemetry, versionamento.
- Testes: projetos de arquitetura, unit, integration, e2e presentes, mas cobertura por feature precisa ser consolidada.
- Docs relevantes: `docs/security/RBAC_MATRIX.md`, ADR-003 (tenancy), ADR-004..007 (RBAC, módulos, observabilidade, governança), TENANCY_HARDENING_PLAN.md, CONTRATOS_FUNDACAO.md.

## 2) Estado atual por eixo
### 2.1 Arquitetura e módulos
- Clean Architecture respeitada na solução Identity; demais módulos ainda inexistentes.
- Convenções de commands/queries/validators/mappers alinhadas ao padrão Identity.
- Regra de dependência preservada (Domain <- Application <- Infrastructure <- Api) nas pastas inspecionadas.

### 2.2 Multi-tenancy
- `AppDbContext` aplica filtro global para `IMultiTenantEntity` via `ModelBuilderTenantExtensions`.
- Interceptor `MultiTenantSaveChangesInterceptor` injeta TenantId em entidades novas quando isolation SharedDb.
- Entidades multi-tenant do Identity (`User`, `Role`, `Permission`, `RolePermission`, `UserRole`, `RefreshToken`) ainda expõem `TenantId` com setter público e factories não recebem TenantId (gap apontado no TENANCY_HARDENING_PLAN).
- Middleware/resolução de tenant já existente (`ITenantContext`), porém ausência de guard explícito para bloquear requisições sem tenant resolvido (gap).

### 2.3 Autenticação/RBAC
- Policies atuais: Authenticated, AdminOnly, UserOnly, UsersRead, UsersManage, UserReadOrSelf/UserManageOrSelf (IdentityController).
- RBAC_MATRIX cobre endpoints do Identity, mas permissions ainda no formato legado (`users.read`, `users.manage`, `roles.manage`) — ADR-004 pede convergência para `{module}.{resource}.{action}`.
- Ownership-check contextual (UserReadOrSelf/UserManageOrSelf) implementado mas ainda não movido para policy canônica.

### 2.4 Observabilidade
- Serilog + OTEL configurados no API pipeline; RequestLoggingMiddleware adiciona CorrelationId.
- ADR-007 exige tags obrigatórias (TenantId, Module, Product, Operation) — ainda não verificado se todos logs/traces propagam TenantId e módulo (gap provável).
- Health checks existem, mas sem validação explícita de resolução de tenant/catalogos (gap conforme ADR-007).

### 2.5 Persistência
- Configurações EF centralizadas na Kernel.Infrastructure com AppDbContext e interceptors.
- Falta checar se todas entidades multi-tenant têm `IEntityTypeConfiguration` e índices contendo TenantId; provável ajuste necessário ao endurecer tenancy.
- Seeds: Identity provavelmente popula permissões/roles/usuários, mas não há seed de tenants/catalogo de módulos/features (gap ADR-003/005).

### 2.6 CI/CD e Docker
- Dockerfile multi-stage presente em `src/Api/Dockerfile` (revisão detalhada pendente contra regra .ai/rules/13-docker.md).
- Workflows em `.github/workflows/` existem; precisam validar requisitos: `permissions` mínimos, `timeout-minutes`, `dotnet restore --locked-mode`, Trivy scan, proibição de tag latest (gap de verificação).

### 2.7 Testes
- Projetos de testes presentes (Architecture, Unit, Integration, E2E). Cobertura específica por handler/validator/endpoint não inventariada; provável falta de testes para novos behaviors/tenancy hardening e RBAC catalogado.
- Integração: precisa garantir headers obrigatórios (`X-Tenant: public`) e testes de autorização (401/403/200) para endpoints protegidos; ausência a confirmar.
- Arquitetura: sugerido teste para garantir TenantId setter privado em IMultiTenantEntity e policy fora do catálogo (conforme ADR-004/TENANCY_HARDENING_PLAN).

## 3) Lacunas principais a endereçar
1. **Tenancy invariants**
   - Tornar `TenantId` setter privado em todas entidades multi-tenant; factories devem receber `tenantId` e atribuir.
   - Bloquear operações sem tenant resolvido (middleware ou behavior) exceto endpoints públicos explícitos.
   - Seeds de tenants iniciais e associação a permissões/módulos/features.
2. **RBAC canônico**
   - Migrar permissions para formato `{module}.{resource}.{action}`; alinhar policies e `RBAC_MATRIX`.
   - Criar `IPermissionCatalog` + enforcement (testes de arquitetura para policies fora do catálogo).
   - Ajustar endpoints `UserReadOrSelf/UserManageOrSelf` para policy/pipeline canônica (owner-check configurável).
3. **Observabilidade reforçada**
   - Enriquecer logs/traces/métricas com TenantId/Module/Product/Operation consistentemente.
   - Health checks: incluir validação de tenant resolver + catálogo de módulos/features.
4. **Persistência e EF**
   - Verificar/add `IEntityTypeConfiguration` por entidade (incluindo índices com TenantId, `ValueGeneratedNever`, ignore DomainEvents).
   - Garantir `DbSet` para novas entidades (quando surgirem) e `DependencyInjection` registrations por módulo.
   - Paginação/Include revisados para evitar N+1; confirmar em repositórios existentes.
5. **CI/CD e Docker compliance**
   - Auditar workflows para `permissions`, `timeout-minutes`, `dotnet restore --locked-mode`, Trivy scan, tag não-latest, deploy com environment.
   - Auditar Dockerfile para saúde (HEALTHCHECK /health/live, user non-root UID 1654, labels OCI, stages restore/publish/final).
6. **Testes faltantes**
   - Handlers/validators novos e críticos com happy/failure path.
   - Integração de authz: 401/403/200 e header `X-Tenant` obrigatório.
   - Arquitetura: testes para TenantId setter privado em IMultiTenantEntity, policies dentro do catálogo, commands com validators.

## 4) Plano de execução (ordem sugerida)
1. **Tenancy Hardening (Sprint 1)**
   - Atualizar entidades multi-tenant (Identity) para setter privado + factory com `tenantId`.
   - Ajustar interceptors/tests para cobrir atribuição; adicionar guard em pipeline/middleware para tenant obrigatório.
   - Criar seeds de tenants básicos e ajustar seeds existentes para preencher TenantId.
2. **RBAC Catalogação (Sprint 1-2)**
   - Implementar `IPermissionCatalog` + policies derivadas; migrar seeds para códigos canônicos.
   - Atualizar `docs/security/RBAC_MATRIX.md` para formato canônico e gerar teste de arquitetura de consistência.
3. **Observabilidade (Sprint 2)**
   - Adicionar enricher de TenantId/Module/Product/Operation em Serilog + OTEL; revisar pontos de log para dados sensíveis.
   - Expandir health checks para resolver tenant e verificar catálogos.
4. **Infra/EF e Seeds (Sprint 2)**
   - Revisar configurações EF por entidade; garantir índices com TenantId e ignorar DomainEvents.
   - Ajustar repositórios para Include/ThenInclude do aggregate e evitar IQueryable na superfície.
5. **CI/CD & Docker (Sprint 3)**
   - Auditar workflows para requisitos mínimos, adicionar Trivy scan e restore locked-mode.
   - Revisar Dockerfile: multi-stage, non-root UID 1654, HEALTHCHECK /health/live, labels OCI, args (VERSION, VCS_REF, BUILD_DATE), tag sem latest.
6. **Testes (contínuo)**
   - Cobertura mínima para cada handler/validator; integração de authz + tenancy headers; arquitetura para policies/tenant invariants.

## 5) Próximas ações imediatas
- Confirmar inventário de endpoints e policies no `IdentityController` e refleti-los no `RBAC_MATRIX` com nomes canônicos.
- Elaborar backlog técnico por estória incluindo testes e ajustes de DI/EF para tenancy.
- Preparar checklist de revisão para PRs conforme `.ai/checklists/pull-request.md` e ADRs citados.

## 6) Referências
- ADR-003_isolamento_dados_multitenant.md
- TENANCY_HARDENING_PLAN.md
- ADR-004_catalogo_permissoes_rbac.md
- ADR-005_modulos_e_composicao_produto.md
- ADR-006_extensibilidade_e_governanca.md
- ADR-007_observabilidade_e_operacao.md
- .github/copilot-instructions.md, backend/api/infrastructure instructions

