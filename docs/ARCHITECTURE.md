# Arquitetura do Projeto

Este documento descreve a arquitetura e os padrões de design utilizados no template.

## Visão Geral

O projeto segue os princípios de **Clean Architecture**, **Domain-Driven Design (DDD)** e **Command Query Responsibility Segregation (CQRS)**, proporcionando uma base sólida, testável e escalável para aplicações .NET.

## Camadas da Arquitetura

```
┌─────────────────────────────────────────────┐
│              API Layer                      │
│  (Controllers, DTOs, Configurations)        │
└─────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────┐
│         Application Layer                   │
│  (Use Cases, Commands, Queries, Handlers)   │
└─────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────┐
│            Domain Layer                     │
│  (Entities, Value Objects, Domain Events)   │
└─────────────────────────────────────────────┘
                    ▲
                    │
┌─────────────────────────────────────────────┐
│        Infrastructure Layer                 │
│  (Persistence, External Services)           │
└─────────────────────────────────────────────┘
```

### 1. Domain Layer (Kernel.Domain)

**Responsabilidade**: Contém a lógica de negócio pura e as regras de domínio.

**Componentes**:
- **Entities**: Objetos com identidade própria
- **Value Objects**: Objetos imutáveis sem identidade
- **Aggregates**: Clusters de entidades e value objects
- **Domain Events**: Eventos que representam mudanças no domínio
- **Domain Exceptions**: Exceções específicas do domínio

**Regras**:
- Não depende de nenhuma outra camada
- Livre de frameworks e bibliotecas externas
- Contém apenas lógica de negócio pura

**Exemplo**:
```csharp
public class Product : Entity<Guid>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    public Product(Guid id, string name, decimal price) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty");

        if (price <= 0)
            throw new DomainException("Product price must be positive");

        Name = name;
        Price = price;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new DomainException("Price must be positive");

        Price = newPrice;
    }
}
```

### 2. Application Layer (Kernel.Application)

**Responsabilidade**: Orquestra a lógica de aplicação e coordena o fluxo de dados.

**Componentes**:
- **Commands**: Operações que modificam estado
- **Queries**: Operações de leitura
- **Handlers**: Implementam a lógica dos Commands/Queries
- **Behaviors**: Interceptam Commands/Queries (cross-cutting concerns)
- **DTOs**: Objetos de transferência de dados
- **Interfaces**: Contratos para repositórios e serviços

**Padrão CQRS**:
```csharp
// Command
public record CreateProductCommand(string Name, decimal Price) : ICommand<Guid>;

// Command Handler
public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var product = new Product(Guid.NewGuid(), command.Name, command.Price);

        // Save to repository
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
```

**Behaviors**:
- **LoggingBehavior**: Registra logs de entrada e saída
- **PerformanceBehavior**: Monitora performance
- **ValidationBehavior**: Valida dados usando FluentValidation

### 3. Infrastructure Layer (Kernel.Infrastructure)

**Responsabilidade**: Implementa interfaces definidas na camada de aplicação.

**Componentes**:
- **DbContext**: Contexto do Entity Framework
- **Repositories**: Implementações de acesso a dados
- **UnitOfWork**: Coordena transações
- **External Services**: Integrações com APIs externas

**Exemplo**:
```csharp
public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

### 4. API Layer (Api)

**Responsabilidade**: Expõe endpoints HTTP e coordena as requisições.

**Componentes**:
- **Controllers**: Endpoints da API
- **DTOs**: Request/Response models
- **Filters**: Tratamento global de exceções
- **Configurations**: Injeção de dependência

**Exemplo**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;

    public ProductsController(ICommandBus commandBus, IQueryBus queryBus)
    {
        _commandBus = commandBus;
        _queryBus = queryBus;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
    {
        var productId = await _commandBus.Send<CreateProductCommand, Guid>(command);
        return CreatedAtAction(nameof(GetById), new { id = productId }, productId);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetProductByIdQuery(id);
        var product = await _queryBus.Send<GetProductByIdQuery, ProductDto>(query);
        return Ok(product);
    }
}
```

