# Product Template - .NET Clean Architecture

Template universal para criação rápida de aplicações .NET com Clean Architecture, DDD, CQRS e padrões de alta qualidade.

## Características

### Arquitetura e Padrões
- **Clean Architecture** com separação clara de responsabilidades
- **Domain-Driven Design (DDD)** com SeedWorks (Entity, AggregateRoot, Domain Events)
- **CQRS** com CommandBus e QueryBus
- **Behaviors** automáticos: Logging, Performance, Validation
- **Estrutura de Testes** completa: Unit, Integration, E2E

### Resiliência e Segurança
- **Políticas de Retry** com backoff exponencial (Polly)
- **Circuit Breaker** para proteção contra falhas em cascata
- **Rate Limiting** configurável por endpoint e IP
- **IP Whitelist/Blacklist** com suporte a CIDR
- **Request/Response Logging** com correlação ID e mascaramento de dados sensíveis
- **Health Checks** com UI (database, memory, disk space)
- **CORS** configurável por ambiente
- **JWT Authentication** com políticas de autorização

### Infraestrutura
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

## Funcionalidades Avançadas

### 1. Resiliência e Polly (Retry, Circuit Breaker)

O template inclui políticas de resiliência usando **Polly** para chamadas HTTP a serviços externos.

#### Configuração (appsettings.json)
```json
{
  "ResiliencePolicies": {
    "RetryCount": 3,
    "CircuitBreakerThreshold": 5,
    "CircuitBreakerDurationSeconds": 30,
    "TimeoutSeconds": 30
  }
}
```

#### Usando HttpClient com Polly
```csharp
public class ExternalService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ExternalService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetDataAsync()
    {
        var client = _httpClientFactory.CreateClient("ResilientHttpClient");
        var response = await client.GetAsync("https://api.example.com/data");
        return await response.Content.ReadAsStringAsync();
    }
}
```

### 2. Rate Limiting (Limite de Requisições)

Protege sua API contra abuso e ataques DDoS.

#### Configuração (appsettings.json)
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      },
      {
        "Endpoint": "POST:/api/users",
        "Period": "1h",
        "Limit": 10
      }
    ]
  }
}
```

**Resultado**: Requisições acima do limite retornarão `429 Too Many Requests`.

### 3. IP Whitelist/Blacklist

Controla acesso baseado em endereços IP.

#### Configuração (appsettings.json)
```json
{
  "IpSecurity": {
    "EnableWhitelist": true,
    "EnableBlacklist": false,
    "AllowedIPs": [
      "127.0.0.1",
      "192.168.1.0/24",
      "10.0.0.1"
    ],
    "BlockedIPs": [
      "203.0.113.0"
    ]
  }
}
```

**Suporte a CIDR**: Use notação CIDR (`192.168.1.0/24`) para permitir ranges de IPs.

### 4. Health Checks

Monitore a saúde da sua aplicação.

#### Endpoints disponíveis

- `/health` - Status geral (database, memory, disk)
- `/health/ready` - Readiness probe (apenas checks críticos)
- `/health/live` - Liveness probe (verifica se a aplicação está rodando)
- `/healthchecks-ui` - Interface visual dos health checks

#### Exemplo de resposta
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0100000",
      "data": {
        "ResponseTime": "10ms",
        "Database": "ProductDb"
      }
    },
    "memory": {
      "status": "Healthy",
      "data": {
        "allocated": "150MB"
      }
    }
  }
}
```

### 5. Request/Response Logging

Todos os requests e responses são logados automaticamente com:

- **Correlation ID** para rastreamento
- **Mascaramento de dados sensíveis** (passwords, tokens)
- **Métricas de performance** (tempo de resposta)
- **Informações de IP e User-Agent**

**Header de Correlation ID**: `X-Correlation-ID` é retornado em todas as respostas.

### 6. CORS (Cross-Origin Resource Sharing)

Configuração flexível de CORS por ambiente.

#### Desenvolvimento (appsettings.Development.json)
```json
{
  "Cors": {
    "AllowedOrigins": [ "*" ],
    "AllowedMethods": [ "*" ],
    "AllowedHeaders": [ "*" ]
  }
}
```

#### Produção (appsettings.json)
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://myapp.com",
      "https://admin.myapp.com"
    ],
    "AllowedMethods": [ "GET", "POST", "PUT", "DELETE" ],
    "AllowedHeaders": [ "Content-Type", "Authorization" ]
  }
}
```

### 7. JWT Authentication (Opcional)

Autenticação JWT está disponível, mas **desabilitada por padrão**.

#### Habilitar JWT (appsettings.json)
```json
{
  "Jwt": {
    "Enabled": true,
    "Secret": "your-super-secret-key-with-at-least-32-characters",
    "Issuer": "YourApp.Api",
    "Audience": "YourApp.Api",
    "ExpirationMinutes": 60
  }
}
```

#### Protegendo endpoints
```csharp
[Authorize] // Requer autenticação
[HttpGet]
public async Task<IActionResult> GetSecureData()
{
    return Ok("Secure data");
}

[Authorize(Policy = "AdminOnly")] // Requer role admin
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(Guid id)
{
    return NoContent();
}
```

#### Gerando um token JWT (exemplo)
```csharp
var tokenHandler = new JwtSecurityTokenHandler();
var key = Encoding.ASCII.GetBytes(configuration["Jwt:Secret"]);
var tokenDescriptor = new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.Name, "user@example.com"),
        new Claim("role", "admin")
    }),
    Expires = DateTime.UtcNow.AddMinutes(60),
    Issuer = configuration["Jwt:Issuer"],
    Audience = configuration["Jwt:Audience"],
    SigningCredentials = new SigningCredentials(
        new SymmetricSecurityKey(key),
        SecurityAlgorithms.HmacSha256Signature)
};
var token = tokenHandler.CreateToken(tokenDescriptor);
var tokenString = tokenHandler.WriteToken(token);
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

### Core
- **.NET 8.0**
- **ASP.NET Core**
- **Entity Framework Core**
- **FluentValidation**
- **Scrutor** (Assembly Scanning)

### Resiliência e Segurança
- **Polly** (Resilience and transient-fault-handling)
- **AspNetCoreRateLimit** (Rate limiting middleware)
- **Microsoft.AspNetCore.Authentication.JwtBearer** (JWT Authentication)

### Observabilidade
- **AspNetCore.HealthChecks** (Health check endpoints e UI)
- **Microsoft.Extensions.Diagnostics.HealthChecks**

### Documentação
- **Swashbuckle.AspNetCore** (Swagger/OpenAPI)

### Testes
- **xUnit** (Test framework)
- **Bogus** (Fake data generation)

## Contribuindo

Contribuições são bem-vindas! Sinta-se livre para abrir issues e pull requests.

## Licença

Este template é fornecido "como está", sem garantias. Use por sua conta e risco.

## Autor

Neuraptor

## Mais Informações

- [Documentação da Arquitetura](ARCHITECTURE.md)
- [Guia de Contribuição](CONTRIBUTING.md)
