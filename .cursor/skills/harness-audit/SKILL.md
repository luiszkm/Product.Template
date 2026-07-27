---
name: harness-audit
version: 1
description: "Scans the entire agent harness (rules, skills, checklists, hooks, agent docs) and verifies it forms a complete, non-contradictory pipeline for BOTH quality code generation and code review. Detects orphaned skills, phantom skill references, broken file paths, duplicate/overlapping triggers, and pipeline stages with no owning skill or rule. TRIGGER: \"audit harness\", \"audit rules and skills\", \"harness health check\", \"check skill coverage\", \"harness gaps\", \"review agent setup\". SKIP: reviewing application code (use /review or /pr-review), writing a new skill (use create-skill), writing a new rule (use create-rule)."
tools: Read, Glob, Grep, Bash
context: fork
---

# Skill: /harness-audit

> Read-only audit of the AI agent harness itself (not the application code). Answers one question: **"If an agent generates code and then reviews it using only what's in this harness, is there a gap?"** Produces a coverage matrix for two pipelines — Code Generation and Code Review — plus a findings list of inconsistencies between harness documents.

This skill never edits `.cursor/rules/`, `.cursor/skills/`, or `.agents/`. It only reports. Fixes go through `create-rule`, `create-skill`, or a manual edit confirmed by the user (per `agent-behavior.mdc` — rules/skills are not "protected files" but changing them changes agent behavior repo-wide, so propose, don't silently apply).

## Arguments

`$ARGUMENTS` (optional): `generation` | `review` | `full` (default `full`)

- `generation` — audit only the code-generation pipeline (entity → command/query → endpoint → migration → tests)
- `review` — audit only the code-review pipeline (local review, PR review, security review, gates, hooks)
- `full` — both, plus cross-document consistency checks

## Context — read before scanning

- `.cursor/rules/agent-behavior.mdc` and `.cursor/rules/global.mdc` — the two files that declare the canonical skill trigger table; treated as the "spec" the filesystem must match
- `AGENTS.md` / `CLAUDE.md` — root agent instructions, may duplicate or diverge from the rules above
- `harness-audit/reference.md` (this skill's own reference file) — full stage taxonomy and detection rule catalog

## Dynamic context

Run these to build the inventory (adjust paths if the repo uses a different root):

```bash
find .cursor/rules -name "*.mdc" | sort
find .cursor/skills -mindepth 2 -maxdepth 2 -name "SKILL.md" | sort
find .agents/checklists -name "*.md" | sort
find .agents/patterns -type f | sort
git log --oneline -15 -- .cursor/rules .cursor/skills .agents AGENTS.md CLAUDE.md
```

On Windows/PowerShell hosts, use `Glob` tool calls instead of `find`.

## Step 1 — Build the harness inventory

Using `Glob`/`Read`, catalog every harness component into this table (keep it — it is the raw input for Steps 2–4):

| Type | Path | Name (frontmatter or heading) | Referenced by |
|---|---|---|---|
| Rule | `.cursor/rules/{x}.mdc` | ... | (filled in Step 3) |
| Skill | `.cursor/skills/{x}/SKILL.md` | ... | ... |
| Checklist | `.agents/checklists/{x}.md` | ... | ... |
| Pattern doc | `.agents/patterns/{x}` | ... | ... |
| Hook | `.claude/settings.json` entries | ... | ... |
| Agent doc | `AGENTS.md`, `CLAUDE.md`, `MEMORY.md` | ... | ... |

For every skill, extract from frontmatter: `name`, `description` (with embedded TRIGGER/SKIP if present), `tools`. For every rule, extract `alwaysApply` vs glob-attached scope.

## Step 2 — Validate the two pipelines

Load `reference.md` for the full stage list and detection rules. For each pipeline, produce a coverage matrix with one row per stage:

**Pipeline A — Code Generation (quality-focused)**

| Stage | Owning skill(s) | Rules it loads | Checklist backing | Status |
|---|---|---|---|---|
| Entity/aggregate creation | | | | |
| Command | | | | |
| Query | | | | |
| Endpoint (API + RBAC) | | | | |
| Migration | | | | |
| New module/bounded context | | | | |
| AI feature (ITool/agent loop) | | | | |
| Naming conventions | | | | |
| File placement | | | | |
| CI/CD pipeline | | | | |
| Docker/containerization | | | | |
| Observability (OTel/health) | | | | |
| Query optimization | | | | |
| Test scaffolding | | | | |
| Pre-merge verification gate | | | | |

**Pipeline B — Code Review**

| Stage | Owning mechanism | Trigger | Status |
|---|---|---|---|
| Local/ad-hoc file review | | | |
| Full PR multi-agent review | | | |
| Security-specific review | | | |
| Bugbot-style review | | | |
| SonarQube review | | | |
| Architecture gate (pre-commit) | | | |
| Build gate (pre-stop) | | | |
| Format gate | | | |
| RBAC matrix consistency | | | |
| CI check loop / babysit | | | |

Status values: ✅ Covered, no gaps · ⚠️ Covered but with an issue (duplicate, broken ref, stale) · ❌ No owning skill/rule found.

## Step 3 — Cross-reference and detect issues

Apply every check in `reference.md` § Detection Rules. At minimum, always run these four (they catch the highest-impact issues with the least effort):

1. **Phantom trigger** — a skill trigger listed in `AGENTS.md` or `global.mdc`'s skill table with no matching `.cursor/skills/{name}/SKILL.md` on disk.
2. **Orphaned skill** — a `SKILL.md` exists on disk but is absent from both `AGENTS.md` and `global.mdc` skill tables.
3. **Broken reference** — any backtick-quoted file path inside a rule or skill (e.g. `` `.cursor/rules/foo.mdc` ``, `` `.agents/checklists/bar.md` ``) that does not resolve to a real file.
4. **Overlapping triggers** — two or more skills whose description TRIGGER phrases would both match the same user phrasing (e.g. two skills both claiming "review this" / "code review").

## Step 4 — Report

```
## Harness Audit — {generation|review|full}

### 📦 Inventory
| Type | Count | Files |
|---|---|---|
| Rules | N | ... |
| Skills | N | ... |
| Checklists | N | ... |

### 🏗️ Pipeline A — Code Generation Coverage
{matrix from Step 2, one row per stage, all ✅/⚠️/❌ resolved}

### 🔎 Pipeline B — Code Review Coverage
{matrix from Step 2}

### 🔴 Critical Gaps (❌ rows — no owning skill/rule)
- {stage}: {what's missing, concrete recommendation — e.g. "no skill covers X; create with /create-skill or extend Y"}

### 🟡 Inconsistencies (⚠️ rows — duplicate, broken ref, stale, orphaned)
- {file}: {finding} — **Fix:** {specific action, e.g. "remove `/review` trigger overlap by narrowing its description to non-PR scope" or "update broken path to existing checklist/pattern file"}

### 🔵 Suggestions
- {non-blocking improvement, e.g. "checklist X has no matching skill enforcing it programmatically"}

### 📊 Score
Generation: {covered}/{total} stages ✅ · Review: {covered}/{total} stages ✅
{N} critical gaps · {N} inconsistencies

### ✅ What's solid
- {2-3 things done well, to avoid over-indexing on negatives}
```

Never silently "fix" a rule or skill file while auditing. If the user asks you to fix a specific finding after seeing the report, use `create-rule` or `create-skill` for new files, or edit the existing file directly for small corrections (broken path, duplicate trigger) and confirm the diff before writing.