## Padrões de Design

### 1. CQRS (Command Query Responsibility Segregation)

Separa operações de leitura (Queries) das operações de escrita (Commands).

**Vantagens**:
- Separação clara de responsabilidades
- Otimização independente de leitura e escrita
- Melhor escalabilidade

### 2. Repository Pattern

Abstrai o acesso a dados, permitindo trocar a implementação de persistência facilmente.

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
}
```

### 3. Unit of Work

Coordena múltiplas operações de repositório em uma única transação.

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### 4. Dependency Injection

Todo o projeto utiliza injeção de dependência nativa do .NET.

Handlers são registrados automaticamente usando **Scrutor**:
```csharp
services.Scan(scan => scan
    .FromApplicationDependencies()
    .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
        .AsImplementedInterfaces()
        .WithScopedLifetime()
);
```

### 5. Domain Events

Permitem comunicação entre agregados sem acoplamento direto.

```csharp
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

public class ProductPriceChangedEvent : IDomainEvent
{
    public Guid ProductId { get; }
    public decimal OldPrice { get; }
    public decimal NewPrice { get; }
    public DateTime OccurredOn { get; }

    public ProductPriceChangedEvent(Guid productId, decimal oldPrice, decimal newPrice)
    {
        ProductId = productId;
        OldPrice = oldPrice;
        NewPrice = newPrice;
        OccurredOn = DateTime.UtcNow;
    }
}
```

## Fluxo de Requisição

```
1. HTTP Request
   │
   ▼
2. Controller
   │
   ▼
3. CommandBus / QueryBus
   │
   ▼
4. Behaviors (Logging, Validation, Performance)
   │
   ▼
5. Handler
   │
   ├─▶ Domain Layer (Business Logic)
   │
   └─▶ Infrastructure Layer (Persistence)
   │
   ▼
6. Response
```

## Testes

### Estrutura de Testes

- **UnitTests**: Testam lógica de domínio e handlers isoladamente
- **IntegrationTests**: Testam integração com banco de dados
- **E2ETests**: Testam fluxos completos da API
- **CommonTests**: Fixtures e helpers compartilhados

### Exemplo de Teste Unitário

```csharp
public class ProductTests
{
    [Fact]
    public void UpdatePrice_WithNegativeValue_ShouldThrowDomainException()
    {
        // Arrange
        var product = new Product(Guid.NewGuid(), "Test Product", 100);

        // Act & Assert
        Assert.Throws<DomainException>(() => product.UpdatePrice(-10));
    }
}
```

## Boas Práticas

### 1. Imutabilidade
- Use `record` para DTOs e Commands/Queries
- Properties privadas com setters privados em Entities

### 2. Validação
- Validação de domínio nas Entities
- Validação de entrada usando FluentValidation

### 3. Exceções
- `DomainException` para erros de domínio
- `NotFoundException` para recursos não encontrados
- Tratamento global via `ApiGlobalExceptionFilter`

### 4. Naming Conventions
- Commands: `{Action}{Entity}Command` (ex: `CreateProductCommand`)
- Queries: `Get{Entity}By{Criteria}Query` (ex: `GetProductByIdQuery`)
- Handlers: `{CommandOrQuery}Handler` (ex: `CreateProductCommandHandler`)

### 5. Organização de Código
- Um arquivo por classe
- Namespaces refletem a estrutura de pastas
- Use cases organizados por feature

## Evolução da Arquitetura

### Adicionar Módulos

Para adicionar um novo módulo de domínio:

1. Criar pasta em `src/Modules/{ModuleName}`
2. Adicionar camadas Domain, Application, Infrastructure
3. Registrar serviços em `Program.cs`

### Migração de Banco de Dados

```bash
# Adicionar migration
dotnet ef migrations add MigrationName --project src/Shared/Kernel.Infrastructure

# Aplicar migration
dotnet ef database update --project src/Shared/Kernel.Infrastructure
```

## Referências

- [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [Vertical Slice Architecture](https://jimmybogard.com/vertical-slice-architecture/)
