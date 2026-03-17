ADR-004 — Catálogo de Permissões RBAC
Status: Implementado
Data: 2026-03-16
Atualizado: 2026-03-17
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

## 5. Inventário implementado

### Identity (`IdentityPermissions`)
| Código canônico | Policy |
|----------------|--------|
| `identity.user.read` | `UsersRead`, `UserReadOrSelf` |
| `identity.user.manage` | `UsersManage`, `UserManageOrSelf` |

### Authorization (`AuthorizationPermissions`)
| Código canônico | Policy |
|----------------|--------|
| `authorization.role.read` | `AuthorizationRolesRead` |
| `authorization.role.manage` | `AuthorizationRolesManage` |
| `authorization.permission.read` | `AuthorizationPermissionsRead` |
| `authorization.permission.manage` | `AuthorizationPermissionsManage` |

### Tenants (`TenantPermissions`)
| Código canônico | Policy |
|----------------|--------|
| `tenants.read` | `TenantsRead` |
| `tenants.manage` | `TenantsManage` |

### Implementação
- ✅ `IPermissionCatalog` + `PermissionDescriptor` implementados em `Kernel.Application`.
- ✅ Cada módulo registra seu catálogo via `IPermissionCatalogSeeder` no bootstrap.
- ✅ `PermissionCatalogAuthorizationConfigurator` valida no boot que toda policy usa código registrado.
- ✅ `RBAC_MATRIX.md` atualizada com todos os endpoints e permissões canônicas.
- ✅ Architecture tests validam que commands têm validators e camadas respeitam dependências.
- 🔜 Architecture test específico para garantir que `[Authorize(Policy=...)]` só referencia políticas derivadas do catálogo.



