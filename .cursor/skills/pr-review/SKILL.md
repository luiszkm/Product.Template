---
name: pr-review
description: Multi-agent PR reviewer for Product.Template. Use ONLY when explicitly asked to review a pull request: "review PR #N", "review this PR", "code review", "check this pull request". Do NOT trigger automatically during coding, feature implementation, or general questions.
license: CC-BY-4.0
metadata:
  author: Product.Template Team
  version: 1.0.0
---

# PR Review — Orchestration Protocol

Coordinates 6 specialized subagents (via the Task tool) then consolidates findings into a unified summary. Each subagent loads the relevant project rules — this skill does not duplicate them.

## Step 1: Initialize

1. Get PR number from context or ask the user.
2. Identify repo: `gh repo view --json nameWithOwner -q .nameWithOwner`
3. Fetch diff: `gh pr diff {PR_NUMBER}`
4. Load existing inline comments: `gh api repos/{REPO}/pulls/{PR_NUMBER}/comments` — build a set of `{path, line}` pairs to avoid reposting.
5. Read PR intent: `gh pr view {PR_NUMBER} --json title,body,headRefName`
6. Extract Linear issue ID from branch name or PR body (pattern `[A-Z]+-[0-9]+`, e.g., `PT-42`). Will be used by Subagent 2.

## Step 2: Launch Subagents in Parallel

Send **one message** with **six Task tool calls** — all launched simultaneously. Pass REPO, PR_NUMBER, the diff, existing comment locations, and PR intent to each subagent prompt. After all complete, run Step 3.

---

## Severity Labels (all subagents use these)

- 🚨 Critical — bugs or logic errors that will cause failures
- 🔒 Security — security vulnerabilities or data exposure
- ⚡ Performance — significant performance concerns
- ⚠️ Warning — code smells or maintainability issues
- 💡 Suggestion — optional improvements

---

## Universal Rules (every subagent must follow)

1. **Comment allowlist:** Only post inline comments on lines in the diff starting with `+` (excluding `+++`).
2. **Skip duplicates:** If `{path, line}` within ±3 lines already has a comment, skip.
3. **Mark resolved:** Reply `[RESOLVED] This appears resolved by the recent changes.` on existing comments where the issue is fixed.
4. **False positive guard:** Only report findings with ≥80% confidence. Skip when uncertain.
5. **Positive highlight:** Include at least one well-done aspect of the change before listing issues.
6. **Tone:** Specific, actionable, collegial. Explain WHY something is a problem.
7. **Never** approve, request-changes, or modify files. Use `--comment` only.
8. **Marker:** Start every inline comment body with `<!-- pt-review:{type} -->` (invisible in rendered view, used by the consolidation subagent).

---

## Subagent 1: Security

**Marker:** `<!-- pt-review:security -->`

Load `.cursor/rules/global-security.mdc` and `.cursor/rules/security.mdc` and `docs/security/RBAC_MATRIX.md`. Review the PR diff for any violations:

- Hardcoded secrets, connection strings, JWT secrets, or API keys in any config file
- `[Authorize]` without explicit `Policy` — must be `[Authorize(Policy = SecurityConfiguration.{PolicyName})]`
- New protected endpoint not present in `docs/security/RBAC_MATRIX.md`
- `[AllowAnonymous]` added without explicit justification
- Sensitive fields (`Password`, `Token`, `Secret`, `Key`, `CreditCard`) returned in Output DTOs
- PII or credentials in Serilog log templates (string interpolation or unmasked destructuring)
- `IgnoreQueryFilters()` used without documented justification (bypasses tenant isolation)
- External HTTP calls made from Domain or Application layers (must be behind an infrastructure interface)

**Second pass:** Re-read the full diff from top to bottom. List every file or hunk you did not comment on. For each uncovered file, ask: "Does this file violate any security rule in my scope?" Only skip a file when you can explicitly state why it is clean.

**Comment format:**
```
<!-- pt-review:security -->
🔒 Security — [Short title]
[What the issue is and why it matters]
**Recommendation:** [Specific fix]
```

---

## Subagent 2: Requirements & Definition of Done

**Marker:** `<!-- pt-review:requirements -->`
**Posts:** One PR-level summary comment only — no inline comments.

Use a two-track approach to find requirements. Run both tracks in parallel; use whichever yields content.

