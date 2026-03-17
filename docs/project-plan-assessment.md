# Product.Template — Estado Atual e Próximos Passos

Status: Atualizado
Data: 2026-03-17
Base: implementação completa em main + suíte de testes verde (63 testes)

## 1) Inventário de módulos

| Módulo | Domain | Application | Infrastructure | Testes integração |
|--------|:------:|:-----------:|:--------------:|:-----------------:|
| **Identity** | ✅ | ✅ | ✅ | ✅ |
| **Authorization** | ✅ | ✅ | ✅ | ✅ |
| **Tenants** | ✅ | ✅ | ✅ | ✅ |
| **Kernel (Shared)** | ✅ | ✅ | ✅ | — |

## 2) Estado atual por eixo

### 2.1 Arquitetura e módulos
- Clean Architecture respeitada em todos os módulos; regra de dependência validada por `ArchitectureTests` (27 testes).
- Padrões CQRS/MediatR uniformes: Commands, Queries, Handlers, Validators, Mappers, DTOs em todos os módulos.
- Módulo `Authorization` separado do `Identity` como bounded context próprio (Roles, Permissions, RolePermissions, UserAssignments).
- Módulo `Tenants` usa `HostDbContext` separado; entidades não carregam complexidade de filtros por tenant.

### 2.2 Multi-tenancy
- `AppDbContext` aplica filtro global `TenantId` via `ModelBuilderTenantExtensions` para todo `IMultiTenantEntity`.
- `MultiTenantSaveChangesInterceptor` injeta `TenantId` via `AssignTenant()` em toda entidade nova (SharedDb).
- Entidades multi-tenant de todos os módulos com `TenantId` setter privado e factories obrigatoriamente recebem `tenantId`.
- `TenantGuardMiddleware` bloqueia requisições sem tenant resolvido; exceções explícitas por endpoint.
- Seeds de tenants configuráveis via seção `Tenants` em `appsettings`.
- 3 modos suportados: `SharedDb`, `SchemaPerTenant`, `DedicatedDb`.

### 2.3 Autenticação/RBAC
- JWT + Refresh Token com rotação, IP tracking e expiração configurável.
- Fluxo completo de confirmação de e-mail: `ConfirmEmailCommand` → handler idempotente → endpoint público `POST /identity/{id}/confirm-email`.
- Auth externa (Microsoft OAuth) com `ExternalLoginCommand`.
- Catálogo de permissões canônico (`IPermissionCatalog`) com formato `{module}.{resource}.{action}`.
- `PermissionCatalogAuthorizationConfigurator` valida no boot que toda policy referencia um código registrado.
- RBAC_MATRIX cobre todos os 3 controllers (IdentityController, AuthorizationController, TenantsController).
- Owner-check contextual mantido via `UserReadOrSelf`/`UserManageOrSelf` para endpoints do Identity.

### 2.4 Domain Events
- `IDomainEvent : INotification` — eventos são MediatR notifications.
- `UnitOfWork.Commit()` coleta, limpa e publica todos os eventos dos aggregates via `IPublisher` após `SaveChangesAsync`.
- `UserRegisteredEvent` e `UserAssignedToRoleEvent` já definidos e sendo disparados.
- `UserRegisteredEventHandler` como exemplo documentado de consumidor de evento de domínio.
- Para adicionar um handler: implementar `INotificationHandler<TEvent>` — MediatR descobre automaticamente.

### 2.5 Observabilidade
- Serilog structured logging com sinks Console + File (rolling) + Seq.
- OpenTelemetry: tracing (ASP.NET Core, HttpClient, EF Core) + metrics (runtime + custom) + exporter OTLP.
- `RequestLoggingMiddleware` propaga `X-Correlation-ID`.
- Health checks: `/health/live` (liveness) e `/health/ready` (readiness, valida DB).
- ADR-007 define tags obrigatórias (TenantId, Module, Product, Operation) — enriquecimento consistente é oportunidade de melhoria contínua.

