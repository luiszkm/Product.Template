# Contributing to Product.Template

## Before You Start

1. Read `README.md` for project overview and AI-first setup.
2. Read `.cursor/rules/global.mdc` — contains all conventions.
3. Read `.cursor/rules/architecture.mdc` for layer boundaries.
4. Invoke `/repo-layout` skill when file placement is unclear.

## Development Workflow

### 1. Pick a Task

- Check the issue tracker for open issues.
- For new features, create an issue first describing the scope.

### 2. Create a Branch

```bash
git checkout -b feature/{short-description}
# or
git checkout -b fix/{short-description}
```

### 3. Implement

Use the right Cursor skill for the task (see [Using AI Agents](#using-ai-agents-cursor)), or follow the `.cursor/rules/` for the relevant layer:

| Layer | Rule File |
|-------|-----------|
| Domain | `.cursor/rules/domain.mdc` |
| Application | `.cursor/rules/application.mdc` |
| Infrastructure | `.cursor/rules/infrastructure.mdc` |
| API | `.cursor/rules/api.mdc` |
| Tests | `.cursor/rules/tests.mdc` |
| Security | `.cursor/rules/security.mdc` |

### 4. Validate

Run through the appropriate checklist in `.agents/checklists/`:

- New feature → `.agents/checklists/new-feature.md`
- New endpoint → `.agents/checklists/api-endpoint.md`
- Database changes → `.agents/checklists/persistence.md`

### 5. Review (antes do PR)

Use `/review` no Cursor Chat para fazer um levantamento completo de brechas antes de abrir o PR:

```
Revise o código da feature {FEATURE} no módulo {MODULE}.
Escopo: src/Core/{Module}/ e src/Api/Controllers/v1/{Module}Controller.cs
```

### 6. Test

```bash
dotnet build
dotnet test
```

All tests must pass. Architecture tests enforce layer boundaries automatically.

### 7. Submit PR

Use `.agents/checklists/pull-request.md` as the review checklist.

---

## Using AI Agents (Cursor)

Skills live in `.cursor/skills/`. Invoke with `/skill-name` or the trigger phrases in `AGENTS.md`:

| Skill | Como usar | Quando usar |
|-------|-----------|-------------|
| `/new-module` | `/new-module Orders` | Desenhar bounded context DDD antes de codar |
| `/new-feature` | `/new-feature Catalog Product` | Scaffold de feature completa (entity → endpoint → tests) |
| `/optimize-query` | `/optimize-query src/Core/Identity/.../GetUserByIdQueryHandler.cs` | Diagnosticar N+1, over-fetching, propor otimização |
| `/review` | `/review src/Core/Catalog/` | Revisão local: segurança, arquitetura, testes |
| `/pr-review` | `/pr-review` (PR #N) | Revisão multi-agent em pull request GitHub |

### O que agentes devem ler antes de gerar código

Para modelos/agentes, instrua-os a ler:

1. `.cursor/rules/global.mdc` — regras globais
2. `.cursor/rules/architecture.mdc` — layer boundaries
3. `.cursor/rules/application.mdc` — padrão de handlers, validators, DTOs
4. `.cursor/rules/api.mdc` — padrão de controllers, RBAC
5. `.cursor/rules/infrastructure.mdc` — persistência, DI
6. A regra específica para a camada sendo modificada (`.cursor/rules/domain.mdc` a `.cursor/rules/security.mdc`)

---

## Code Style

- Enforced by `.editorconfig` — do not override.
- File-scoped namespaces.
- Private fields: `_camelCase`.
- All public members: PascalCase.
- Interfaces: `I` prefix.
- See `.cursor/rules/style.mdc` for complete guidelines.

## Commit Messages

Use conventional commits:

```
feat: add Catalog module with CRUD operations
fix: resolve N+1 query in ListUsersQueryHandler
test: add authorization tests for CatalogController
docs: update RBAC matrix for Catalog endpoints
refactor: extract shared pagination logic to base repository
```

## Adding a New Module

1. Create the project structure:
   ```
   src/Core/{Module}/
   ├── {Module}.Domain/{Module}.Domain.csproj
   ├── {Module}.Application/{Module}.Application.csproj
   └── {Module}.Infrastructure/{Module}.Infrastructure.csproj
   ```

2. Set project references per `.cursor/rules/architecture.mdc`.

3. Add projects to `Product.Template.sln`.

4. Register DI in `Api/Configurations/CoreConfiguration.cs`.

5. Register MediatR assembly in `Api/Configurations/KernelConfigurations.cs`.

6. Use `/new-feature` to scaffold the first feature.

7. Follow `.agents/checklists/new-feature.md` for completeness.

8. Run `/review` on the changed paths before opening the PR.

## RBAC Matrix

Every protected endpoint must be documented in `docs/security/RBAC_MATRIX.md`.

This is enforced by `tests/E2ETests/Security/RbacMatrixConsistencyTests.cs` — PRs that add endpoints without updating the matrix will fail CI.

## Questions?

If unsure about a pattern, look at the **Identity module** (`src/Core/Identity/`) — it's the canonical reference implementation.

For code review, use `/review` (local) or `/pr-review` (GitHub PR).
