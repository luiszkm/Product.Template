# Plan: decouple-harness-from-template
Date: 2026-07-27
Status: in-progress

## Scope

Reusable agent harness (`.cursor/rules/`, `.cursor/skills/`, `.claude/settings.json`) — remove hardcoded project facts (canonical reference module name, solution/project name, docker/db names, test project paths) so the harness works unmodified when copied to a new project. Project facts move to one doc: `.agents/patterns/project-facts.md`.

## Trigger

User: harness too coupled to this template via direct file references; make it project-independent.

## Affected files

```
.agents/patterns/project-facts.md          (new — single source of truth)
.agents/patterns/README.md                 (pointer instead of literal path)
Agents.md                                   (canonical reference module section -> pointer)
MEMORY.md                                   (canonical reference mentions -> pointer)
.claude/settings.json                      (PreToolUse hook: dynamic test-project discovery)
.cursor/rules/agent-behavior.mdc
.cursor/rules/global.mdc
.cursor/rules/global-commits.mdc
.cursor/rules/global-security.mdc
.cursor/skills/*/SKILL.md (new-query, new-command, new-entity, new-endpoint, new-module,
  new-feature, new-ai-feature, test-writer, review, repo-layout, docker-setup,
  setup-cicd, optimize-query, naming-conventions, pr-review, setup-observability)
```

Out of scope (flagged, not touched): `.agents/examples/README.md` (already correctly-scoped concrete example doc), `.cursor/plans/2026-05-24-*.md` (historical artifact), `.claude/settings.local.json` / `packages.json` (personal machine paths, separate concern).

## Input

Scan result: 15+ harness files hardcode `src/Core/Identity/` as "canonical reference", 8 files hardcode literal `Product.Template` name/docker/db names, hook + ~8 docs hardcode `tests/ArchitectureTests` etc.

## Expected output

- `.agents/patterns/project-facts.md` holds: reference module, project/solution name, docker image name, db name, test project paths + naming convention.
- All `.cursor/rules/*.mdc` and `.cursor/skills/*/SKILL.md` reference `.agents/patterns/project-facts.md` instead of hardcoding these facts; illustrative examples reframed as "e.g." rather than "canonical reference is X".
- `.claude/settings.json` PreToolUse hook discovers the architecture-test project directory dynamically (glob on `*architecture*` under `tests/`) instead of hardcoding `tests/ArchitectureTests`.
- `Agents.md` / `MEMORY.md` canonical-reference mentions point at `project-facts.md` (single source, no duplication).

## Acceptance command

```bash
dotnet build
dotnet test tests/ArchitectureTests
git commit -m "test" --dry-run  # sanity-check hook still triggers/discovers correctly
```

## Rollback

`git checkout -- .agents .cursor .claude/settings.json Agents.md MEMORY.md` (all changes are doc/config only, no src/ touched).

## Steps

- [x] 1. Write `.agents/patterns/project-facts.md`
- [x] 2. Update `.claude/settings.json` hook to dynamic discovery
- [ ] 3. Update `Agents.md`, `MEMORY.md`, `.agents/patterns/README.md` pointers
- [ ] 4. Sweep `.cursor/rules/*.mdc` and `.cursor/skills/*/SKILL.md`
- [ ] 5. Run acceptance command, review `git diff`

## Blockers / notes

(none)
