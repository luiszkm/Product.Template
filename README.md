# Product Template - .NET Clean Architecture

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Build Status](https://github.com/Neuraptor/Product.Template/workflows/Validate%20Template/badge.svg)](https://github.com/Neuraptor/Product.Template/actions)

Template universal para criação rápida de aplicações .NET com Clean Architecture, DDD, CQRS e padrões de alta qualidade.

## 🚀 Início Rápido

### Instalação do Template

#### Opção 1: Clonar do GitHub (Recomendado)

```bash
# Clonar o repositório
git clone https://github.com/Neuraptor/Product.Template.git
cd Product.Template

# Instalar o template
dotnet new install .
```

#### Opção 2: Instalar direto do GitHub (sem clonar)

```bash
# Download e instalação automática
dotnet new install https://github.com/Neuraptor/Product.Template/archive/refs/tags/v1.0.0.zip
```

#### Opção 3: Instalação local para desenvolvimento

```bash
# Na pasta raiz do template
dotnet new install .
```

### Criar Novo Projeto

#### Método 1: Com Script Automatizado (Recomendado) 🌟

O script automaticamente cria o projeto e organiza os Solution Folders:

```powershell
# Windows PowerShell
pwsh scripts/init-project.ps1 -ProjectName "MeuNovoProjeto"

# Com opções adicionais
pwsh scripts/init-project.ps1 -ProjectName "API.Vendas" -OutputPath "C:\Projects" -OpenIDE
```

**Parâmetros disponíveis:**
- `-ProjectName` (obrigatório): Nome do projeto
- `-OutputPath` (opcional): Diretório onde criar (padrão: atual)
- `-SkipBuild` (opcional): Não compila após criar
- `-OpenIDE` (opcional): Abre no IDE após criar

#### Método 2: Criação Manual

```bash
# 1. Criar projeto do template
dotnet new product-template -n MeuProjeto

# 2. Navegar para o projeto
cd MeuProjeto

# 3. Organizar Solution Folders (IMPORTANTE!)
pwsh ../scripts/organize-solution.ps1

# 4. Restaurar e compilar
dotnet restore
dotnet build

# 5. Executar a API
dotnet run --project src/Api/Api.csproj
```

Acesse: `https://localhost:5001/swagger`

> ⚠️ **Importante:** Por padrão, o .NET não organiza projetos em Solution Folders. Execute o script `organize-solution.ps1` para refletir a estrutura de diretórios na Solution.

### Organizar Solution Folders Existente

Se você já criou um projeto e quer organizá-lo:

```powershell
# Na pasta raiz do projeto
pwsh scripts/organize-solution.ps1

# Ou especificando a solution
pwsh scripts/organize-solution.ps1 -SolutionPath "MeuProjeto.sln"
```

Isso organizará automaticamente:
```
Solution 'MeuProjeto'
├── 📁 src/
│   ├── Api.csproj
│   ├── Kernel.Domain.csproj
│   ├── Kernel.Application.csproj
│   └── Kernel.Infrastructure.csproj
└── 📁 tests/
    ├── UnitTests.csproj
    ├── IntegrationTests.csproj
    ├── E2ETests.csproj
    └── CommonTests.csproj
```

### Desinstalar Template

```bash
dotnet new uninstall Product.Template
```

---

## Características

### 🎯 Recursos Avançados (Novos!)
- **Response Compression** (Brotli + Gzip) - Reduz respostas em até 70-80%
- **Output Caching** - Cache de respostas HTTP com políticas configuráveis
- **Request Deduplication** - Previne requisições duplicadas (idempotência)
- **Feature Flags** - Controle de features sem redeploy
- **Audit Trail** - Rastreamento automático de criação/modificação de entidades
- **Current User Service** - Acesso ao usuário autenticado em toda aplicação

> 📖 **Documentação completa:** [ADVANCED_FEATURES.md](./docs/ADVANCED_FEATURES.md)  
> 🔄 **Migrando da v1.0?** [Guia de Migração](./docs/MIGRATION_GUIDE_v1.0_to_v1.1.md)

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

### Infraestrutura e Observabilidade
- **API RESTful** com Swagger/OpenAPI melhorado
- **API Versioning** completo (URL, Header, Query String)
- **Serilog** para logging estruturado (Console, File, Seq)
- **OpenTelemetry** para traces e métricas (Jaeger, Prometheus, Datadog)
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

### 8. Serilog - Logging Estruturado

O template utiliza **Serilog** para logging estruturado com suporte a múltiplos sinks (Console, File, Seq).

#### Características

- **Logging Estruturado**: Logs em formato JSON com propriedades tipadas
- **Enrichers**: Adiciona contexto automático (Machine Name, Thread ID, Exception Details)
- **Múltiplos Sinks**: Console, Arquivo (com rotação diária), Seq
- **Correlation ID**: Integração com X-Correlation-ID header
- **Performance**: Request logging otimizado com Serilog

#### Configuração (appsettings.json)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341"
        }
      }
    ]
  }
}
```

#### Usando Serilog no código

```csharp
public class ProductService
{
    private readonly ILogger<ProductService> _logger;

