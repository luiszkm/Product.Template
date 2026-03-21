---
description: Scaffold a CQRS Query + Handler + Output DTO + Mapper + Unit Tests
tools: Read, Edit, Write, Glob, Grep
disable-model-invocation: true
---

# Skill: /new-query

> Creates a complete CQRS query slice: query record, handler, output DTO record, mapper extension, and unit tests.

## Arguments

`$ARGUMENTS` format: `{MODULE_NAME} {QUERY_NAME}`

Example: `/new-query Identity GetUserById`

Where `{QUERY_NAME}` is the full query name without the `Query` suffix (e.g., `GetUserById` → generates `GetUserByIdQuery`).

## Context — read these files before generating any code

- `.ai/rules/03-application.md`
- `.ai/rules/11-naming.md`
- `.ai/rules/12-folder-structure.md`
- `.ai/prompts/create-query.md`
- `src/Core/Identity/Identity.Application/Queries/` — canonical reference

## Instruction

Parse `$ARGUMENTS` as `MODULE_NAME` (first token) and `QUERY_NAME` (second token, without `Query` suffix).

Determine `NOUN` from the query name:
- `GetUserById` → noun is `User`
- `ListActiveOrders` → noun is `Order`

Determine if this is a paginated list query (name starts with `List`).

Create these files:

### 1. Query record
**Path:** `src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/Queries/{NOUN}/Queries/{QUERY_NAME}Query.cs`

- `record` type implementing `IQuery<{NOUN}Output>` for single items
- For paginated lists: inherit from `ListInput` AND implement `IQuery<PaginatedListOutput<{NOUN}Output>>`
- Use file-scoped namespace

### 2. Query Handler
**Path:** `src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/Queries/{NOUN}/{QUERY_NAME}QueryHandler.cs`

- Implement `IQueryHandler<{QUERY_NAME}Query, {NOUN}Output>` (or paginated variant)
- Inject the repository interface only
- **NEVER call `IUnitOfWork.Commit()`** — queries are strictly read-only
- For paginated queries, use `_repository.ListAllAsync(request, cancellationToken)`
- Throw `NotFoundException` if entity is not found (for single-item queries)
- Use `.AsNoTracking()` semantics when applicable

### 3. Output DTO
**Path:** `src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/Queries/{NOUN}/{NOUN}Output.cs`

- Must be a `record` type (never a class)
- Include only the properties the caller needs — no domain entity exposure

### 4. Mapper
**Path:** `src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/Queries/{NOUN}/{NOUN}Mapper.cs`

- Static class with extension method: `public static {NOUN}Output ToOutput(this {NOUN} entity)`

### 5. Unit Tests
**Path:** `tests/UnitTests/{NOUN}/{QUERY_NAME}QueryHandlerTests.cs`

- Test naming: `Handle_{Scenario}_{ExpectedResult}`
- At minimum: happy path returning correct output, not-found case
- No mocking frameworks — use inline fakes/stubs at bottom of file
- Use `NullLogger<T>.Instance` for loggers

## Output format

For each file:
```
### File: `{full/path/to/file.cs}`
{complete file content with correct namespaces}
```
