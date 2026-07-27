# Harness Audit — Reference

Full stage taxonomy and detection rule catalog for `/harness-audit`. Read this file when running the audit; it is intentionally kept out of `SKILL.md` to keep the main flow short.

## Pipeline A — Code Generation Stage → Expected Owner Map

This is the expected mapping in a healthy `Product.Template`-derived harness. Use it as the baseline; flag any deviation.

| Stage | Expected skill | Expected rules loaded | Expected checklist |
|---|---|---|---|
| Entity/aggregate creation | `new-entity` | `domain.mdc`, `architecture.mdc` | `new-feature.md` (entity section) |
| Command | `new-command` | `application.mdc`, `architecture.mdc` | `new-feature.md` |
| Query | `new-query` | `application.mdc` | `new-feature.md` |
| Full vertical slice | `new-feature` | `global.mdc`, `architecture.mdc`, `domain.mdc`, `application.mdc`, `infrastructure.mdc`, `api.mdc`, `tests.mdc` | `new-feature.md` |
| Endpoint (API + RBAC) | `new-endpoint` | `api.mdc`, `security.mdc`, `openapi-contracts.mdc` | `api-endpoint.md` |
| Migration | `new-migration` | `infrastructure.mdc` | — (schema changes are high-risk; verify a checklist exists, flag if not) |
| New module/bounded context | `new-module` | `architecture.mdc`, `folder-structure.mdc` | — |
| AI feature (ITool/agent loop) | `new-ai-feature` | `ai-features.mdc` | — |
| Naming conventions | `naming-conventions` | `style.mdc` | — |
| File placement | `repo-layout` | `folder-structure.mdc` | — |
| CI/CD pipeline | `setup-cicd` | `cicd.mdc` | — |
| Docker/containerization | `docker-setup` | `docker.mdc` | — |
| Observability (OTel/health) | `setup-observability` | `observability.mdc` | — |
| Query optimization | `optimize-query` | `infrastructure.mdc` | — |
| Test scaffolding | `test-writer` | `tests.mdc`, `application.mdc` | `new-feature.md` §6 |
| Pre-merge verification gate | `make verify` (Makefile) + `.claude/settings.json` hooks | — | `pull-request.md` |

If a stage has no skill and no rule, it's a ❌ critical gap — code for that stage will be generated ad hoc with no enforced convention.

## Pipeline B — Code Review Stage → Expected Owner Map

| Stage | Expected mechanism | Notes |
|---|---|---|
| Local/ad-hoc file review | `/review` skill | Single-file or directory scope, 6-area scan |
| Full PR multi-agent review | `/pr-review` skill | 6 parallel subagents via Task tool, posts inline GH comments |
| Security-specific review | `security-review` subagent (`review-security` skill) | User-invoked only |
| Bugbot-style review | `bugbot` subagent (`review-bugbot` skill) | User-invoked only |
| SonarQube review | `sonarqube-reviewer` subagent / `sonar-*` skills | Requires SonarQube MCP |
| Architecture gate (pre-commit) | `.claude/settings.json` `PreToolUse` hook on `git commit*` | Runs `dotnet test tests/ArchitectureTests` |
| Build gate (pre-stop) | `.claude/settings.json` `Stop` hook | Runs `dotnet build` |
| Format gate | `make verify` step 4 | `dotnet format --verify-no-changes` |
| RBAC matrix consistency | `tests/E2ETests/Security/RbacMatrixConsistencyTests.cs` + `pull-request.md` checklist item | Verify the test file actually exists if claimed |
| CI check loop | `loop-on-ci` / `fix-ci` / `ci-watcher` (external skill/subagent) | Not part of this repo's `.cursor/skills`; note as external dependency, not a gap |

**Known duplicate-risk pair:** `/review` and `/pr-review` both describe triggers like "review this", "code review" in their frontmatter `description`. This is intentional divergence in scope (file-level vs PR-level) but is a **real trigger ambiguity** for the agent unless the descriptions clearly disambiguate scope (single file/dir vs a PR number). Always check both descriptions verbatim and confirm they state their scope boundary explicitly — if not, flag as ⚠️.

