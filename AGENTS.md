# AGENTS.md — Product.Template

## Stack
.NET 10, EF Core 10, MediatR 14, FluentValidation 12, Serilog, OpenTelemetry, JWT+RBAC, xUnit+Bogus, SQL Server, Scalar/OpenAPI, Docker, GitHub Actions.

## Commands (verification gates)
```bash
# Build
dotnet build

# Tests
dotnet test
dotnet test tests/UnitTests
dotnet test tests/IntegrationTests
dotnet test tests/ArchitectureTests
dotnet test tests/E2ETests

# Format check
dotnet format --verify-no-changes

# Run API
cd src/Api && dotnet run

# Infrastructure
docker compose up
```

## Verification Gate (MANDATORY before declaring task done)
Shortcut: `make verify` (runs steps 1–4 in order).

Run in order — all must pass:
1. `dotnet build`
2. `dotnet test tests/ArchitectureTests`
3. `dotnet test tests/UnitTests`
4. `dotnet format --verify-no-changes`
5. (If API running) `make health` — must return HTTP 200

Hooks in `.claude/settings.json` enforce #1 on Stop and #2 on `git commit`.

## Workflow

| Scope | Mode |
|-------|------|
| Single file, ≤20 lines, no layer crossing | Direct edit |
| 2+ files OR touches 2+ layers OR changes public contract (DTO/route/event) | Plan Mode → save plan to `.cursor/plans/{yyyy-mm-dd}-{slug}.md` → execute |
| Unclear scope or business intent | Ask |

Plan format: scope, affected files, inputs, expected output, acceptance command, rollback. Template: `.cursor/plans/_template.md`.

Loop detection: same failure 3× → stop, escalate. Do not introduce workarounds.

## Agent Boundaries

### Protected files — never modify without explicit user confirmation
- `.env*`, `compose.env*`
- `Product.Template.sln`, `Directory.Build.props`, `global.json`
- Applied migrations under `src/**/Migrations/*.cs` (never run `dotnet ef migrations remove` on applied)
- `docs/security/RBAC_MATRIX.md` (only edit as part of a new endpoint task)

### Forbidden auto-actions
- `git commit` without showing diff first
- `git push --force*`
- `rm -rf`, `dotnet ef database drop`
- Bypass flags: `--no-verify`, `--force`, `--skip-tests`
- Deleting RBAC matrix entries without corresponding endpoint removal

### Credential found in code
Stop immediately. Do not commit. Alert user. Suggest: rotate credential + `git filter-repo` to remove from history.

### No workarounds
If a fix requires bypassing verification (lint skip, test skip, `--no-verify`): stop and escalate. Correct the root cause.

### git diff
Run `git diff` and review before each commit in long agent runs.

## Memory (anti-context-rot)
Update `MEMORY.md` at end of each session: status per module, decisions taken, active blockers, next step.
If conversation > 50 turns or context feels saturated: finalize task, update `MEMORY.md`, request new chat.

## Parallel work

Use `git worktree` for 2+ independent tasks. One agent per worktree. Never two agents editing the same file simultaneously. Main thread coordinates merge.

### Subagent Coordination

Spawn a subagent (worktree) when: 2+ independent modules touched OR task has a clear isolated boundary.

**Steps:**
1. `git worktree add ../<branch-name> -b <branch-name>`
2. Update `MEMORY.md`: add branch name, scope, expected output
3. Subagent works in isolation — no shared files with main thread during execution
4. Main thread reviews diff before merge: `git diff main...<branch-name>`
5. Merge + remove worktree: `git worktree remove ../<branch-name>`

**When to delegate vs supervise inline:**
- Delegate: independent feature slice, separate module, isolated refactor
- Supervise inline: shared domain types, cross-cutting changes, migrations

**Communication:** subagent updates its own MEMORY.md section; main thread reads it before merging.

**Subagent blocker protocol:**
If a subagent hits a blocker (ambiguous requirement, protected file conflict, 3× loop failure):
1. Stop work in the worktree
2. Commit current state with message `wip: blocked — <reason>`
3. Update MEMORY.md under the branch entry: `BLOCKED: <reason>`
4. Return control to main thread — do not attempt workarounds

**Background Agents (long-running tasks):**
- Use for: full module scaffolding, mass refactors, test generation across multiple files
- Do NOT use for: migrations, any change to protected files, changes requiring user confirmation
- After background agent completes: main thread must run full verification gate before merge
  ```bash
  make verify
  git diff main
  # If API running:
  make health
  ```

## Rules and skills

### Layer rules (glob-attached)
- `domain.mdc` — `src/Core/**/*.Domain/**`
- `application.mdc` — `src/Core/**/*.Application/**`
- `infrastructure.mdc` — `src/Core/**/*.Infrastructure/**`
- `api.mdc` — `src/Api/**`
- `tests.mdc` — `tests/**`
- `cicd.mdc` — `.github/**`
- `docker.mdc` — `Dockerfile*, docker-compose*`
- `ai-features.mdc` — `src/**/Ai/**`

### Always-active rules
- `style.mdc` (alwaysApply: true)
- `global.mdc`, `architecture.mdc`, `naming.mdc` (alwaysApply: true)
- `global-security.mdc` (alwaysApply: true) — credential handling, pre-deploy checklist, tenant isolation
- `global-commits.mdc` (alwaysApply: true) — Conventional Commits format

### Skills
`.cursor/skills/{new-feature,new-command,new-query,new-endpoint,new-entity,new-module,new-migration,optimize-query,review,test-writer}/SKILL.md`

### Canonical reference module
`src/Core/Identity/` — look here first for any pattern.

## Commits (Conventional Commits)
Format: `<type>(<scope>): <subject>`
Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `ci`, `perf`
Scopes: `identity`, `authorization`, `tenants`, `api`, `infra`, `tests`, `docs`, `ci`, `docker`, `ai`
