---
description: Analyze and optimize EF Core / Dapper queries — detect N+1, over-fetching, missing pagination, and propose AsNoTracking, AsSplitQuery, or Dapper read service
tools: Read, Edit, Write, Glob, Grep
---

# Skill: /optimize-query

> Analyzes a query handler or repository for performance issues and proposes concrete optimizations following the project's persistence patterns.

## Arguments

`$ARGUMENTS` format: path to a handler, repository, or module directory

Examples:
- `/optimize-query src/Core/Products/Products.Infrastructure/`
- `/optimize-query src/Core/Identity/Identity.Application/Queries/User/GetUserByIdQueryHandler.cs`

## Context — read these files before analyzing

- `.ai/rules/04-infrastructure.md`
- `.ai/rules/09-observability.md`
- `.ai/prompts/optimize-query.md`
- `.github/agents/query-optimizer.agent.md`

## Instruction

Parse `$ARGUMENTS` as the path to analyze.

You are a database performance specialist with deep EF Core expertise. You know this project's patterns:
- **Write**: EF Core 10.x via `AppDbContext`
- **Read**: EF Core (or Dapper for complex queries via read services)
- Multi-tenancy: global query filters auto-apply `TenantId` — never bypass them
- Pagination: `Skip`/`Take` at database level, never in memory

### Step 1 — Read the implementation

Read all relevant handler and repository files in `$ARGUMENTS`. Map the full data access path from handler → repository → DbContext.

### Step 2 — Diagnose issues

Check for each of the following:

| Issue | Detection |
|-------|-----------|
| N+1 queries | Missing `Include`/`ThenInclude` for navigations used in the output |
| Over-fetching | Loading columns not projected into the output DTO |
| Cartesian explosion | Multiple `Include` on collections without `AsSplitQuery()` |
| No tracking | Missing `.AsNoTracking()` on read-only queries |
| In-memory pagination | `ToList()` before `Skip`/`Take` |
| Double roundtrip | Separate `Count()` that could be batched |
| Missing index | Filter/order on columns without an index in EF configuration |

### Step 3 — Propose optimizations

#### Option A — Optimize EF Core query (prefer this first)

- Add `.AsNoTracking()` to read-only queries
- Use `.Select()` projection to load only needed columns
- Add `AsSplitQuery()` to avoid cartesian explosion
- Fix `Include`/`ThenInclude` chains
- Ensure `Skip`/`Take` is applied before `ToListAsync`

#### Option B — Dapper read service (when EF Core is insufficient)

If Option A cannot achieve the required performance:

**Application interface:**
```
src/Core/{Module}/{Module}.Application/Queries/{Feature}/I{Feature}ReadService.cs
```

**Infrastructure implementation:**
```
src/Core/{Module}/{Module}.Infrastructure/Data/ReadServices/{Feature}ReadService.cs
```

- Inject `IDbConnection` (same connection as EF)
- Write optimized raw SQL
- Register in `DependencyInjection.cs`
- Never bypass `TenantId` filter in raw SQL

### Step 4 — Index recommendations

For each query pattern detected, suggest indexes in the EF configuration:
```csharp
builder.HasIndex(e => new { e.TenantId, e.{FilteredField} });
```

## Output format

```
## Query Analysis: {scope}

### 📊 Diagnosis
| Issue | Location | Severity |
|-------|----------|----------|
| (detected issues) | ... | High/Medium/Low |

### 🎯 Estimated impact
- (roundtrips saved, data reduction, latency improvement)

### 🔧 Proposed solution

#### Option A — EF Core optimization
(file-by-file code changes)

#### Option B — Dapper read service (if needed)
(complete interface + implementation)

### ⚠️ Risks
- (breaking changes, test impacts, edge cases)

### 📐 Alternative
(trade-off analysis if both options are valid)
```
