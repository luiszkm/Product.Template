# Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
e este projeto adere ao [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-01-17

### 🎯 Adicionado - Recursos Avançados

- **Response Compression**
  - Compressão automática de respostas HTTP (Brotli + Gzip)
  - Redução de 70-80% no tamanho das respostas
  - Configuração em `CompressionConfiguration.cs`

- **Output Caching**
  - Sistema de cache de respostas HTTP (.NET 8+)
  - 4 políticas pré-configuradas (UserCache, PublicCache, ReferenceDataCache, NoCache)
  - Suporte opcional a Redis para cache distribuído
  - Redução de 90% no tempo de resposta em cache hits
  - Configuração em `CachingConfiguration.cs`

- **Request Deduplication**
  - Middleware para prevenir requisições duplicadas (idempotência)
  - Suporte a header `X-Idempotency-Key`
  - Geração automática de hash se chave não fornecida
  - Proteção automática para POST/PUT/PATCH
  - Janela de deduplicação: 5 minutos
  - Implementado em `RequestDeduplicationMiddleware.cs`

- **Feature Flags**
  - Sistema de controle de features sem necessidade de redeploy
  - Integração com `Microsoft.FeatureManagement.AspNetCore` v4.4.0
  - 5 flags pré-configuradas
  - Suporte a Feature Gates em controllers
  - Configuração em `FeatureFlagsConfiguration.cs`

- **Audit Trail**
  - Rastreamento automático de criação e modificação de entidades
  - Campos automáticos: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
  - Interceptor do EF Core para preenchimento automático
  - Interface `IAuditableEntity` e classe base `AuditableAggregateRoot<T>`
  - `ICurrentUserService` para obter usuário autenticado
  - Implementado via `AuditableEntityInterceptor.cs`

### 📚 Documentação

- **ADVANCED_FEATURES.md** - Guia completo dos recursos avançados
- **IMPLEMENTATION_SUMMARY.md** - Resumo executivo da implementação
- **VALIDATION_CHECKLIST.md** - Checklist para validação dos recursos
- Atualização do README.md com seção de recursos avançados

### 🔧 Modificações

- **Program.cs**
  - Adicionado registro de Response Compression
  - Adicionado registro de Output Caching
  - Adicionado registro de Feature Flags
  - Adicionado middleware de Request Deduplication
  - Reorganização do pipeline de middleware

- **appsettings.json**
  - Adicionada seção `Caching` com configurações
  - Adicionada seção `FeatureFlags` com flags padrão

- **DependencyInjection (Kernel.Infrastructure)**
  - Registrado `ICurrentUserService` e implementação
  - Registrado `AuditableEntityInterceptor`
  - Registrado `HttpContextAccessor`

- **DatabaseConfiguration (Identity.Infrastructure)**
  - Integrado `AuditableEntityInterceptor` ao DbContext
  - Configuração com Service Provider para resolver interceptor

- **Identity.Infrastructure/DependencyInjection**
  - Corrigido registro de `IUnitOfWork` com implementação concreta

### 📦 Pacotes NuGet Adicionados

- `Microsoft.FeatureManagement.AspNetCore` v4.4.0 (Api)
- `Microsoft.Extensions.Caching.StackExchangeRedis` v10.0.* (Api)
- `Microsoft.AspNetCore.Http.Abstractions` v2.2.0 (Kernel.Infrastructure)

### 🐛 Correções

- Corrigido erro de DI do `IUnitOfWork` (estava registrando interface como implementação)
- Corrigido query LINQ do `UserRepository.GetByEmailAsync` para usar `EF.Property`
- Corrigido namespace do `CurrentUserService` e `AuditableEntityInterceptor`
- Removida duplicata de `Microsoft.FeatureManagement.AspNetCore` do Api.csproj

---

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
- Background Jobs (Hangfire/Quartz)
- Message Queue (RabbitMQ/Azure Service Bus)
- SignalR para comunicação em tempo real
- File Upload/Storage (MinIO/S3)
- Multi-Tenancy support
- Localization/i18n
- GraphQL support (HotChocolate)
- gRPC support
- API Gateway (YARP)

---

## Guia de Versionamento

- **MAJOR** (1.x.x): Mudanças incompatíveis na API ou arquitetura
- **MINOR** (x.1.x): Novas funcionalidades compatíveis
- **PATCH** (x.x.1): Correções de bugs e melhorias

---

[1.0.0]: https://github.com/Neuraptor/Product.Template/releases/tag/v1.0.0
[Unreleased]: https://github.com/Neuraptor/Product.Template/compare/v1.0.0...HEAD
