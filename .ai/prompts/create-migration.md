# Prompt: Create Migration

> Use this prompt to generate an EF Core migration after model changes.

---

## Context Files
- `.ai/rules/04-infrastructure.md`

## Instruction

Generate a migration for the following model change: **`{DESCRIPTION}`**.

### Steps

1. **Verify EF Configuration exists** for every new/modified entity in `Kernel.Infrastructure/Persistence/Configurations/`.
2. **Verify DbSet exists** in `AppDbContext` for new entities.
3. **Generate the migration command**:
   ```bash
   cd src/Tools/Migrator
   dotnet ef migrations add {MigrationName} \
     --project ../Shared/Kernel.Infrastructure/Kernel.Infrastructure.csproj \
     --startup-project . \
     --context AppDbContext
   ```
4. **Review the generated migration** — ensure:
   - Table names are correct (plural PascalCase).
   - FK constraints are correct.
   - Indexes are created for `TenantId` composite keys.
   - No data loss operations without explicit confirmation.

### Naming Convention

Migration name: `{Date}_{Description}` or just `{Description}`.
Examples: `AddCatalogTable`, `AddProductPriceColumn`, `CreateOrderIndexes`.

### HostDb Migrations

If changing the `TenantConfig` model:
```bash
dotnet ef migrations add {MigrationName} \
  --project ../Shared/Kernel.Infrastructure/Kernel.Infrastructure.csproj \
  --startup-project . \
  --context HostDbContext
```

### Post-Migration Checklist
- [ ] Migration compiles.
- [ ] `dotnet ef database update` runs without errors.
- [ ] Seeder updated if new required data is needed.
- [ ] Rollback migration exists (`Down` method is correct).