    public ProductService(ILogger<ProductService> logger)
    {
        _logger = logger;
    }

    public async Task<Product> GetProductAsync(int id)
    {
        _logger.LogInformation("Buscando produto {ProductId}", id);

        try
        {
            var product = await _repository.GetByIdAsync(id);
            _logger.LogInformation("Produto {ProductId} encontrado: {ProductName}",
                id, product.Name);
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar produto {ProductId}", id);
            throw;
        }
    }
}
```

#### Visualizando logs no Seq

Se você tiver o **Seq** rodando localmente (http://localhost:5341), todos os logs estruturados serão enviados automaticamente para visualização e análise.

**Instalando Seq com Docker**:
```bash
docker run -d --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
```

Acesse: http://localhost:5341

### 9. API Versioning

Suporte completo para versionamento de API com múltiplas estratégias.

#### Características

- **Versionamento por URL**: `/api/v1/products`, `/api/v2/products`
- **Versionamento por Header**: `X-Api-Version: 2.0`
- **Versionamento por Query String**: `?api-version=1.0`
- **Swagger Multi-Versão**: Documentação separada para cada versão
- **Versão Padrão**: v1.0 quando não especificada
- **Deprecation Support**: Marcar versões antigas como descontinuadas

#### Criando controllers versionados

**Versão 1.0**:
```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace MeuProjeto.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        // Implementação v1.0
        return Ok(new[] { "Product A", "Product B" });
    }
}
```

**Versão 2.0 (com breaking changes)**:
```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace MeuProjeto.Api.Controllers.v2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        // Versão 2.0 com paginação
        return Ok(new
        {
            items = new[] { "Product A", "Product B" },
            totalCount = 2,
            page,
            pageSize
        });
    }
}
```

#### Chamando a API

**Por URL (recomendado)**:
```bash
curl https://localhost:5001/api/v1/products
curl https://localhost:5001/api/v2/products
```

**Por Header**:
```bash
curl -H "X-Api-Version: 1.0" https://localhost:5001/api/products
curl -H "X-Api-Version: 2.0" https://localhost:5001/api/products
```

**Por Query String**:
```bash
curl https://localhost:5001/api/products?api-version=1.0
curl https://localhost:5001/api/products?api-version=2.0
```

#### Swagger com múltiplas versões

O Swagger UI exibirá um dropdown para selecionar a versão da API:

- **v1** - Product Template API v1.0
- **v2** - Product Template API v2.0

Cada versão terá sua própria documentação completa com schemas, exemplos e autenticação JWT.

#### Deprecando versões antigas

```csharp
[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    // v1.0 será marcada como deprecated no Swagger
}
```

### 10. OpenTelemetry Lite - Observabilidade Distribuída

O template inclui **OpenTelemetry** para traces e métricas, seguindo o padrão da indústria para observabilidade.

#### Características

- **Traces Distribuídos**: Rastreamento automático de requisições HTTP (entrada e saída)
- **Métricas de Runtime**: CPU, memória, GC, threads
- **Vendor Neutral**: Funciona com Jaeger, Tempo, Datadog, Prometheus, etc
- **Zero Configuração**: Instrumentação automática de ASP.NET Core e HttpClient
- **OTLP Exporter**: Protocolo padrão OpenTelemetry

#### Configuração (appsettings.json)

```json
{
  "OpenTelemetry": {
    "ServiceName": "Product.Template.Api",
    "ServiceVersion": "1.0.0",
    "EnableTraces": true,
    "EnableMetrics": true,
    "EnableConsoleExporter": false,
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

#### Traces automáticos

Todos os requests HTTP são rastreados automaticamente:

```
GET /api/v2/products/123
├─ Span: GET /api/v2/products/{id}
│  ├─ http.method: GET
│  ├─ http.url: /api/v2/products/123
│  ├─ http.status_code: 200
│  └─ duration: 45ms
```

#### Custom Spans (Traces Personalizados)

```csharp
using Product.Template.Api.Configurations;

public class ProductService
{
    public async Task<Product> GetProductAsync(int id)
    {
        // Criar um custom span
        using var activity = OpenTelemetryConfiguration.ActivitySource.StartActivity("GetProduct");
        activity?.SetTag("product.id", id);

        var product = await _repository.GetByIdAsync(id);

        activity?.SetTag("product.found", product != null);
        activity?.SetTag("product.category", product.Category);

        return product;
    }
}
```

#### Métricas disponíveis (automáticas)

**Runtime Metrics**:
- `process.runtime.dotnet.gc.collections.count` - Contagem de GC
- `process.runtime.dotnet.gc.heap.size` - Tamanho do heap
- `process.runtime.dotnet.thread_pool.threads.count` - Threads ativas

**ASP.NET Core Metrics**:
- `http.server.request.duration` - Duração das requisições
- `http.server.active_requests` - Requisições ativas

#### Visualizando Traces com Jaeger

**1. Instalar Jaeger via Docker**:

```bash
docker run -d --name jaeger \
  -p 16686:16686 \
  -p 4317:4317 \
  jaegertracing/all-in-one:latest
```

**2. Configurar endpoint no appsettings.json**:

```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

**3. Acessar Jaeger UI**: http://localhost:16686

#### Visualizando Métricas com Prometheus + Grafana

**Docker Compose (opcional)**:

```yaml
version: '3.8'
services:
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"  # Jaeger UI
      - "4317:4317"    # OTLP gRPC receiver

  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
```

#### Console Exporter (Desenvolvimento)

Para ver traces e métricas no console durante desenvolvimento:

```json
// appsettings.Development.json
{
  "OpenTelemetry": {
    "EnableConsoleExporter": true
  }
}
```

#### Exemplo de trace completo

```
Trace ID: 7f8a9b2c-1234-5678-90ab-cdef12345678
│
├─ HTTP GET /api/v2/products/5 (200 OK) - 52ms
│  ├─ product.id: 5
│  ├─ http.method: GET
│  ├─ http.status_code: 200
│  │
│  └─ GetProductById - 48ms
│     ├─ product.id: 5
│     ├─ product.found: true
│     ├─ product.name: "Product 5"
│     └─ Event: "Querying database"
```

#### Exportando para outros backends

O template usa **OTLP** (OpenTelemetry Protocol), compatível com:

- **Jaeger** - Traces
- **Tempo** - Traces (Grafana)
- **Prometheus** - Metrics
- **Datadog** - Traces + Metrics
- **New Relic** - Traces + Metrics
- **Azure Application Insights** - Traces + Metrics
- **AWS X-Ray** - Traces

Basta alterar o `OtlpEndpoint` para o backend desejado.

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
- **Serilog** (Structured logging)
  - Serilog.AspNetCore
  - Serilog.Enrichers.Environment
  - Serilog.Enrichers.Thread
  - Serilog.Exceptions
  - Serilog.Sinks.File
  - Serilog.Sinks.Seq
- **OpenTelemetry** (Distributed tracing and metrics)
  - OpenTelemetry.Extensions.Hosting
  - OpenTelemetry.Instrumentation.AspNetCore
  - OpenTelemetry.Instrumentation.Http
  - OpenTelemetry.Instrumentation.Runtime
  - OpenTelemetry.Exporter.Console
  - OpenTelemetry.Exporter.OpenTelemetryProtocol
- **AspNetCore.HealthChecks** (Health check endpoints e UI)
- **Microsoft.Extensions.Diagnostics.HealthChecks**

### Documentação e Versionamento
- **Swashbuckle.AspNetCore** (Swagger/OpenAPI)
- **Swashbuckle.AspNetCore.Annotations** (Swagger annotations)
- **Asp.Versioning.Mvc** (API Versioning)
- **Asp.Versioning.Mvc.ApiExplorer** (Swagger multi-version support)

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

### 📚 Documentação Completa

- **[📑 Índice de Documentação](./docs/INDEX.md)** - Encontre rapidamente o que precisa
- **[🏗️ Arquitetura](./docs/ARCHITECTURE.md)** - Arquitetura detalhada do projeto
- **[🎯 Recursos Avançados](./docs/ADVANCED_FEATURES.md)** - Guia dos recursos v1.1.0
- **[🔄 Guia de Migração](./docs/MIGRATION_GUIDE_v1.0_to_v1.1.md)** - Migrar de v1.0 para v1.1
- **[✅ Checklist de Validação](./docs/VALIDATION_CHECKLIST.md)** - Validar implementação
- **[📋 Changelog](./docs/CHANGELOG.md)** - Histórico de mudanças
- **[🤝 Contribuindo](./docs/CONTRIBUTING.md)** - Como contribuir

### 🔗 Links Rápidos

- [MediatR Implementation](./MEDIATR_IMPLEMENTATION_SUMMARY.md)
- [Rotas Criadas](./ROTAS_CRIADAS.md)
- [Agents Documentation](./docs/AGENTS.md)
