# Plano para completar o RBAC

## Objetivo
Evoluir o template de **RBAC base** para **RBAC completo e validado**, garantindo:
- proteção consistente por papel/política;
- gestão de papéis por fluxo de aplicação;
- testes automatizados de autorização;
- trilha de evolução para permissões granulares.

---

## Fase 0 — Baseline e alinhamento (1-2 dias)

### 0.1 Definir matriz de acesso (fonte de verdade)
Criar uma matriz simples por endpoint/caso de uso:
- recurso (ex.: usuários, tenants);
- ação (listar, ler, criar, atualizar, remover);
- perfis permitidos (`Admin`, `Manager`, `User`).

**Entregável:** tabela de autorização versionada no repositório (ex.: `docs/security/RBAC_MATRIX.md`).

### 0.2 Padronizar nomenclatura de roles
Fixar convenção única (case-sensitive): `Admin`, `Manager`, `User`.

**Critério de aceite:** nenhuma policy depende de valores divergentes (`admin` vs `Admin`).

---

## Fase 1 — Correções de consistência de segurança (alta prioridade, 1 dia)

### 1.1 Alinhar claim type/policies
Hoje o token usa `ClaimTypes.Role`; as policies devem consumir isso de forma consistente.

Ajustar `SecurityConfiguration` para usar:
- `RequireRole("Admin")` / `RequireRole("User", "Admin")`
  **ou**
- `RequireClaim(ClaimTypes.Role, "Admin")`.

### 1.2 Revisar configuração de validação de token
Garantir que `TokenValidationParameters.RoleClaimType` esteja coerente com o claim emitido.

**Critério de aceite:** usuário com role `Admin` passa na policy `AdminOnly` em teste automatizado.

---

## Fase 2 — Aplicação de RBAC nos endpoints (alta prioridade, 2-4 dias)

### 2.1 Classificar endpoints por criticidade
- Públicos (`[AllowAnonymous]`)
- Autenticados (`[Authorize]`)
- Administrativos (`[Authorize(Policy = "AdminOnly")]`)
- Operacionais (`[Authorize(Policy = "UserOnly")]` ou policy específica)

### 2.2 Aplicar políticas explicitamente
Substituir `[Authorize]` genérico nos endpoints críticos por policy/role explícita.

### 2.3 Criar policies por domínio (opcional recomendado)
Ex.: `Users.Read`, `Users.Manage`, `Tenants.Manage`.

**Critério de aceite:** todo endpoint sensível tem policy definida e documentada na matriz.

---

## Fase 3 — Gestão de roles via aplicação (média prioridade, 3-5 dias)

### 3.1 Casos de uso obrigatórios
- Atribuir role a usuário
- Remover role de usuário
- Listar roles do usuário
- (Opcional) CRUD de roles customizadas

### 3.2 Contratos/API
Adicionar endpoints administrativos com policy `AdminOnly`:
- `POST /users/{id}/roles`
- `DELETE /users/{id}/roles/{role}`
- `GET /users/{id}/roles`

### 3.3 Regras de negócio
- impedir duplicidade de role;
- bloquear remoção da última role administrativa do tenant (regra de segurança);
- registrar auditoria das mudanças de autorização.

**Critério de aceite:** fluxos de role funcionam ponta-a-ponta com validações de domínio.

---

## Fase 4 — Testes RBAC (alta prioridade, 2-3 dias)

### 4.1 Testes de integração de autorização
Cobrir cenários por endpoint:
- sem token → `401`;
- token sem role necessária → `403`;
- token com role correta → `2xx`.

### 4.2 Testes de policy/claim
- validar compatibilidade `RoleClaimType`;
- validar case-sensitive de roles;
- validar policies customizadas.

### 4.3 Testes de regressão de segurança
Tabela mínima de regressão para endpoints críticos (admin e escrita).

**Critério de aceite:** suite de autorização executa no CI e bloqueia regressões.

---

## Fase 5 — Observabilidade e governança (média prioridade, 1-2 dias)

### 5.1 Auditoria
Registrar eventos de autorização sensíveis:
- role atribuída/removida;
- acesso negado por policy;
- alteração de role administrativa.

### 5.2 Telemetria
Métricas sugeridas:
- `authz_denied_total` por policy/endpoint;
- `role_change_total` por tenant;
- tempo de resposta de endpoints administrativos.

---

## Fase 6 — Evolução para permissões granulares (opcional, 4-8 dias)

### 6.1 Modelo de permissões
Introduzir:
- `Permission` (`users.read`, `users.manage`, ...)
- `RolePermission`
- bootstrap de permissões por role padrão.

### 6.2 Policies dinâmicas
Resolver policies por permission claim (`RequireAssertion` / custom policy provider).

### 6.3 Migração incremental
Manter compatibilidade com roles atuais até completar transição.

**Critério de aceite:** endpoints podem autorizar por permissão sem quebrar fluxo atual por role.

---

## Backlog técnico objetivo (checklist)
- [x] Corrigir `AdminOnly` e `UserOnly` para `RequireRole(...)` (ou equivalente consistente).
- [x] Definir `RoleClaimType` explicitamente na validação JWT.
- [x] Mapear todos os endpoints e aplicar policy explícita.
- [x] Implementar comandos/handlers para atribuição e remoção de roles.
- [x] Expor endpoints administrativos de role management.
- [x] Criar testes integração/autorização base por endpoint crítico (policy coverage e endpoints administrativos).
- [x] Adicionar auditoria para mudanças de role.
- [x] Publicar matriz RBAC no repositório.
- [x] Adicionar métricas de autorização (denied/assign/revoke).
- [x] Adicionar testes HTTP de integração (401/403/200) com host real.
- [x] Adicionar teste automatizado de consistência entre endpoints protegidos e matriz RBAC (Identity).

---

## Definição de pronto (DoD)
RBAC será considerado completo no template quando:
1. policies e claims estiverem 100% consistentes;
2. endpoints críticos estiverem protegidos por policy explícita;
3. existir fluxo administrativo de gestão de roles;
4. houver testes automatizados de autorização no CI;
5. documentação (matriz + guia de uso) estiver atualizada.

---

## Sequência recomendada de execução (sprint)
1. **Sprint 1:** Fase 1 + início Fase 2 (consistência + endpoints críticos)
2. **Sprint 2:** concluir Fase 2 + Fase 3 (aplicação + gestão de roles)
3. **Sprint 3:** Fase 4 + Fase 5 (testes + observabilidade)
4. **Sprint 4 (opcional):** Fase 6 (permissões granulares)
