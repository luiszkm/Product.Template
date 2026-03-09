# 10 — Agent Behavior

> Instructions for LLMs and AI coding agents working in this repository.

## Before Generating Code

1. **Read `.ai/rules/00-global.md`** to understand the stack and principles.
2. **Read the rule file for the layer you are modifying** (e.g., `02-domain.md` for domain changes).
3. **Read `12-folder-structure.md`** to know where to place new files.
4. **Read `11-naming.md`** to ensure consistent naming.
5. If creating an endpoint, read `05-api.md` and `08-security.md`.
6. If creating tests, read `06-tests.md`.

## When Generating Code

### Structure
- Always provide the **full file path** relative to the repository root.
- Place files in the correct folder per `12-folder-structure.md`.
- Follow the established namespace pattern: `Product.Template.{Layer}.{Module}.{Folder}`.

### Code Quality
- Follow `.editorconfig` rules (file-scoped namespaces, `_camelCase` private fields, PascalCase for everything else).
- Use `record` for all DTOs, commands, queries.
- Use `sealed` for classes that are not base classes.
- Always accept `CancellationToken` in async methods.
- Always use `async`/`await` — never `.Result` or `.Wait()`.

### Dependencies
- Check existing NuGet packages before suggesting new ones.
- Check existing project references before adding new ones.
- Never add a reference from Domain to Infrastructure.

### Completeness
- For every **command**, also create: handler, validator, output DTO, mapper (if new entity), test.
- For every **query**, also create: handler, output DTO (reuse if exists), test.
- For every **endpoint**, also: update `docs/security/RBAC_MATRIX.md`, create integration test.
- For every **entity**, also create: EF configuration, repository interface, repository implementation, seeder (if needed).

## Response Format

When creating or modifying code, always structure your response as:

```
### Files Created
- `path/to/NewFile.cs` — brief description

### Files Modified
- `path/to/ExistingFile.cs` — what changed and why

### Tests
- `tests/UnitTests/Feature/HandlerTests.cs` — tests for the new handler

### RBAC Matrix Update
(if endpoints were added/modified)
```

## How to Handle Ambiguity

- If the user's request is ambiguous about which layer owns the logic, **put it in the domain** if it's a business rule, or **in the handler** if it's orchestration.
- If the user doesn't specify a module, ask — don't guess.
- If a pattern is unclear, **look at the Identity module** as the reference implementation.

## Common Mistakes to Avoid

1. ❌ Putting business logic in controllers.
2. ❌ Returning domain entities from handlers (return DTOs instead).
3. ❌ Creating `[Authorize]` without a `Policy`.
4. ❌ Forgetting `CancellationToken`.
5. ❌ Creating a handler that calls another handler.
6. ❌ Adding files outside the established folder structure.
7. ❌ Creating a repository for a child entity (only aggregate roots have repos).
8. ❌ Using `IQueryable<T>` in return types of repositories.
9. ❌ Forgetting to update `RBAC_MATRIX.md` when adding protected endpoints.
10. ❌ Forgetting to register new services in `DependencyInjection.cs`.

## Identity Module as Reference

The `Identity` module (`src/Core/Identity/`) is the canonical reference for:
- Domain entity structure → `Identity.Domain/Entities/`
- Command/Query organization → `Identity.Application/Handlers/` and `Identity.Application/Queries/`
- Repository implementation → `Identity.Infrastructure/Data/Persistence/`
- Controller design → `Api/Controllers/v1/IdentityController.cs`
- Seeders → `Identity.Infrastructure/Data/Seeders/`
- Authorization → `Api/Configurations/SecurityConfiguration.cs`

When in doubt, follow the Identity module's patterns.

