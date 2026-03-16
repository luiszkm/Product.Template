ADR-005 — Módulos e Composição de Produto
Status: Proposta
Data: 2026-03-16
Relacionada: ADR-002 — Plataforma Base SaaS Multi-Tenant

## 1. Contexto
- A foundation precisa suportar módulos habilitáveis por tenant e composição de produtos (ERP primeiro consumidor).
- Hoje não há catálogo de módulos/features nem contrato de composição.

## 2. Decisão proposta
- Introduzir catálogo de módulos e features:
  - `ModuleCatalog` (lista de módulos disponíveis).
  - `TenantModuleCatalog` (módulos habilitados por tenant).
  - `FeatureCatalog`/`TenantFeatureCatalog` (features habilitadas por módulo e tenant).
- Ponto de extensão de produto:
  - `IProductComposition` registra módulos, permissões, features, endpoints e jobs.
  - `IProductModule` descreve nome, permissões, features, registros de DI e endpoints do módulo.
- Enforcement:
  - Gate de módulo/feature em controllers/handlers.
  - Decisão: pipeline MediatR antes do handler para comandos/queries; filtro em controllers para endpoints que não usam MediatR (fallback).
  - APIs/admin para habilitar/desabilitar módulos/features por tenant.

## 3. Consequências
- (+) Permite habilitar/disable módulos por tenant e compor produtos diferentes na mesma foundation.
- (+) Facilita bootstrap do ERP como primeiro produto sem acoplar à foundation.
- (-) Requer persistência/caching de módulos/features e disciplina de gating em runtime.

## 4. Próximos passos
- Contratos (ref): `IModuleCatalog`, `ITenantModuleCatalog`, `IModuleDescriptor`, `IFeatureDescriptor`, `IProductComposition`, `IProductModule`.
- Enforcement: decidido → pipeline MediatR antes do handler para comandos/queries; filtro em controllers para endpoints que não usam MediatR (fallback).
- Persistência/cache:
  - Tabelas para módulos, features e habilitação por tenant.
  - Cache por tenant com invalidação por mudança de habilitação (ex.: cache por TenantId + ModuleId com chave ETag para bust).
- Administração:
  - APIs para habilitar/desabilitar módulo/feature por tenant (protegidas por RBAC catalogado). Sugestão: 
    - `POST /tenants/{tenantId}/modules/{moduleCode}:enable`
    - `DELETE /tenants/{tenantId}/modules/{moduleCode}`
    - `POST /tenants/{tenantId}/modules/{moduleCode}/features/{featureCode}:enable`
    - `DELETE /tenants/{tenantId}/modules/{moduleCode}/features/{featureCode}`
- Composição ERP (exemplo):
  - Módulos Finance, Sales, Inventory, Purchasing registrados via `IProductComposition`.
  - Cada módulo registra permissões e features no catálogo em bootstrap.
- Checklist imediato:
  - Rascunhar modelo de storage/cache e endpoints admin mínimos (habilitar/desabilitar por tenant) — ver sugestões acima.
  - Criar documento de bootstrap ERP com módulos, permissões e features iniciais.
  - Desenhar teste de integração/pipeline que nega acesso quando módulo/feature desabilitado para o tenant.





