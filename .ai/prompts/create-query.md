# Prompt: Create Query

> Scaffold a CQRS query with handler, output DTO, and test.

---

## Context Files
- `.ai/rules/03-application.md`
- `.ai/rules/11-naming.md`
- `.ai/rules/12-folder-structure.md`
- Reference: `src/Core/Identity/Identity.Application/Queries/User/`

## Instruction

Create a query **`{QUERY_NAME}`** in module **`{MODULE_NAME}`**.

### Files to Create

#### 1. Query Definition
- Path: `src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/Queries/{Feature}/Commands/{QUERY_NAME}.cs`
- Must be a `record` implementing `IQuery<{OUTPUT_TYPE}>`.
- For paginated lists, inherit from `ListInput` AND implement `IQuery<PaginatedListOutput<{OUTPUT_TYPE}>>`.

#### 2. Query Handler
- Path: `src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/Queries/{Feature}/{QUERY_NAME}Handler.cs`
- Implement `IQueryHandler<{QUERY_NAME}, {OUTPUT_TYPE}>`.
- Inject the repository interface.
- **Never call `IUnitOfWork.Commit()`** — queries are read-only.
- For paginated queries, use `_repository.ListAllAsync(request, cancellationToken)`.

#### 3. Output DTO (if not already existing)
- Path: `src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/Queries/{Feature}/{Noun}Output.cs`
- Must be a `record` type.

#### 4. Unit Test
- Path: `tests/UnitTests/{Feature}/{QUERY_NAME}HandlerTests.cs`
- Test at least: happy path, not-found case.

## Output Format
Provide complete files with correct namespaces.

