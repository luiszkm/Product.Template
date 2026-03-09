# Prompt: Criar Nova Feature

> Prompt reutilizável para criar uma feature completa seguindo o padrão deste template.

---

## Instruções para o agente

Antes de gerar qualquer código:

1. Leia `.github/copilot-instructions.md` para entender as regras globais.
2. Leia `.github/instructions/backend.instructions.md` para o padrão de organização.
3. Leia `.github/instructions/api.instructions.md` para o padrão de endpoints.
4. Leia `.github/instructions/infrastructure.instructions.md` para o padrão de persistência.
5. Inspecione o módulo Identity (`src/Core/Identity/`) como referência canônica.

## Input esperado

Forneça ao agente:
- **Módulo**: nome do bounded context (ex: `Catalog`, `Billing`)
- **Entidade principal**: nome do aggregate root (ex: `Product`, `Invoice`)
- **Campos**: lista de propriedades com tipo
- **Operações**: quais CRUDs ou operações são necessários
- **Policy**: qual policy de segurança se aplica (usar constantes de `SecurityConfiguration`)

## Template de uso

```
Crie uma feature completa para o módulo {MODULE} com a entidade {ENTITY}.

Campos da entidade:
- {campo}: {tipo} (required/optional)
- ...

Operações necessárias:
- Create / Read / Update / Delete / List
- (ou operações específicas)

Policy de segurança: SecurityConfiguration.{PolicyName}

Siga EXATAMENTE o padrão do módulo Identity como referência.
```

## O que o agente DEVE gerar

### Domain (`src/Core/{Module}/{Module}.Domain/`)
- [ ] `Entities/{Entity}.cs` — herda `AggregateRoot`, implementa `IMultiTenantEntity`, construtor privado, factory `Create(...)`, behavior methods
- [ ] `Repositories/I{Entity}Repository.cs` — herda `IBaseRepository<{Entity}>`
- [ ] Value Objects (se necessário)
- [ ] Domain Events (se necessário)

### Application (`src/Core/{Module}/{Module}.Application/`)
- [ ] `Handlers/{Feature}/Commands/Create{Entity}Command.cs` — `record : ICommand<{Entity}Output>`
- [ ] `Handlers/{Feature}/Create{Entity}CommandHandler.cs` — injeta repo + UoW + logger
- [ ] `Handlers/{Feature}/Commands/Update{Entity}Command.cs`
- [ ] `Handlers/{Feature}/Update{Entity}CommandHandler.cs`
- [ ] `Handlers/{Feature}/Commands/Delete{Entity}Command.cs` — `: ICommand`
- [ ] `Handlers/{Feature}/Delete{Entity}CommandHandler.cs`
- [ ] `Queries/{Feature}/Commands/Get{Entity}ByIdQuery.cs`
- [ ] `Queries/{Feature}/Get{Entity}ByIdQueryHandler.cs`
- [ ] `Queries/{Feature}/Commands/List{Entity}Query.cs` — herda `ListInput`
- [ ] `Queries/{Feature}/List{Entity}QueryHandler.cs`
- [ ] `Queries/{Feature}/{Entity}Output.cs` — `record`
- [ ] `Validators/Create{Entity}CommandValidator.cs`
- [ ] `Validators/Update{Entity}CommandValidator.cs`
- [ ] `Mappers/{Entity}Mapper.cs` — extension method `ToOutput()`

### Infrastructure (`src/Core/{Module}/{Module}.Infrastructure/`)
- [ ] `Data/Persistence/{Entity}Repository.cs`
- [ ] `DependencyInjection.cs`

### Kernel.Infrastructure
- [ ] `Persistence/Configurations/{Entity}Configurations.cs`
- [ ] Adicionar `DbSet<{Entity}>` em `AppDbContext`

### Api
- [ ] `Controllers/v1/{Module}Controller.cs`

### Registros
- [ ] Wire em `CoreConfiguration.cs`
- [ ] Atualizar `docs/security/RBAC_MATRIX.md`

### Testes
- [ ] `tests/UnitTests/{Feature}/Create{Entity}CommandHandlerTests.cs`
- [ ] `tests/UnitTests/{Feature}/Create{Entity}CommandValidatorTests.cs`

## Formato de resposta esperado

Para cada arquivo, forneça:
```
### `{caminho/completo/do/arquivo.cs}`
{conteúdo completo do arquivo}
```

Ao final:
```
## Resumo de alterações
- Arquivos criados: (lista)
- Arquivos modificados: (lista + o que mudou)
- Testes criados: (lista)
- RBAC Matrix: (linha adicionada)
```

