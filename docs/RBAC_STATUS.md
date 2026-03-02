# Status atual do RBAC

## Resumo executivo (validação do template)
O template está com RBAC **implementado no nível de roles/policies para o módulo Identity**, incluindo proteção administrativa e regra de escopo para acesso a dados de usuário (self-or-admin).

### Implementado
- ✅ Claims de role no JWT (`ClaimTypes.Role`).
- ✅ `RoleClaimType` alinhado com o token na validação JWT.
- ✅ Policies registradas (`Authenticated`, `AdminOnly`, `UserOnly`) usando `RequireRole(...)`.
- ✅ Policies por permissão adicionadas (`UsersRead`, `UsersManage`) com claim `permission`.
- ✅ Endpoints administrativos de roles do usuário (`GET/POST/DELETE /identity/{id}/roles`) protegidos com `UsersManage`.
- ✅ CRUD de roles disponível por API (`GET/GET by id/POST/PUT/DELETE /identity/roles`).
- ✅ `GET /identity` protegido com `UsersRead`.
- ✅ Regra de escopo no controller para endpoints `UserOnly` (`GetById` e `UpdateUser`): acesso apenas ao próprio usuário, exceto Admin.
- ✅ Cobertura inicial de testes para política explícita e cenários de autorização no `IdentityController`.
- ✅ Testes de integração HTTP adicionados para cenários 401/403/200/404 em endpoints críticos de RBAC (incluindo `UsersManage`).
- ✅ Teste automatizado de consistência entre endpoints protegidos e `docs/security/RBAC_MATRIX.md` (governança).

## O que ainda falta para um RBAC “completo” de produção
1. **Expandir governança cross-módulos**
   - O gate de consistência já cobre Identity; aplicar o mesmo padrão para novos módulos/controllers no crescimento do template.

## Critério prático de conclusão no template atual
1. Endpoints sensíveis do Identity com policy explícita e escopo adequado.
2. Cobertura de testes automatizados para cenários permitidos e negados.
3. Matriz RBAC atualizada no repositório.