### Track A — Linear Issue

Linear is the source of truth for specs and acceptance criteria in this project.

1. Extract a Linear issue ID from the PR branch name (pattern `[A-Z]+-[0-9]+`, e.g., `feat/PT-42-catalog-module` → `PT-42`).
2. If found, fetch the issue via the Linear MCP tool (or Linear REST API if MCP unavailable):
   - Get: `title`, `description`, `acceptanceCriteria` (custom field if present), `labels`, `state`
   - Also fetch sub-issues/children if any: look for linked issues with type `sub-issue`
3. Extract from the issue body: acceptance criteria (lines after `## Acceptance Criteria` or checkboxes `- [ ]`), design goals, non-goals, and any explicit DoD items.
4. If no Linear ID in branch name, check PR title and body for a mention like `PT-42`, `Closes PT-42`, or a Linear URL.

### Track B — PR Checklist (`.agents/checklists/pull-request.md`)

Always load this file. It defines the universal DoD for every PR in this project. Use it as the baseline requirements checklist regardless of Track A.

### Resolution Logic

| Tracks with content | Action |
|---|---|
| Both A and B | Merge requirements; note the source of each item |
| B only | Use checklist requirements |
| A only | Use Linear requirements |
| Neither | Post: "⚠️ No Linear issue found — using standard PR checklist." and proceed with Track B |

Compare the merged requirements against the PR diff and post a summary with `gh pr comment {PR_NUMBER} --body '...'`

**Second pass:** After drafting the summary, re-read the full requirements list one item at a time and ask: "Did I evaluate this criterion against the diff?" For any item not yet assessed, find the relevant section of the diff and explicitly mark it ✅, ❌, or 🔲.

**Summary format:**
```markdown
<!-- pt-review:requirements -->
## 📋 Requirements Review

**Sources:** {e.g. "Linear: PT-42 + PR Checklist" | "PR Checklist only"}

### ✅ Implemented
### ❌ Missing or Incomplete
### 🔲 Definition of Done
- [x] covered  - [ ] not covered
### 💬 Notes
```

---

## Subagent 3: Test Coverage

**Marker:** `<!-- pt-review:tests -->`

Load `.cursor/rules/tests.mdc` and section 6 of `.agents/checklists/new-feature.md`. Use those rules as the reference for what correct tests look like. Review the PR diff for:

**Missing tests (🚨 Critical):**
- New command handler without at least one happy-path **and** one failure-path unit test
- New validator without unit tests covering required fields and edge cases
- New protected endpoint without authorization integration tests (401 + 403 + 200)

**Test quality issues (⚠️ Warning):**
- Mocking framework used (`Moq`, `NSubstitute`, etc.) — must use inline fakes/stubs (sealed classes)
- Integration test missing `X-Tenant: public` header
- Integration test not using `TestAuthHandler` scheme
- Roles/permissions injected by means other than `X-Test-Roles` / `X-Test-Permissions` / `X-Test-UserId` headers
- Test name not following `{SystemUnderTest}_{Scenario}_{ExpectedResult}` convention
- Fake/stub placed inline in test method instead of at bottom of test class or shared inner class
- `NullLogger<T>.Instance` not used for logger dependencies (custom mock used instead)

**Architecture test gaps (⚠️ Warning):**
- New module added without corresponding architecture tests validating layer dependencies and naming conventions
- `RBAC_MATRIX.md` consistency test not updated after new endpoint added

**Second pass:** Re-read the full diff from top to bottom. List every new or modified handler, validator, and controller action you did not comment on. For each uncovered item, ask: "Is there a corresponding test covering the happy path and at least one failure case?" Only skip when you can explicitly state why coverage already exists or is not applicable.

**Comment format:**
```
<!-- pt-review:tests -->
[🚨/⚠️/💡] — [Short title]
[Description of the gap or anti-pattern]
**Recommendation:** [Pattern to follow per tests.mdc]
```

---

## Subagent 4: Architecture & Patterns

**Marker:** `<!-- pt-review:architecture -->`

### Phase 0 — Load all reference documents

Load every document listed below before touching the diff:

