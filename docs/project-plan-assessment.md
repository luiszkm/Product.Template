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
- "Tenancy Hardening" parcialmente aplicado: entidades multi-tenant atualizadas com `TenantId` imutável e seeds configuráveis de tenants já disponíveis no `appsettings`.

## 2) Estado atual por eixo
### 2.1 Arquitetura e módulos
- Clean Architecture respeitada na solução Identity; demais módulos ainda inexistentes.
- Convenções de commands/queries/validators/mappers alinhadas ao padrão Identity.
- Regra de dependência preservada (Domain <- Application <- Infrastructure <- Api) nas pastas inspecionadas.

### 2.2 Multi-tenancy
- `AppDbContext` aplica filtro global para `IMultiTenantEntity` via `ModelBuilderTenantExtensions`.
- Interceptor `MultiTenantSaveChangesInterceptor` injeta TenantId em entidades novas quando isolation SharedDb.
- Entidades multi-tenant do Identity (`User`, `Role`, `Permission`, `RolePermission`, `UserRole`, `RefreshToken`) **já expõem `TenantId` somente-leitura e factories recebem `tenantId`** (ação concluída).
- Guard explícito adicionado (`TenantGuardMiddleware`) para bloquear requisições sem tenant resolvido; comportamento complementar ao pipeline MediatR.
- Seeds de tenants configuráveis via seção `Tenants` em `appsettings`; rotina de inicialização já provisiona e seed por tenant ativo.

### 2.3 Autenticação/RBAC
- Policies atuais: Authenticated, AdminOnly, UserOnly, UsersRead, UsersManage, UserReadOrSelf/UserManageOrSelf (IdentityController).
- RBAC_MATRIX cobre endpoints do Identity e agora usa permissões canônicas (`identity.user.read`, `identity.user.manage`, etc.).
- Catalogação inicial implementada (`IPermissionCatalog`, `IdentityPermissions` + seeder). A API agora configura as policies via `PermissionCatalogAuthorizationConfigurator`, que valida se cada permission code existe no catálogo antes de concluir o boot.
- Ownership-check contextual (UserReadOrSelf/UserManageOrSelf) permanece vigente e deve ser incorporado à estratégia canônica de policies.

### 2.4 Observabilidade
- Serilog + OTEL configurados no API pipeline; RequestLoggingMiddleware adiciona CorrelationId.
- ADR-007 exige tags obrigatórias (TenantId, Module, Product, Operation) — ainda não verificado se todos logs/traces propagam TenantId e módulo (gap provável).
- Health checks existem, mas sem validação explícita de resolução de tenant/catalogos (gap conforme ADR-007).

### 2.5 Persistência
- Configurações EF centralizadas na Kernel.Infrastructure com AppDbContext e interceptors.
- Entidades multi-tenant atualizadas para `TenantId` imutável; `MultiTenantSaveChangesInterceptor` usa `AssignTenant` e garante carimbo único por insert.
- Falta checar se todas entidades multi-tenant têm `IEntityTypeConfiguration` e índices contendo TenantId; provável ajuste necessário ao endurecer tenancy.
- Seeds: Identity popula permissões/roles/usuários; agora também há seed configurável de tenants, mas ainda não existe catálogo de módulos/features (gap ADR-003/005).

### 2.6 CI/CD e Docker
- Dockerfile multi-stage presente em `src/Api/Dockerfile` (revisão detalhada pendente contra regra .ai/rules/13-docker.md).
- Workflows em `.github/workflows/` existem; precisam validar requisitos: `permissions` mínimos, `timeout-minutes`, `dotnet restore --locked-mode`, Trivy scan, proibição de tag latest (gap de verificação).

### 2.7 Testes
- Projetos de testes presentes (Architecture, Unit, Integration, E2E). Cobertura específica por handler/validator/endpoint não inventariada; provável falta de testes para novos behaviors/tenancy hardening e RBAC catalogado.
- Integração: precisa garantir headers obrigatórios (`X-Tenant: public`) e testes de autorização (401/403/200) para endpoints protegidos; ausência a confirmar.
- Arquitetura: sugerido teste para garantir TenantId setter privado em IMultiTenantEntity e policy fora do catálogo (conforme ADR-004/TENANCY_HARDENING_PLAN).

## 3) Lacunas principais a endereçar
1. **Tenancy invariants (em andamento)**
   - ✅ Entidades multi-tenant com setter privado e factories exigindo `tenantId`.
   - ✅ Interceptor atualizado + guard middleware para tenant obrigatório.
   - 🔜 Seeds adicionais de tenants/módulos/features e testes arquiteturais para garantir invariantes.
2. **RBAC canônico**
- ✅ Permissions já seguem `{module}.{resource}.{action}` e `RBAC_MATRIX` está alinhada.
- ✅ Policies da API agora dependem do catálogo via `PermissionCatalogAuthorizationConfigurator`, impedindo códigos fora do inventário canônico.
- 🔜 Criar teste arquitetural para policies fora do catálogo e expandir o catálogo para futuros módulos.
- 🔜 Ajustar endpoints `UserReadOrSelf/UserManageOrSelf` para policy/pipeline canônica (owner-check configurável, reutilizável).
3. **Observabilidade reforçada**
   - Enriquecer logs/traces/métricas com TenantId/Module/Product/Operation consistentemente.
   - Health checks: incluir validação de tenant resolver + catálogo de módulos/features.
4. **Persistência e EF**
- Revisar configurações EF por entidade; garantir índices com TenantId, `ValueGeneratedNever`, ignore DomainEvents.
- Ajustar repositórios para Include/ThenInclude do aggregate e evitar IQueryable na superfície.
- Garantir que seeds multi-tenant propaguem dados específicos por tenant além do baseline Identity.
5. **CI/CD & Docker (Sprint 3)**
   - Auditar workflows para requisitos mínimos, adicionar Trivy scan e restore locked-mode.
   - Revisar Dockerfile: multi-stage, non-root UID 1654, HEALTHCHECK /health/live, labels OCI, args (VERSION, VCS_REF, BUILD_DATE), tag sem latest.
6. **Testes (contínuo)**
   - Handlers/validators novos e críticos com happy/failure path.
   - Integração de authz: 401/403/200, header `X-Tenant: public` obrigatório e cobertura do novo `TenantGuardMiddleware`.
   - Arquitetura: testes para TenantId setter privado em IMultiTenantEntity, policies dentro do catálogo, commands com validators.

## 5) Próximas ações imediatas
- Confirmar inventário de endpoints e policies no `IdentityController` e refleti-los no `RBAC_MATRIX` com nomes canônicos (agora bloqueado em runtime pelo configurador, mas ainda precisa de arquitetura/teste).
- Elaborar backlog técnico por estória incluindo testes e ajustes de DI/EF para tenancy.
- Preparar checklist de revisão para PRs conforme `.ai/checklists/pull-request.md` e ADRs citados.
- Implementar teste arquitetural para validar uso exclusivo de permissões registradas e publicar guia de adoção do catálogo para novos módulos.
- **Planejar sprint focada em RBAC canônico + observabilidade após o hardening de tenancy (seção 3.2 e 3.3).**

## 6) Referências
- ADR-003_isolamento_dados_multitenant.md
- TENANCY_HARDENING_PLAN.md
- ADR-004_catalogo_permissoes_rbac.md
- ADR-005_modulos_e_composicao_produto.md
- ADR-006_extensibilidade_e_governanca.md
- ADR-007_observabilidade_e_operacao.md
- .github/copilot-instructions.md, backend/api/infrastructure instructions
