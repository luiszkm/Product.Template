# Guia — Como registrar permissões canônicas e consumir o catálogo

Status: 2026-03-16  
Base: `.github/copilot-instructions.md`, `ADR-004_catalogo_permissoes_rbac.md`, `docs/security/RBAC_MATRIX.md`

## 1. Objetivo
Assegurar que todo novo módulo publique suas permissões canônicas no `IPermissionCatalog` **antes** de expor endpoints protegidos, garantindo alinhamento entre policies, RBAC Matrix e seeders de banco.

## 2. Pré-requisitos
- Módulo estruturado em `src/Core/{Module}/` (Domain, Application, Infrastructure).
- Controller correspondente em `src/Api/Controllers/v1/`.
- `docs/security/RBAC_MATRIX.md` atualizado para o módulo.

## 3. Passo a passo
### 3.1 Defina o contrato de permissões do módulo
1. Crie `src/Core/{Module}/{Module}.Application/Permissions/{Module}Permissions.cs`.
2. Declare `const string` no formato `{module}.{resource}.{action}` e exponha `IReadOnlyCollection<PermissionDescriptor> All`.

```csharp
public static class BillingPermissions
{
    public const string InvoiceRead = "billing.invoice.read";
    public static IReadOnlyCollection<PermissionDescriptor> All { get; } =
    [
        new PermissionDescriptor(InvoiceRead, "billing", "invoice", "read", "Leitura de faturas")
    ];
}
```

### 3.2 Implemente o seeder do catálogo
1. Crie `.../Permissions/BillingPermissionCatalogSeeder.cs` implementando `IPermissionCatalogSeeder`.
2. No `Register`, chame `catalog.Register(BillingPermissions.All);`.

### 3.3 Registre o seeder na infraestrutura do módulo
1. Abra `src/Core/{Module}/{Module}.Infrastructure/DependencyInjection.cs`.
2. Adicione `services.AddSingleton<IPermissionCatalogSeeder, BillingPermissionCatalogSeeder>();` dentro de um método dedicado (ex.: `AddPermissionCatalogSeeders`).
3. Garanta que `Add{Module}InJections` invoque esse método. O `Kernel.Infrastructure.DependencyInjection` já coleta todos os `IPermissionCatalogSeeder` registrados e executa na inicialização do container.

### 3.4 Atualize seeds de banco (opcional, porém recomendado)
- Se o módulo possuir tabela de permissões própria, ajuste o seeder (ex.: `PermissionSeeder`) para usar os códigos canônicos definidos no passo 3.1.

### 3.5 Declare policies e atualize RBAC Matrix
1. Para policies novas, defina `public const string` em `SecurityConfiguration` e utilize os códigos canônicos. O `PermissionCatalogAuthorizationConfigurator` valida automaticamente se o código está registrado.
2. Documente cada rota/policy/permissão no `docs/security/RBAC_MATRIX.md` na mesma PR.

### 3.6 Consuma o catálogo em outros serviços
- **Controllers / Authorization**: utilize apenas as constantes do módulo (`BillingPermissions.InvoiceRead`).
- **Handlers / Validators**: injete `IPermissionCatalog` se precisar validar permissões dinâmicas (ex.: RBAC customizável via API).
- **Arquitetura/Testes**: adicione regras em `tests/ArchitectureTests/` impedindo literais fora do catálogo.

## 4. Checklist rápido antes do PR
- [ ] `ModulePermissions` criado com códigos `{module}.{resource}.{action}`.
- [ ] `ModulePermissionCatalogSeeder` registrando `All` no catálogo.
- [ ] `DependencyInjection.cs` do módulo registra o seeder.
- [ ] Seeds de banco usam os códigos canônicos (quando aplicável).
- [ ] Policies/App controllers usam apenas constantes do módulo.
- [ ] `docs/security/RBAC_MATRIX.md` atualizado.
- [ ] Guia/ADR referenciado no PR (link para este arquivo).

Seguindo estes passos, qualquer módulo novo passa a compartilhar o mesmo catálogo, reforçando auditoria, documentação e enforcement automático de políticas.

