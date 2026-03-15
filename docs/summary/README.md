# Product.Template — Frontend Summary

> Documentação de referência para equipes de front-end que consomem esta API.
> Atualizado em: Março 2026 | Versão da API: v1

---

## O que é este projeto

**Product.Template** é um backend .NET 10 que serve como base para produtos multi-tenant com autenticação JWT e controle de acesso baseado em roles/permissões (RBAC).

O front-end interage com este sistema para autenticar usuários, gerenciar perfis e controlar acesso a recursos com base nos papéis (roles) atribuídos a cada usuário.

---

## Índice

| Documento | Conteúdo |
|-----------|---------|
| [api-contracts.md](./api-contracts.md) | Todos os endpoints, payloads de request e response |
| [auth-guide.md](./auth-guide.md) | Fluxos de autenticação (JWT, Microsoft/OAuth2) e gestão de token |
| [rbac-guide.md](./rbac-guide.md) | Roles, permissões e o que cada perfil pode acessar |
| [error-handling.md](./error-handling.md) | Formato de erros, códigos HTTP e como tratar no front-end |
| [headers-and-conventions.md](./headers-and-conventions.md) | Headers obrigatórios, multi-tenancy, CORS e rate limiting |

---

## Visão geral da stack de comunicação

```
Frontend (SPA / Mobile)
        │
        │  HTTPS — JSON
        │  Header: Authorization: Bearer {token}
        │  Header: X-Tenant: {tenant-slug}
        ▼
API — https://{host}/api/v1/{controller}
        │
        ├── /identity/login          → Autenticação
        ├── /identity/register       → Cadastro
        ├── /identity/external-login → OAuth2 (Microsoft, Google)
        ├── /identity/providers      → Provedores disponíveis
        ├── /identity                → Gestão de usuários
        └── /identity/roles          → Gestão de roles
```

---

## URL base da API

| Ambiente | URL |
|----------|-----|
| Desenvolvimento | `http://localhost:8080/api/v1` |
| Staging | `https://staging.{domain}/api/v1` |
| Produção | `https://{domain}/api/v1` |

**Documentação interativa (Scalar/OpenAPI):** `{url-base-sem-api-v1}/scalar`

---

## Checklist mínimo de integração

Antes de iniciar o desenvolvimento do front-end, certifique-se de:

- [ ] Entender o fluxo de autenticação: [auth-guide.md](./auth-guide.md)
- [ ] Configurar o header `X-Tenant` em todas as requisições: [headers-and-conventions.md](./headers-and-conventions.md)
- [ ] Mapear as telas protegidas com base na tabela RBAC: [rbac-guide.md](./rbac-guide.md)
- [ ] Implementar o interceptor de erros globais: [error-handling.md](./error-handling.md)
- [ ] Implementar refresh ou redirect ao receber `401`
- [ ] Respeitar o rate limiting de 100 req/min por IP

