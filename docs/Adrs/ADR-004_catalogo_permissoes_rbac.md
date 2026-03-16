ADR-004 — Catálogo de Permissões RBAC
Status: Proposta
Data: 2026-03-16
Relacionada: ADR-002 — Plataforma Base SaaS Multi-Tenant

## 1. Contexto
- RBAC é capacidade central; precisamos de um catálogo único, formatado e registrável por módulo/produto.
- Permissões atuais do Identity não seguem ainda um catálogo formal por módulo.

## 2. Decisão proposta
- Formato canônico de permissão: `{module}.{resource}.{action}` (lowercase, snake-case opcional para multi-palavras).
- Catálogo central: fonte de verdade para geração de policies e documentação (`docs/security/RBAC_MATRIX.md`).
- Registro de permissões:
  - Cada módulo expõe sua lista de permissões e a registra no bootstrap.
  - Produto (ex.: ERP) agrega permissões de seus módulos no start.
- Policies são derivadas do catálogo (evitar policies ad-hoc fora do padrão).

## 3. Consequências
- (+) Consistência de naming, fácil auditoria e documentação.
- (+) Policies rastreáveis ao catálogo; matriz RBAC sempre atualizável.
- (-) Exige disciplina e testes para garantir que novos endpoints só usem permissões catalogadas.

## 4. Próximos passos
- Contratos/fluxo:
  - `IPermissionCatalog` com registro de `PermissionDescriptor` (Code, Module, Resource, Action, Description).
  - Registro obrigatório em bootstrap de módulo/produto; policies derivadas do catálogo.
  - Geração/atualização de `docs/security/RBAC_MATRIX.md` a partir do catálogo (manualmente ou script).
- Naming canônico (exemplos):
  - `identity.user.read`, `identity.user.manage`
  - `finance.invoice.read`, `finance.invoice.create`, `sales.order.update`
- Testes/guardrails:
  - Architecture test para proibir `[Authorize(Policy=...)]` fora do catálogo.
  - Verificar que todo endpoint protegido referencia permission/policy canônica.
- Checklist imediato:
  - Mapear todas as permissões atuais do módulo Identity para o formato canônico.
  - Definir catálogo inicial para módulos ERP (finance, sales, inventory, purchasing).
  - Atualizar RBAC_MATRIX com o novo formato e criar teste de arquitetura correspondente.

## 5. Inventário atual — Identity
- Permissões seed atuais (Infrastructure):
  - `users.read` → alvo canônico: `identity.user.read`
  - `users.manage` → alvo canônico: `identity.user.manage`
  - `roles.manage` → alvo canônico: `identity.role.manage`
- Policies atuais:
  - `UsersRead` (role Admin ou claim `permission=users.read`) → alinhar para policy derivada de `identity.user.read`.
  - `UsersManage` (role Admin ou claim `permission=users.manage`) → alinhar para policy derivada de `identity.user.manage` e `identity.role.manage`.
- Ações necessárias:
  - Refletir os códigos canônicos na seed e nas policies (quando for a fase de implementação).
  - Atualizar RBAC_MATRIX para usar os códigos canônicos (rascunho já incluído).
  - Criar teste de arquitetura para garantir que toda policy referencie permissão catalogada.



