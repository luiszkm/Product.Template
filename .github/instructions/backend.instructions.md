# Backend Instructions — Product.Template

> Regras de backend derivadas do padrão real deste repositório.
> Referência canônica: módulo Identity (`src/Core/Identity/`).

## Organização por módulo

Cada bounded context é um módulo em `src/Core/{Module}/` com 3 projetos:

```
{Module}.Domain/
├── Entities/              → User.cs, Role.cs, UserRole.cs
├── ValueObjects/          → Email.cs, Password.cs
├── Events/                → UserRegisteredEvent.cs
├── Repositories/          → IUserRepository.cs
└── {Module}.Domain.csproj → ref: Kernel.Domain

{Module}.Application/
├── Handlers/
│   ├── {Feature}/
│   │   ├── Commands/      → RegisterUserCommand.cs
│   │   └── RegisterUserCommandHandler.cs
├── Queries/
│   └── {Feature}/
│       ├── Commands/      → GetUserByIdQuery.cs, ListUserQuery.cs
│       ├── GetUserByIdQueryHandler.cs
│       └── UserOutput.cs
├── Validators/            → RegisterUserCommandValidator.cs
├── Mappers/               → UserMapper.cs
├── Security/              → RbacMetrics.cs
└── {Module}.Application.csproj → ref: {Module}.Domain + Kernel.Application

{Module}.Infrastructure/
├── Data/
│   ├── Persistence/       → UserRepository.cs
│   ├── Seeders/           → RoleSeeder.cs, UserSeeder.cs
│   └── DatabaseConfiguration.cs
├── DependencyInjection.cs
└── {Module}.Infrastructure.csproj → ref: {Module}.Application + Kernel.Infrastructure
```

## Commands

Padrão real extraído de `RegisterUserCommand.cs`:

```csharp
// src/Core/{Module}/{Module}.Application/Handlers/{Feature}/Commands/{Verbo}{Substantivo}Command.cs
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.{Module}.Application.Handlers.{Feature}.Commands;

public record {Verbo}{Substantivo}Command(
    // propriedades do command
) : ICommand<{Substantivo}Output>;  // ou ICommand se não retorna
```

Regras:
- Sempre `record`.
- Implementa `ICommand<TOutput>` quando retorna algo, `ICommand` quando void.
- Nome: `{Verbo}{Substantivo}Command` — ex: `RegisterUserCommand`, `DeleteRoleCommand`, `AddUserRoleCommand`.
- Vive dentro de `Handlers/{Feature}/Commands/`.

## Command Handlers

Padrão real extraído de `RegisterUserCommandHandler.cs`:

```csharp
// src/Core/{Module}/{Module}.Application/Handlers/{Feature}/{Verbo}{Substantivo}CommandHandler.cs
namespace Product.Template.Core.{Module}.Application.Handlers.{Feature};

public class {Verbo}{Substantivo}CommandHandler : ICommandHandler<{Verbo}{Substantivo}Command, {Output}>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<{Handler}> _logger;

    // Constructor injection

    public async Task<{Output}> Handle({Command} request, CancellationToken cancellationToken)
    {
        // 1. Buscar entidade (se necessário)
        // 2. Validar regras de negócio → throw BusinessRuleException / NotFoundException
        // 3. Criar/modificar entidade via factory ou behavior method
        // 4. Persistir via repository
        // 5. await _unitOfWork.Commit(cancellationToken);
        // 6. Logar com Serilog structured
        // 7. Retornar Output DTO (entity.ToOutput())
    }
}
```

Regras:
- Injeta repository + `IUnitOfWork` + `ILogger<T>`.
- Sempre chama `_unitOfWork.Commit(cancellationToken)` após mutação.
- Retorna DTO `record`, nunca entidade.
- Usa `entity.ToOutput()` (mapper extension method).
- Throw `NotFoundException` para entidades inexistentes.
- Throw `BusinessRuleException` para violações de regra.

## Queries

Padrão real extraído de `ListUserQuery.cs` e `GetUserByIdQuery.cs`:

```csharp
// Query simples
public record GetUserByIdQuery(Guid UserId) : IQuery<UserOutput>;

// Query paginada (herda ListInput)
public record ListUserQuery() : ListInput, IQuery<PaginatedListOutput<UserOutput>>;
```

- Queries vivem em `Queries/{Feature}/Commands/`.
- Query handlers vivem em `Queries/{Feature}/`.
- Query handlers NUNCA chamam `IUnitOfWork.Commit()`.
- Queries paginadas herdam de `ListInput` (PageNumber, PageSize, SearchTerm, SortBy, SortDirection).

## Validators

Padrão real extraído de `RegisterUserCommandValidator.cs`:

```csharp
// src/Core/{Module}/{Module}.Application/Validators/{CommandName}Validator.cs
namespace Product.Template.Core.{Module}.Application.Validators;

public class {CommandName}Validator : AbstractValidator<{CommandName}>
{
    public {CommandName}Validator()
    {
        RuleFor(x => x.Prop)
            .NotEmpty().WithMessage("mensagem")
            .MaximumLength(N).WithMessage("mensagem");
    }
}
```

Regras:
- Um validator por command — obrigatório.
- Validam shape: required, min/max length, format.
- NÃO validam regras de negócio (unicidade, existência) — isso é no handler.
- Executados automaticamente pelo `ValidationBehavior` no pipeline MediatR.

## Output DTOs

```csharp
// src/Core/{Module}/{Module}.Application/Queries/{Feature}/{Substantivo}Output.cs
public record UserOutput(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool EmailConfirmed,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    IEnumerable<string> Roles
);
```

- Sempre `record`.
- Sufixo `Output`.
- Nunca expõem entidades de domínio.

## Mappers

```csharp
// src/Core/{Module}/{Module}.Application/Mappers/{Substantivo}Mapper.cs
public static class UserMapper
{
    public static UserOutput ToOutput(this User user) => new(
        Id: user.Id,
        Email: user.Email,
        // ...
    );
}
```

## DI Pattern

```csharp
// src/Core/{Module}/{Module}.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection Add{Module}InJections(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddTransient<I{Entity}Repository, {Entity}Repository>();
        return services;
    }
}
```

Wiring em `Api/Configurations/CoreConfiguration.cs`:
```csharp
services.Add{Module}InJections();
```

## MediatR Pipeline Behaviors

Ordem no pipeline (Kernel.Application):
1. `ValidationBehavior` — executa FluentValidation validators.
2. `LoggingBehavior` — log de entrada/saída.
3. `PerformanceBehavior` — log de requests lentas.

