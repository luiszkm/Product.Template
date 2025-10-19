# Product Template - .NET Clean Architecture

Template universal para criação rápida de aplicações .NET com Clean Architecture, DDD, CQRS e padrões de alta qualidade.

## Características

- **Clean Architecture** com separação clara de responsabilidades
- **Domain-Driven Design (DDD)** com SeedWorks (Entity, AggregateRoot, Domain Events)
- **CQRS** com CommandBus e QueryBus
- **Behaviors** automáticos: Logging, Performance, Validation
- **Estrutura de Testes** completa: Unit, Integration, E2E
- **API RESTful** com Swagger/OpenAPI
- **Entity Framework Core** com suporte a múltiplos bancos
- **Docker** pronto para uso

## Estrutura do Projeto

```
Product.Template/
├── src/
│   ├── Api/
│   │   └── Api/                          # API REST
│   │       ├── Configurations/           # Configurações de DI
│   │       ├── Controllers/              # Controllers
│   │       ├── GlobalFilter/             # Filtros globais
│   │       └── ApiModels/                # DTOs de API
│   ├── Shared/
│   │   ├── Kernel.Domain/                # Camada de Domínio
│   │   │   ├── SeedWorks/                # Base classes (Entity, AggregateRoot)
│   │   │   └── Exceptions/               # Domain exceptions
│   │   ├── Kernel.Application/           # Camada de Aplicação
│   │   │   ├── Behaviors/                # CQRS Behaviors
│   │   │   ├── Messaging/                # CommandBus, QueryBus
│   │   │   ├── Data/                     # Interfaces de persistência
│   │   │   └── Events/                   # Event Publisher
│   │   └── Kernel.Infrastructure/        # Camada de Infraestrutura
│   │       └── Persistence/              # DbContext, Repositories
│   └── Modules/                          # (Para módulos futuros)
└── tests/
    ├── UnitTests/                        # Testes unitários
    ├── IntegrationTests/                 # Testes de integração
    ├── E2ETests/                         # Testes end-to-end
    └── CommonTests/                      # Fixtures compartilhados
```

## Instalação do Template

### 1. Instalar o template localmente

```bash
# Na pasta raiz do template
dotnet new install .
```

### 2. Verificar instalação

```bash
dotnet new list | findstr product-template
```

## Criando um Novo Projeto

### Uso básico

```bash
dotnet new product-template -n MeuProjeto
```

Isso criará um projeto com o namespace `MeuProjeto.*`

### Navegando para o projeto

```bash
cd MeuProjeto
```

### Restaurar dependências

```bash
dotnet restore
```

### Compilar o projeto

```bash
dotnet build
```

### Executar a API

```bash
dotnet run --project src/Api/Api/Api.csproj
```

A API estará disponível em: `https://localhost:5001` (ou conforme configurado)

## Executando Testes

### Todos os testes

```bash
dotnet test
```

### Testes unitários apenas

```bash
dotnet test tests/UnitTests/UnitTests.csproj
```

### Testes de integração

```bash
dotnet test tests/IntegrationTests/IntegrationTests.csproj
```

### Testes E2E

```bash
dotnet test tests/E2ETests/E2ETests.csproj
```

## Como Usar

### 1. Criar uma Entidade de Domínio

```csharp
// src/Shared/Kernel.Domain/Entities/Product.cs
using MeuProjeto.Kernel.Domain.SeedWorks;

namespace MeuProjeto.Kernel.Domain.Entities;

public class Product : Entity<Guid>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    public Product(Guid id, string name, decimal price) : base(id)
    {
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

### 2. Criar um Command (CQRS)

```csharp
// src/Shared/Kernel.Application/UseCases/CreateProduct/CreateProductCommand.cs
using MeuProjeto.Kernel.Application.Messaging.Interfaces;

namespace MeuProjeto.Kernel.Application.UseCases.CreateProduct;

public record CreateProductCommand(string Name, decimal Price) : ICommand<Guid>;
```

### 3. Criar um CommandHandler

```csharp
// src/Shared/Kernel.Application/UseCases/CreateProduct/CreateProductCommandHandler.cs
using MeuProjeto.Kernel.Application.Messaging.Interfaces;
using MeuProjeto.Kernel.Application.Data;
using MeuProjeto.Kernel.Domain.Entities;

namespace MeuProjeto.Kernel.Application.UseCases.CreateProduct;

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

        // Adicionar ao repositório
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
```

### 4. Criar um Controller

```csharp
// src/Api/Api/Controllers/ProductsController.cs
using Microsoft.AspNetCore.Mvc;
using MeuProjeto.Kernel.Application.Messaging.Interfaces;
using MeuProjeto.Kernel.Application.UseCases.CreateProduct;

namespace MeuProjeto.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ICommandBus _commandBus;

    public ProductsController(ICommandBus commandBus)
    {
        _commandBus = commandBus;
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
        // Implementar Query
        return Ok();
    }
}
```

## Padrões e Convenções

### Camadas

- **Kernel.Domain**: Entidades, Value Objects, Domain Events, Exceptions de domínio
- **Kernel.Application**: Use Cases, Commands, Queries, Handlers, Behaviors, Interfaces
- **Kernel.Infrastructure**: Implementações de persistência, DbContext, Repositories
- **Api**: Controllers, DTOs, Configurações, Filters

### CQRS

- **Commands**: Operações que modificam estado (Create, Update, Delete)
- **Queries**: Operações de leitura (Get, List, Search)
- **Handlers**: Implementam a lógica de negócio
- **Behaviors**: Interceptam Commands/Queries (Logging, Validation, Performance)

### Dependency Injection

Os handlers são registrados automaticamente usando **Scrutor**. Basta criar a classe implementando `ICommandHandler<,>` ou `IQueryHandler<,>`.

## Configuração de Banco de Dados

Edite `src/Api/Api/Configurations/ConnectionsConfigurations.cs`:

```csharp
public static IServiceCollection AddAppConnections(
    this IServiceCollection services,
    IConfiguration config)
{
    // Exemplo: SQL Server
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

    return services;
}
```

## Docker

### Build da imagem

```bash
docker build -t meuprojeto-api .
```

### Executar container

```bash
docker run -p 8080:80 meuprojeto-api
```

## Desinstalar Template

```bash
dotnet new uninstall Neuraptor.Product.Template
```

## Tecnologias Utilizadas

- **.NET 8.0**
- **ASP.NET Core**
- **Entity Framework Core**
- **FluentValidation**
- **Scrutor** (Assembly Scanning)
- **Swashbuckle** (Swagger/OpenAPI)
- **xUnit** (Testes)
- **Bogus** (Geração de dados fake)

## Contribuindo

Contribuições são bem-vindas! Sinta-se livre para abrir issues e pull requests.

## Licença

Este template é fornecido "como está", sem garantias. Use por sua conta e risco.

## Autor

Neuraptor

## Mais Informações

- [Documentação da Arquitetura](ARCHITECTURE.md)
- [Guia de Contribuição](CONTRIBUTING.md)
