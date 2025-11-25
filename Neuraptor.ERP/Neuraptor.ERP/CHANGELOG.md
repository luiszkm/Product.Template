# Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
e este projeto adere ao [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-01-XX

### Adicionado
- **Arquitetura Base**
  - Clean Architecture com separação em camadas (Domain, Application, Infrastructure, API)
  - Domain-Driven Design (DDD) com SeedWorks (Entity, AggregateRoot, Value Objects, Domain Events)
  - CQRS com CommandBus e QueryBus
  - Behaviors automáticos (Logging, Performance, Validation)

- **Resiliência e Segurança**
  - Políticas de Retry com backoff exponencial (Polly)
  - Circuit Breaker para proteção contra falhas em cascata
  - Rate Limiting configurável por endpoint e IP
  - IP Whitelist/Blacklist com suporte a CIDR
  - Request/Response Logging com Correlation ID e mascaramento de dados sensíveis
  - Health Checks com UI (database, memory, disk space)
  - CORS configurável por ambiente
  - JWT Authentication (opcional, desabilitada por padrão)

- **Observabilidade**
  - Serilog para logging estruturado (Console, File, Seq)
  - OpenTelemetry para traces e métricas distribuídas
  - Suporte a exporters: Console, OTLP (Jaeger, Prometheus, Datadog, etc)
  - Métricas automáticas de runtime (.NET GC, threads, memória)
  - Traces automáticos de HTTP (ASP.NET Core e HttpClient)

- **API**
  - API Versioning completo (URL, Header, Query String)
  - Swagger/OpenAPI melhorado com múltiplas versões
  - Swagger Annotations para documentação rica
  - Suporte a XML documentation
  - JWT Auth integrado no Swagger
  - Controllers de exemplo versionados (v1 e v2)

- **Infraestrutura**
  - Entity Framework Core com suporte a múltiplos bancos
  - Repository Pattern e Unit of Work
  - Docker pronto para uso
  - Template configurável via dotnet new

- **Testes**
  - Estrutura completa: UnitTests, IntegrationTests, E2ETests
  - CommonTests para fixtures compartilhados
  - xUnit como framework
  - Bogus para geração de dados fake

- **Configuração e Padronização**
  - .editorconfig com padrões de código C#
  - global.json para versão do SDK
  - Directory.Build.props para propriedades compartilhadas
  - Template.json com substituição automática de GUIDs e namespaces

- **Documentação**
  - README.md completo com exemplos
  - ARCHITECTURE.md com explicação detalhada
  - Documentação de todas as funcionalidades avançadas

### Características Técnicas
- **.NET 8.0** como target framework
- **Scrutor** para Assembly Scanning automático
- **FluentValidation** para validação de comandos
- Suporte a **Nullable Reference Types**
- **Implicit Usings** habilitado

### Pacotes Principais
- Polly 8.5.0
- Serilog.AspNetCore 8.0.0
- OpenTelemetry.* 1.9.0
- Asp.Versioning.Mvc 8.1.0
- AspNetCoreRateLimit 5.0.0
- AspNetCore.HealthChecks.UI 8.0.1
- Swashbuckle.AspNetCore 6.6.2
- Entity Framework Core 9.0.9

---

## [Unreleased]

### Planejado para versões futuras
- Distributed Caching (Redis)
- Response Compression
- Output Caching (.NET 8)
- Background Jobs (Hangfire)
- Message Queue (RabbitMQ/Azure Service Bus)
- GraphQL support (opcional)
- gRPC support (opcional)

---

## Guia de Versionamento

- **MAJOR** (1.x.x): Mudanças incompatíveis na API ou arquitetura
- **MINOR** (x.1.x): Novas funcionalidades compatíveis
- **PATCH** (x.x.1): Correções de bugs e melhorias

---

[1.0.0]: https://github.com/Neuraptor/Neuraptor.ERP/releases/tag/v1.0.0
[Unreleased]: https://github.com/Neuraptor/Neuraptor.ERP/compare/v1.0.0...HEAD
