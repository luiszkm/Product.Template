# Plan: {slug}
Date: {yyyy-mm-dd}
Status: draft | in-progress | done | blocked

## Scope

<!-- What bounded context / module / layer does this touch? -->

## Trigger

<!-- What user request or task prompted this plan? -->

## Affected files

<!-- List every file expected to change. If unknown, list the directories. -->

```
src/Core/{Module}/{Module}.Domain/...
src/Core/{Module}/{Module}.Application/...
src/Core/{Module}/{Module}.Infrastructure/...
src/Api/Controllers/...
tests/UnitTests/Core/{Module}/...
tests/IntegrationTests/Core/{Module}/...
```

## Input

<!-- What does the agent receive? (user story, command, entity name, etc.) -->

## Expected output

<!-- What exists after the plan executes? List new/modified files and their purpose. -->

## Acceptance command

```bash
dotnet build
dotnet test tests/ArchitectureTests
dotnet test tests/UnitTests
dotnet test tests/IntegrationTests --filter "FullyQualifiedName~{Feature}"
dotnet format --verify-no-changes
# If API running:
curl -f http://localhost:5000/health
```

## Rollback

<!-- How to undo if something goes wrong mid-execution? -->
<!-- e.g. "revert commits", "dotnet ef migrations remove", "delete branch" -->

## Steps

- [ ] 1. {first step}
- [ ] 2. {second step}
- [ ] 3. Run acceptance command

## Blockers / notes

(none)
