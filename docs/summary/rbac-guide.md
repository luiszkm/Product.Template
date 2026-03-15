# RBAC Guide — Frontend

> Como funciona o controle de acesso baseado em roles e permissões, e como aplicar no front-end.

---

## Conceitos

| Conceito | Descrição |
|----------|-----------|
| **Role** | Perfil de acesso — ex: `Admin`, `User`, `Manager` |
| **Permission** | Capacidade granular — ex: `users.read`, `users.manage` |
| **Policy** | Regra no backend que combina roles e/ou permissions |

O usuário autenticado carrega suas roles e permissions dentro do **JWT** (ver [auth-guide.md](./auth-guide.md#estrutura-do-jwt)).

---

## Roles do sistema

| Role | Descrição |
|------|-----------|
| `Admin` | Acesso total — pode fazer tudo |
| `User` | Usuário padrão — acessa e edita apenas o próprio perfil |
| `Manager` | Papel intermediário — pode ler dados de usuários |

---

## Permissions granulares

| Permission | Descrição |
|-----------|-----------|
| `users.read` | Pode listar e ler dados de usuários e roles |
| `users.manage` | Pode criar, editar, deletar usuários e gerenciar roles |

> Permissions são atribuídas como **claims** no JWT. Um usuário sem role `Admin` pode ter as mesmas capacidades se tiver a permission correspondente.

---

## Policies do backend (mapeamento)

| Policy | Quem pode | Claims necessários |
|--------|----------|-------------------|
| `Authenticated` | Qualquer usuário logado | Token JWT válido |
| `UserOnly` | Usuários com role User, Admin ou Manager | `role: User` **ou** `role: Admin` **ou** `role: Manager` |
| `AdminOnly` | Somente Admin | `role: Admin` |
| `UsersRead` | Admin **ou** quem tem permission | `role: Admin` **ou** `permission: users.read` |
| `UsersManage` | Admin **ou** quem tem permission | `role: Admin` **ou** `permission: users.manage` |

---

## Matriz de acesso por endpoint

| Ação | `Admin` | `Manager` | `User` | `permission: users.read` | `permission: users.manage` |
|------|:-------:|:---------:|:------:|:------------------------:|:---------------------------:|
| Ver providers | ✅ | ✅ | ✅ | ✅ | ✅ |
| Login / Register | ✅ | ✅ | ✅ | ✅ | ✅ |
| Ver próprio perfil | ✅ | ✅ | ✅ | — | — |
| Editar próprio perfil | ✅ | ✅ | ✅ | — | — |
| Ver perfil de outro usuário | ✅ | — | ❌ | — | — |
| Listar todos os usuários | ✅ | — | ❌ | ✅ | ✅ |
| Listar roles | ✅ | — | ❌ | ✅ | ✅ |
| Criar/editar/remover role | ✅ | — | ❌ | — | ✅ |
| Adicionar/remover role de usuário | ✅ | — | ❌ | — | ✅ |
| Deletar usuário | ✅ | — | ❌ | — | ✅ |

> **Owner-check**: endpoints marcados com `UserOnly` aplicam uma regra adicional no backend — mesmo que o usuário tenha a role correta, ele só pode acessar/editar **seus próprios dados** a não ser que seja `Admin`.

---

## Como usar no front-end

### 1. Lendo roles e permissions do JWT

```ts
// auth.utils.ts
import { jwtDecode } from 'jwt-decode';

interface JwtPayload {
  sub: string;
  email: string;
  role: string | string[];
  permission?: string | string[];
  exp: number;
}

function getTokenPayload(token: string): JwtPayload {
  return jwtDecode<JwtPayload>(token);
}

function getUserRoles(token: string): string[] {
  const payload = getTokenPayload(token);
  const role = payload.role;
  return Array.isArray(role) ? role : [role];
}

function getUserPermissions(token: string): string[] {
  const payload = getTokenPayload(token);
  const permission = payload.permission ?? [];
  return Array.isArray(permission) ? permission : [permission];
}
```

### 2. Funções de verificação de acesso

```ts
// rbac.utils.ts
export function hasRole(roles: string[], required: string | string[]): boolean {
  const required_ = Array.isArray(required) ? required : [required];
  return required_.some(r => roles.includes(r));
}

export function hasPermission(permissions: string[], required: string): boolean {
  return permissions.includes(required);
}

export function isAdmin(roles: string[]): boolean {
  return roles.includes('Admin');
}

export function canReadUsers(roles: string[], permissions: string[]): boolean {
  return isAdmin(roles) || hasPermission(permissions, 'users.read');
}

export function canManageUsers(roles: string[], permissions: string[]): boolean {
  return isAdmin(roles) || hasPermission(permissions, 'users.manage');
}

export function canAccessUserProfile(
  roles: string[],
  currentUserId: string,
  targetUserId: string
): boolean {
  return isAdmin(roles) || currentUserId === targetUserId;
}
```

### 3. Guard de rotas (React Router)

```tsx
// ProtectedRoute.tsx
import { Navigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

interface Props {
  children: React.ReactNode;
  requiredRole?: string;
  requiredPermission?: string;
}

export function ProtectedRoute({ children, requiredRole, requiredPermission }: Props) {
  const { isAuthenticated, roles, permissions } = useAuth();

  if (!isAuthenticated) return <Navigate to="/login" replace />;

  if (requiredRole && !hasRole(roles, requiredRole)) {
    return <Navigate to="/unauthorized" replace />;
  }

  if (requiredPermission && !hasPermission(permissions, requiredPermission)) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <>{children}</>;
}

// Uso nas rotas:
// <ProtectedRoute requiredPermission="users.read">
//   <UsersListPage />
// </ProtectedRoute>
```

### 4. Ocultando elementos de UI

```tsx
// Exemplo: esconder botão de "Gerenciar Usuários" para não-admins
function AdminPanel() {
  const { roles, permissions } = useAuth();

  return (
    <nav>
      <Link to="/profile">Meu Perfil</Link>

      {canReadUsers(roles, permissions) && (
        <Link to="/users">Usuários</Link>
      )}

      {canManageUsers(roles, permissions) && (
        <Link to="/users/manage">Gerenciar Roles</Link>
      )}

      {isAdmin(roles) && (
        <Link to="/admin">Painel Admin</Link>
      )}
    </nav>
  );
}
```

> ⚠️ **Importante**: ocultar elementos na UI é apenas UX — o backend **sempre** valida as permissões. Nunca confie somente no controle de UI para segurança.

---

## Fluxos de atribuição de roles

### Fluxo: Admin atribui role a um usuário

```
Admin abre lista de usuários (GET /identity)
    │
    ├── Clica em usuário
    │
    ├── GET /identity/{id}/roles  ← lista roles atuais
    │
    ├── Seleciona nova role no formulário
    │
    └── POST /identity/{id}/roles  { roleName: "Manager" }
```

### Fluxo: Admin remove role de um usuário

```
DELETE /identity/{id}/roles/{roleName}
```

---

## Roles padrão criadas no seed

Na inicialização do sistema, as seguintes roles já existem:

| Role | ID (seed) | Descrição |
|------|-----------|-----------|
| `Admin` | (gerado) | Administrador completo |
| `User` | (gerado) | Usuário padrão |
| `Manager` | (gerado) | Gerente |

> Novos usuários registrados via `/register` recebem automaticamente a role `User`.

