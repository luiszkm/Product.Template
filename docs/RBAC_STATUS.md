# Status atual do RBAC

## Resumo executivo
O projeto **já possui base sólida de RBAC**, mas ainda **não está completo** para cenários de produção com governança fina.

- ✅ Modelo de domínio de papéis e vínculo com usuário (`Role`, `UserRole`, `User.AddRole`)
- ✅ Seed inicial de papéis (`Admin`, `User`, `Manager`)
- ✅ JWT inclui roles como claims
- ✅ Políticas de autorização estão registradas
- ⚠️ Aplicação de autorização por papel/política ainda é parcial nos endpoints
- ⚠️ Faltam casos de uso administrativos (gerenciar papel de usuário, CRUD de roles)
- ⚠️ Falta camada de permissão granular (claims/permissões por recurso/ação)

## O que já existe (implementado)

### 1) Entidades e relacionamento RBAC
- Entidades `Role` e `UserRole` existem e mapeiam a relação usuário ↔ papel.
- O agregado `User` possui comportamento de `AddRole` / `RemoveRole`.

### 2) Seed inicial
- Há seed de papéis base: `Admin`, `User`, `Manager`.
- O seed de usuários associa papéis no bootstrap.

### 3) Token com role claims
- O serviço de JWT adiciona claims de role no token.

### 4) Políticas de autorização registradas
- Existem políticas `Authenticated`, `AdminOnly` e `UserOnly`.

## Lacunas para RBAC “completo”

### A. Aplicar RBAC nos endpoints de negócio
Hoje os endpoints usam majoritariamente `[Authorize]` sem papel/política específica.

**Falta:**
- aplicar `[Authorize(Policy = "AdminOnly")]` / `[Authorize(Roles = "Admin")]` onde necessário;
- separar claramente endpoints de leitura, operação e administração.

### B. Corrigir consistência de claims/policies
As policies estão com `RequireClaim("role", "admin")` e `"user"` (minúsculo), enquanto as roles seedadas são `Admin`/`User` e o token usa `ClaimTypes.Role`.

**Risco:** policy não casar com claim/valor esperado.

### C. Casos de uso de administração de acesso
Não há fluxo completo para gestão de autorização.

**Falta:**
- endpoint/command para atribuir/remover role de usuário;
- listagem de roles por usuário;
- CRUD/versionamento de roles (se aplicável ao produto).

### D. Permissões granulares (além de role)
Para “completo” em sistemas maiores, role-only tende a ser insuficiente.

**Falta (opcional recomendado):**
- modelo de `Permission` (ex.: `orders.read`, `orders.manage`);
- associação `RolePermission`;
- policies baseadas em permission claim.

### E. Testes específicos de autorização
Há testes para multi-tenant, mas não um pacote robusto de testes de autorização RBAC.

**Falta:**
- testes de integração validando 401/403 por role/policy;
- testes para garantir coerência de claim type/valor e policies.

## Definição prática de “RBAC completo” neste template
Considere “completo” quando o template entregar minimamente:
1. Policies/roles coerentes (claim type + valores);
2. Endpoints críticos protegidos por role/policy específica;
3. Fluxos de gestão de roles em usuários;
4. Testes automatizados de autorização (sucesso e negação).

## Prioridade sugerida (ordem de implementação)
1. **Alta:** alinhar claim/policy (evitar falso positivo de segurança).
2. **Alta:** aplicar políticas por endpoint crítico.
3. **Média:** comandos/endpoints de gestão de roles.
4. **Média:** testes de integração de autorização.
5. **Baixa/Média:** evoluir para permission-based access (quando necessário).