### 2.6 Persistência
- `EfModelAssemblyRegistry`: cada módulo de Infrastructure registra suas próprias configurações EF; `AppDbContext` compõe na inicialização.
- Design-time factories para `AppDbContext` e `HostDbContext` usam env var `ConnectionStrings__AppDb` / `ConnectionStrings__HostDb`, com detecção automática de provider (SQL Server / PostgreSQL / SQLite).
- Migrations em `Kernel.Infrastructure/Migrations/AppDb/` e `HostDb/`.
- Soft delete global via `ISoftDeletableEntity` + query filter `DeletedAt IS NULL`.
- Audit log automático via `AuditLogInterceptor` (captura Added/Modified/Deleted com old → new values em JSON).

### 2.7 Testes

| Projeto | Testes | Status |
|---------|:------:|:------:|
| UnitTests | 4 | ✅ Verde |
| IntegrationTests | 15 | ✅ Verde |
| ArchitectureTests | 27 | ✅ Verde |
| E2ETests | 17 | ✅ Verde |
| **Total** | **63** | ✅ |

- Integration tests usam SQLite in-memory (`AuthorizationHandlerTestFixture`) ou EF InMemory (`TenantsHandlerTestFixture`).
- `NoopPublisher` isola testes de integração de side-effects de domain events.
- E2E tests usam `WebApplicationFactory<Program>` + `TestAuthHandler`; `X-Test-Roles`/`X-Test-Permissions` headers para injetar claims.
- Architecture tests validam: dependências de camada, invariantes de tenancy (TenantId setter privado, IMultiTenantEntity), commands com validators, handlers isolados.

### 2.8 CI/CD e Docker
- Dockerfile multi-stage em `src/Api/Dockerfile`.
- Workflows em `.github/workflows/`.
- Docker Compose com SQL Server + Seq + API.
- **Pendente**: auditar workflows para `--locked-mode`, `timeout-minutes`, Trivy scan, proibição de tag `latest`.

## 3) Lacunas pendentes (baixa prioridade)

### 3.1 Escalabilidade
- ⚠️ **Idempotency**: `RequestDeduplicationMiddleware` usa `IMemoryCache` — não funciona em múltiplas réplicas K8s. Substituir por `IDistributedCache` (Redis ou SQL Server) antes de produção em cluster.
- ⚠️ **Rate limit por tenant**: implementação atual é apenas por IP. Adicionar policy `fixed-by-tenant` usando `ITenantContext` como partition key.

### 3.2 CI/CD
- Auditar `.github/workflows/` contra regras em `.ai/rules/13-docker.md`: `permissions` mínimos, `timeout-minutes`, `dotnet restore --locked-mode`, Trivy scan, proibição de tag `latest`.

### 3.3 Observabilidade avançada
- Enriquecer todos os logs/traces com `Module` e `Product` consistentemente (ADR-007).
- Health check incluindo validação de tenant resolver + catálogo de módulos/features.
- API de consulta de audit logs (`GET /audit?entityType=User&entityId=...`).

### 3.4 Manutenção
- Job de limpeza de refresh tokens expirados (> 90 dias).
- Job de hard delete de entidades soft-deleted (> 1 ano).
- Blacklist distribuída de refresh tokens para revogação instantânea em cluster.

## 4) Módulos futuros

Para adicionar um novo módulo, seguir o checklist em `.ai/checklists/` e o guia em `docs/guides/module-designer-quickstart.md`:
1. Criar os 3 projetos em `src/Core/{Module}/` (Domain, Application, Infrastructure).
2. Registrar permissões canônicas (`{module}.{resource}.{action}`) via `IPermissionCatalog`.
3. Registrar assembly EF no `EfModelAssemblyRegistry` no `DependencyInjection.cs`.
4. Adicionar `DependencyInjection.cs` da Infrastructure e wire em `CoreConfiguration.cs`.
5. Adicionar o assembly ao scan MediatR em `KernelConfigurations.cs`.
6. Atualizar `RBAC_MATRIX.md` com os novos endpoints.
7. Criar integration tests com fixture SQLite in-memory.

## 5) Referências
- ADR-003_isolamento_dados_multitenant.md
- ADR-004_catalogo_permissoes_rbac.md
- ADR-005_modulos_e_composicao_produto.md
- ADR-006_extensibilidade_e_governanca.md
- ADR-007_observabilidade_e_operacao.md
- TENANCY_HARDENING_PLAN.md
- docs/security/RBAC_MATRIX.md
- docs/ENTERPRISE_ANALYSIS.md
