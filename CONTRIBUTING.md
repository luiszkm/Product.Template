# Contributing to Product.Template

## Before You Start

1. Read `README.md` for project overview.
2. Read `.ai/rules/00-global.md` for stack and principles.
3. Read `.ai/rules/01-architecture.md` for layer boundaries.
4. Read `.ai/rules/12-folder-structure.md` for file placement.

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

Follow the `.ai/rules/` for the relevant layer:

| Layer | Rule File |
|-------|-----------|
| Domain | `.ai/rules/02-domain.md` |
| Application | `.ai/rules/03-application.md` |
| Infrastructure | `.ai/rules/04-infrastructure.md` |
| API | `.ai/rules/05-api.md` |
| Tests | `.ai/rules/06-tests.md` |
| Security | `.ai/rules/08-security.md` |

### 4. Validate

Run through the appropriate checklist in `.ai/checklists/`:

- New feature → `.ai/checklists/new-feature.md`
- New endpoint → `.ai/checklists/api-endpoint.md`
- Database changes → `.ai/checklists/persistence.md`

### 5. Test

```bash
dotnet build
dotnet test
```

All tests must pass. Architecture tests enforce layer boundaries automatically.

### 6. Submit PR

Use `.ai/checklists/pull-request.md` as the review checklist.

## Using AI Agents

### Instructing an Agent

Copy the content of `.ai/prompts/master-prompt.md` as the system prompt, then use the specific prompt for your task:

| Task | Prompt |
|------|--------|
| New feature | `.ai/prompts/create-feature.md` |
| New entity | `.ai/prompts/create-entity.md` |
| New endpoint | `.ai/prompts/create-endpoint.md` |
| New command | `.ai/prompts/create-command.md` |
| New query | `.ai/prompts/create-query.md` |
| Review code | `.ai/prompts/review-feature.md` |
| Optimize query | `.ai/prompts/optimize-query.md` |
| Create migration | `.ai/prompts/create-migration.md` |

### What Agents Must Read

Before generating code, agents should read (in order):
1. `.ai/rules/00-global.md`
2. `.ai/rules/10-agent-behavior.md`
3. The specific rule file for the layer being modified
4. `.ai/rules/12-folder-structure.md`
5. `.ai/rules/11-naming.md`

## Code Style

- Enforced by `.editorconfig` — do not override.
- File-scoped namespaces.
- Private fields: `_camelCase`.
- All public members: PascalCase.
- Interfaces: `I` prefix.
- See `.ai/rules/07-style.md` for complete guidelines.

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

2. Set project references per `.ai/rules/01-architecture.md`.

3. Add projects to `Product.Template.sln`.

4. Register DI in `Api/Configurations/CoreConfiguration.cs`.

5. Register MediatR assembly in `Api/Configurations/KernelConfigurations.cs`.

6. Follow `.ai/checklists/new-feature.md` for completeness.

## RBAC Matrix

Every protected endpoint must be documented in `docs/security/RBAC_MATRIX.md`.

This is enforced by `RbacMatrixConsistencyTests` — PRs that add endpoints without updating the matrix will fail CI.

## Questions?

If unsure about a pattern, look at the **Identity module** (`src/Core/Identity/`) — it's the canonical reference implementation.