1. `.cursor/rules/architecture.mdc`
2. `.cursor/rules/domain.mdc`
3. `.cursor/rules/application.mdc`
4. `.cursor/rules/infrastructure.mdc`
5. `.cursor/rules/api.mdc`
6. `.cursor/rules/csharp-patterns.mdc`
7. `.agents/checklists/new-feature.md`
8. `.agents/checklists/pull-request.md`
9. Invoke `/naming-conventions` to check naming deviations

Then scan the diff for directory structure: note which layers (`Domain`, `Application`, `Infrastructure`, `Api`) are touched.

### Phase 1 — Extract the rule list from the loaded documents

Do not use a hardcoded list. After loading all documents in Phase 0, scan each one and extract every explicit rule (lines marked `✅`, `❌`, `- [ ]`, or explicit forbidden/required statements) into a single numbered checklist. Number the combined list sequentially from 1. This is your evaluation matrix for Phase 2.

### Phase 2 — Evaluate the matrix

Work through the diff **one file at a time**. For each changed file:

- For each rule in the Phase 1 list, decide: **PASS** / **VIOLATION** / **N/A**
- N/A is only valid when the rule is structurally inapplicable to the file type
- For every VIOLATION: post an inline comment on the exact `+` line in the diff

Key areas to focus on:
- **Layer violations**: Domain referencing Application/Infrastructure; Application referencing Infrastructure
- **Handler anti-patterns**: Handler calling another handler; query handler calling `IUnitOfWork.Commit()`; command handler missing `Commit()` after mutation
- **Domain patterns**: Entity missing private constructor + `Create()` factory; mutable public setters; invariants enforced outside the entity
- **Application patterns**: Handler returning domain entity instead of Output DTO; command missing `AbstractValidator`; `CancellationToken` not forwarded
- **API patterns**: Controller action with business logic; `[Authorize]` without explicit Policy; missing `[ProducesResponseType]`; action body > ~20 lines
- **Naming**: Any deviation from `{Verb}{Noun}Command`, `{Get|List}{Noun}Query`, `{Noun}Output`, `{Noun}Mapper`, `I{AggregateRoot}Repository`
- **New module wiring**: `DependencyInjection.cs` missing; not wired in `CoreConfiguration.cs`; assembly not in MediatR scan

**Second pass:** For all files, re-read the full diff from top to bottom. List every file or hunk not yet evaluated. Only skip a file when you can explicitly state which rules are N/A and why.

**Comment format:**
```
<!-- pt-review:architecture -->
[🚨/⚠️/💡] — [Short title]
Rule: [Rule number + which doc, e.g. "Rule 8 — architecture.mdc Forbidden"]
[What in the diff violates it — quote the offending line]
**Recommendation:** [Exact fix, code snippet if < 8 lines]
```

---

## Subagent 5: Regression & Hallucination Detection

**Marker:** `<!-- pt-review:regression -->`

Review the PR diff for code changes unrelated to the PR's stated purpose, or showing signs of AI-generated artifacts:

- **🚨 Critical**: Deleted code unrelated to the PR's stated purpose
- **🚨 Critical**: Phantom using directives referencing non-existent namespaces
- **🚨 Critical**: Method calls with wrong signatures or incorrect argument count/types
- **🚨 Critical**: `IUnitOfWork.Commit()` removed from a command handler
- **⚠️ Warning**: `TODO` comment left in production code without a tracking issue
- **⚠️ Warning**: Commented-out code left in
- **⚠️ Warning**: Weakened error handling (`catch {}`, `catch { return null }`, exception swallowed silently)
- **⚠️ Warning**: Duplicate logic that already exists in another handler or service in the same module
- **⚠️ Warning**: Weakened test assertions (removed `Assert`, changed `Equal` to `NotNull`, etc.)
- **⚠️ Warning**: Type casts or null forgiving operator (`!`) hiding compiler errors
- **💡 Suggestion**: Dead code (private methods never called, unreachable branches)
- **💡 Suggestion**: Unused `using` directives introduced in the diff

**Second pass:** Re-read the full diff from top to bottom. List every file or hunk you did not comment on. For each uncovered file, ask: "Does this file contain any unrelated deletions, phantom namespaces, duplicate logic, or weakened assertions?" Only skip when none of those categories apply.

**Comment format:**
```
<!-- pt-review:regression -->
[🚨/⚠️/💡] — [Short title]
Type: [unrelated-deletion | phantom-namespace | wrong-signature | duplicate | regression | dead-code | weakened-assertion]
[Specific description with quoted evidence from the diff]
**Recommendation:** [Exact fix]
```

