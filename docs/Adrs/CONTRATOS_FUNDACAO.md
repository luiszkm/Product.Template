CONTRATOS DA FOUNDATION — Módulos, Permissões e Composição
Status: Draft
Data: 2026-03-16
Base: ADR-002, ADR-003..007 (propostas)

## Objetivo
Documentar contratos de extensão sem alterar estrutura de pastas, servindo como referência rápida para implementação futura.

## Catálogo de Permissões (RBAC)
- Formato: `{module}.{resource}.{action}`.
- Contrato sugerido: `IPermissionCatalog`
  - `RegisterPermissions(IEnumerable<PermissionDescriptor> permissions)`
  - `IEnumerable<PermissionDescriptor> GetAll()`
  - `PermissionDescriptor` inclui: `Code`, `Description`, `Module`, `Resource`, `Action`.
- Fluxo: cada módulo registra suas permissões no bootstrap; produto agrega e ativa policies derivadas.
- Exemplos de códigos: `identity.user.read`, `identity.user.manage`, `finance.invoice.read`, `sales.order.create`.
- Guardrails: tests para garantir que `[Authorize(Policy=...)]` referencie apenas permissões catalogadas.
- Checklist imediato:
  - Mapear permissões atuais do Identity para `{module}.{resource}.{action}` e atualizar `docs/security/RBAC_MATRIX.md`.
  - Definir catálogo inicial para novos módulos (ERP: finance, sales, inventory, purchasing).
  - Criar teste de arquitetura que falha se houver policy fora do catálogo.

## Catálogo de Módulos/Features
- Contratos sugeridos:
  - `IModuleCatalog` (lista de módulos disponíveis).
  - `ITenantModuleCatalog` (módulos habilitados por tenant).
  - `IFeatureCatalog` / `ITenantFeatureCatalog` (features por módulo e tenant).
  - `IModuleDescriptor` (Name, Version opcional, Permissions, Features, Endpoints info).
  - `IFeatureDescriptor` (Name, Description, DependsOn opcional).
- Persistência: registrar módulos/features e estados por tenant; cache com invalidação por tenant.
- Enforcement: gate de módulo/feature em controllers/handlers (filtro ou pipeline MediatR).
- Preferência: pipeline MediatR antes do handler para comandos/queries; filtro para endpoints sem MediatR.
- Checklist imediato:
  - Decidir enforcement final e registrar no ADR-005.
  - Definir modelo de storage/cache por tenant (tabelas de habilitação + cache com invalidação).
  - Descrever APIs admin mínimas para habilitar/desabilitar módulos/features por tenant.

## Composição de Produto
- Contratos sugeridos:
  - `IProductComposition`
    - `RegisterModules(IServiceCollection services)`
    - `RegisterPermissions(IPermissionCatalog catalog)`
    - `RegisterFeatures(IFeatureCatalog catalog)`
    - `RegisterEndpoints(IEndpointRouteBuilder endpoints)`
    - `RegisterJobs(IJobScheduler scheduler)`
  - `IProductModule`
    - `Name`
    - `RegisterServices(IServiceCollection services)`
    - `RegisterPermissions(IPermissionCatalog catalog)`
    - `RegisterFeatures(IFeatureCatalog catalog)`
    - `RegisterEndpoints(IEndpointRouteBuilder endpoints)`
- ERP como primeiro produto: composition agrega módulos Finance, Sales, Inventory, Purchasing.
- Checklist imediato:
  - Criar documento de bootstrap ERP listando módulos e permissões/features iniciais.
  - Definir ordem de registro (módulos → permissões → features → endpoints → jobs).

## Tenancy (resumo dos invariantes)
- `TenantId` em todas as entidades multi-tenant com setter privado; atribuição via factory.
- Filtro global de TenantId no `AppDbContext` e auditoria de TenantId em alterações.
- `ITenantResolver` + `ITenantContext` centralizam resolução (header/subdomínio) e exposição do tenant atual; operações sem TenantId resolvido devem ser bloqueadas.
- Seeds obrigatórios: tenants e associação inicial a módulos/features/permissões.
- Checklist imediato:
  - Revisar entidades multi-tenant e planejar setter privado + atribuição em factory.
  - Documentar filtro global no `AppDbContext` e audit de TenantId; bloquear sem TenantId resolvido.
  - Definir seeds mínimos de tenants e vinculação inicial a módulos/features/permissões.

## Observabilidade
- Tags obrigatórias em logs/traces/métricas: `CorrelationId`, `TenantId`, `UserId`, `Module`, `Product`, `Operation`, `Environment`.
- Middleware de correlação + enrichment para Tenant/Module/User; health checks para resolver de tenant e catálogos.

## Governança
- Toda nova capacidade estrutural requer ADR.
- Foundation não depende de produtos; produtos dependem da foundation.
- Tests de arquitetura para:
  - Proibir foundation → produto.
  - Garantir validators para todos os commands.
  - Garantir entidades multi-tenant com invariantes de TenantId.


