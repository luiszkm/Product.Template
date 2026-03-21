---
description: Guide EF Core migration creation — verify configurations, run dotnet ef migrations add, validate output
tools: Read, Edit, Write, Bash, Glob, Grep
disable-model-invocation: true
allowed-tools: Read, Edit, Write, Bash, Glob, Grep
---

# Skill: /new-migration

> Guides you through adding an EF Core migration: verifies EF configuration exists, runs `dotnet ef migrations add`, and validates the generated migration.

## Arguments

`$ARGUMENTS` format: `{MIGRATION_NAME}`

Example: `/new-migration AddProductsTable`

Migration name should be descriptive PascalCase: `AddProductsTable`, `AddPriceColumnToProducts`, `CreateOrderIndexes`.

## Dynamic context

Current migrations (for naming conflicts and ordering awareness):
`!dotnet ef migrations list --project src/Tools/Migrator --startup-project src/Tools/Migrator --context AppDbContext`

## Context — read these files before proceeding

- `.ai/rules/04-infrastructure.md`
- `.ai/prompts/create-migration.md`

## Instruction

Parse `$ARGUMENTS` as `MIGRATION_NAME`.

### Step 1 — Verify EF entity configuration

For every new or modified entity involved in this migration:

1. Check that `IEntityTypeConfiguration<{Entity}>` exists in `src/Shared/Kernel.Infrastructure/Persistence/Configurations/{Entity}Configurations.cs`
   - Table name: plural PascalCase (e.g., `Products`)
   - All required columns configured with correct types and max lengths
   - `TenantId` included (required by `IMultiTenantEntity`)
   - Indexes on `TenantId` and any frequently-filtered columns

2. Check that `DbSet<{Entity}>` exists in `AppDbContext`

3. If either is missing, create or update the file before proceeding.

### Step 2 — Run migration

Execute:
```bash
cd src/Tools/Migrator && dotnet ef migrations add {MIGRATION_NAME} \
  --project ../Shared/Kernel.Infrastructure/Kernel.Infrastructure.csproj \
  --startup-project . \
  --context AppDbContext
```

For `TenantConfig` model changes, use `--context HostDbContext` instead.

### Step 3 — Review generated migration

Read the generated migration file and validate:
- [ ] Table names are correct (plural PascalCase)
- [ ] FK constraints reference the correct tables
- [ ] Composite indexes include `TenantId`
- [ ] No data loss operations (column drops, type changes) without explicit confirmation
- [ ] `Down()` method correctly reverses all changes

### Step 4 — Post-migration checklist

- [ ] Migration compiles (`dotnet build src/Tools/Migrator`)
- [ ] Seeder updated if new required data is needed
- [ ] `Down()` method is correct and complete

## HostDb migrations

If the migration affects `TenantConfig`:
```bash
cd src/Tools/Migrator && dotnet ef migrations add {MIGRATION_NAME} \
  --project ../Shared/Kernel.Infrastructure/Kernel.Infrastructure.csproj \
  --startup-project . \
  --context HostDbContext
```

## Output format

Report results for each step:
```
### Step 1 — EF Configuration
✅ {Entity}Configurations.cs exists and is correct
✅ DbSet<{Entity}> found in AppDbContext

### Step 2 — Migration command
(command output)

### Step 3 — Migration review
✅ Table names correct
✅ FK constraints correct
⚠️ (any warnings found)

### Step 4 — Checklist
- [ ] Compiles
- [ ] Down() is correct
```
