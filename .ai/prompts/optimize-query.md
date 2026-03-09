# Prompt: Optimize Query

> Use this prompt to optimize a slow query or introduce Dapper for read-heavy operations.

---

## Context Files
- `.ai/rules/04-infrastructure.md`
- `.ai/rules/03-application.md`

## Instruction

Optimize the query **`{QUERY_NAME}`** in module **`{MODULE_NAME}`**.

### Analysis Steps

1. **Identify the current implementation** — read the handler and repository method.
2. **Check for N+1 problems** — look for missing `Include`/`ThenInclude` or lazy loading.
3. **Check for over-fetching** — are columns loaded that aren't needed by the output DTO?
4. **Check pagination** — is `Skip`/`Take` applied at the DB level or in memory?

### Optimization Options (in order of preference)

#### Option A: Optimize EF Core Query
- Add missing `Include` / `ThenInclude`.
- Use `.AsNoTracking()` for read-only queries.
- Use `.Select()` projection to load only needed columns.
- Add database indexes if query patterns warrant it.

#### Option B: Introduce Dapper Read Service
- Create an interface in Application: `I{Feature}ReadService`.
- Implement in Infrastructure with Dapper using raw SQL.
- Inject `IDbConnection` (from the same connection as EF).
- Keep the handler clean — it calls the read service, not Dapper directly.
- File structure:
  ```
  src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/Queries/{Feature}/I{Feature}ReadService.cs
  src/Core/{MODULE_NAME}/{MODULE_NAME}.Infrastructure/Data/ReadServices/{Feature}ReadService.cs
  ```

### Output Format

```
### Analysis
(what was found)

### Recommendation
(which option and why)

### Changes
(file-by-file implementation)
```