---

## Subagent 6: Performance

**Marker:** `<!-- pt-review:performance -->`

Load `.cursor/rules/infrastructure.mdc` (Repository and EF Core sections). Only flag issues **clearly visible in the diff** — do not speculate about code not shown.

Look for:
- **N+1 queries**: Repository or DbContext lookup inside a `foreach`/`for` loop without batch loading
- **Missing `Include`/`ThenInclude`**: Navigation property accessed after fetch without eager loading configured — causes lazy-load N+1 or `NullReferenceException`
- **In-memory pagination**: `.ToList()` or `.ToListAsync()` called before `.Skip()`/`.Take()` — loads full table into memory
- **Unbounded queries**: `GetAllAsync()` / `FindAsync()` with no pagination, filter, or `Take()` limit on potentially large tables
- **Sequential awaits for independent operations**: Multiple `await repository.GetByIdAsync(...)` calls that could run concurrently with `Task.WhenAll`
- **Redundant `Commit()` calls**: Multiple `await _unitOfWork.Commit()` in a single command handler — should be one call after all mutations
- **Synchronous I/O**: `.Result`, `.Wait()`, or blocking calls on async methods

**Second pass:** Re-read the full diff from top to bottom. List every repository call, LINQ expression, and loop body you did not comment on. For each uncovered block, ask: "Does this contain a clearly visible performance issue?" Only skip when you can explicitly state why none of the patterns above apply.

**Comment format:**
```
<!-- pt-review:performance -->
⚡ Performance — [Short title]
[Description with estimated impact, e.g. "O(N) queries per request on large tenant tables"]
**Recommendation:** [Fix with short code sketch if < 8 lines]
```

---

## Step 3: Consolidation

After all 6 subagents complete, spawn one more subagent via Task tool to consolidate:

1. `gh api repos/{REPO}/pulls/{PR_NUMBER}/comments` — fetch all inline comments.
2. Filter to those starting with `<!-- pt-review: -->` and parse the type from the marker.
3. Fetch PR-level comments for the `<!-- pt-review:requirements -->` summary.
4. Group by severity: 🔒 Security → 🚨 Critical → ⚡ Performance → ⚠️ Warning → 💡 Suggestion.
5. Deduplicate findings at the same `{path, line}` (±3 lines) — note both agents in the entry.
6. Collect one positive highlight per agent.
7. **Gap detection:** Run `gh pr diff {PR_NUMBER} --name-only` to get the full list of changed files. Cross-reference against all collected inline comment paths. For any file with zero inline comments from any subagent, add it to a `### 🔍 Files With No Inline Comments` section. Omit a file from this section only if it is a config/lock file (`*.json`, `*.yaml`, `*.csproj`, `*.sln`, `*.lock`) or a pure DTO/record with no logic.
8. Post: `gh pr review {PR_NUMBER} --comment --body '...'`

**Summary format:**
```markdown
## 🤖 Product.Template AI Review Summary

| | |
|---|---|
| **Subagents invoked** | {N} of 6 (Security · Requirements · Test Coverage · Architecture · Regression · Performance) |
| **Rules loaded** | `.cursor/rules/architecture.mdc`, `.cursor/rules/global-security.mdc`, `.cursor/rules/tests.mdc`, `.cursor/rules/infrastructure.mdc`, + 4 others |
| **Docs loaded** | `docs/security/RBAC_MATRIX.md`, `.agents/checklists/pull-request.md`, `.agents/checklists/new-feature.md` |
| **Findings** | {N} across {M} files |

---

### 🔒 Security ({N})
- [`path/File.cs:L42`] Finding title

### 🚨 Critical ({N})
### ⚡ Performance ({N})
### ⚠️ Warnings ({N})
### 💡 Suggestions ({N})

---
### 🔍 Files With No Inline Comments
- `path/to/File.cs` — no findings from any subagent (verify manually or re-run targeted review)

_(Omit this section if all logic files received at least one comment.)_

---
### ✅ Highlights
- [One positive highlight per agent]

---
> See inline comments for details and recommendations.
> For targeted single-file review: `/review src/Core/{Module}/`
```

If no findings across all agents: post `✅ No issues found across all review dimensions.` but still include the metadata table.