## Detection Rules — full catalog

### 1. Phantom trigger
A skill name/trigger appears in `AGENTS.md`'s "Skills (invoke on-demand)" table or `global.mdc`'s "Skills" table, but `.cursor/skills/{name}/SKILL.md` does not exist.
**Fix:** create the skill with `create-skill`, or remove the table row if it's aspirational/deprecated.

### 2. Orphaned skill
`.cursor/skills/{name}/SKILL.md` exists but `{name}` (or its `/{name}` invocation form) is absent from both `AGENTS.md` and `global.mdc` skill tables.
**Fix:** add a row to both tables (they should stay in sync — see Detection Rule 6).

### 3. Broken reference
Any backtick-quoted path resembling a file (`.mdc`, `.md`, `.cs`, `.json`, ending in a real extension) cited inside a rule or skill body does not exist on disk. Common offenders: renamed rules, removed checklists, or stale pattern paths.
**Fix:** either create the missing file or update the reference to the correct/current path.

### 4. Overlapping triggers
Two or more skill `description` fields contain near-identical TRIGGER phrases (fuzzy match on quoted trigger strings) without a clear scope disambiguator (file vs PR, single vs bulk, read vs write).
**Fix:** narrow one or both descriptions to state explicit scope, or merge the skills.

### 5. Rule drift inside a skill
A skill's "Context — invariants (rules)" section lists rule files; check each still exists AND check it isn't missing a rule that obviously governs the layer the skill touches (e.g., a skill that creates API endpoints not loading `security.mdc` or `api.mdc`).
**Fix:** add the missing rule reference to the skill's context section.

### 6. Cross-document inconsistency
`AGENTS.md` and `.cursor/rules/global.mdc` both maintain a "Skills" trigger table. Diff them: same skill name should have consistent (or at least non-contradictory) trigger phrases in both. Same check applies to the "Glob-attached rules" table in `AGENTS.md` vs actual `.cursor/rules/*.mdc` frontmatter `alwaysApply`/glob scope.
**Fix:** treat one file as canonical (recommend `AGENTS.md` since it's the root agent entrypoint) and sync the other.

### 7. Checklist coverage gap
A skill in the "Completeness" contract of `agent-behavior.mdc` (e.g., "for every command, also create: handler, validator, output DTO, mapper, test") has no corresponding checklist item enforcing it in `.agents/checklists/`.
**Fix:** add the item to the relevant checklist, or note explicitly that enforcement is code-based (e.g., an `ArchitectureTests` rule) rather than checklist-based — that's also acceptable, just must be traceable.

### 8. Verification gate wiring gap
A skill that generates or modifies code does not mention running `dotnet build` / `make verify` / the relevant `dotnet test` subset in its own Steps/Instruction section — meaning an agent following only that skill might skip verification.
**Fix:** add a verification step to the skill (see `test-writer`'s Step 6 as the reference pattern).

### 9. Hook claim mismatch
`AGENTS.md` states: "Hooks in `.claude/settings.json` enforce #1 on Stop and #2 on `git commit`." Read the actual `.claude/settings.json` and confirm the `Stop` hook really runs step 1 (`dotnet build`) and the commit-gated hook really runs step 2 (`dotnet test tests/ArchitectureTests`). Flag any drift between the prose claim and the JSON.

### 10. Stale skill metadata
Skill frontmatter missing a `description`, or a `description` with no TRIGGER/SKIP-style guidance at all (inconsistent with the rest of the harness's skills), making it unlikely the agent will invoke it appropriately.
**Fix:** rewrite the description following the pattern used by `test-writer`/`review`/`optimize-query`.

## Severity guide for the report

- 🔴 **Critical gap** — a whole pipeline stage (generation or review) has no owning skill/rule at all. Code will be produced or merged with zero enforced convention for that concern.
- 🟡 **Inconsistency** — something exists but is broken, duplicated, contradictory, or drifted (rules 1, 3, 4, 5, 6, 9).
- 🔵 **Suggestion** — coverage exists but could be strengthened (rules 7, 8, 10) — non-blocking.
