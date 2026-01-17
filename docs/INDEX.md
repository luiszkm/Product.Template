# 📚 Índice de Documentação - Product Template

Bem-vindo à documentação completa do Product Template! Este índice ajuda você a encontrar rapidamente o que precisa.

## 🚀 Começando

| Documento | Descrição | Para quem? |
|-----------|-----------|------------|
| [README.md](../README.md) | Visão geral, instalação e início rápido | Todos |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Arquitetura detalhada do projeto | Desenvolvedores |
| [CONTRIBUTING.md](./CONTRIBUTING.md) | Como contribuir com o projeto | Contribuidores |
| [FAQ.md](./FAQ.md) | Perguntas frequentes e troubleshooting | Todos |

---

## 🎯 Recursos Avançados (v1.1.0)

| Documento | Conteúdo | Nível |
|-----------|----------|-------|
| [ADVANCED_FEATURES.md](./ADVANCED_FEATURES.md) | Guia completo dos 5 recursos avançados | Intermediário |
| [IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md) | Resumo executivo da implementação | Todos |
| [VALIDATION_CHECKLIST.md](./VALIDATION_CHECKLIST.md) | Checklist para validar recursos | Desenvolvedor |

### Recursos Específicos

#### Response Compression
- **Arquivo:** `src/Api/Configurations/CompressionConfiguration.cs`
- **Documentação:** [ADVANCED_FEATURES.md#1-response-compression](./ADVANCED_FEATURES.md#1-response-compression-)
- **Benefício:** Reduz 70-80% do tamanho das respostas

#### Output Caching
- **Arquivo:** `src/Api/Configurations/CachingConfiguration.cs`
- **Documentação:** [ADVANCED_FEATURES.md#2-output-caching](./ADVANCED_FEATURES.md#2-output-caching-)
- **Benefício:** Reduz 90% do tempo de resposta (cache hit)

#### Request Deduplication
- **Arquivo:** `src/Api/Middleware/RequestDeduplicationMiddleware.cs`
- **Documentação:** [ADVANCED_FEATURES.md#3-request-deduplication](./ADVANCED_FEATURES.md#3-request-deduplication-)
- **Benefício:** Previne processamento duplicado

#### Feature Flags
- **Arquivo:** `src/Api/Configurations/FeatureFlagsConfiguration.cs`
- **Documentação:** [ADVANCED_FEATURES.md#4-feature-flags](./ADVANCED_FEATURES.md#4-feature-flags-)
- **Benefício:** Deploy sem downtime

#### Audit Trail
- **Arquivos:** `src/Shared/Kernel.Domain/SeedWorks/AuditableEntity.cs` e mais
- **Documentação:** [ADVANCED_FEATURES.md#5-audit-trail](./ADVANCED_FEATURES.md#5-audit-trail-)
- **Benefício:** Rastreabilidade completa

---

## 🔄 Migração e Atualização

| Documento | Descrição | Quando usar |
|-----------|-----------|-------------|
| [MIGRATION_GUIDE_v1.0_to_v1.1.md](./MIGRATION_GUIDE_v1.0_to_v1.1.md) | Guia passo a passo para migrar da v1.0 para v1.1 | Ao atualizar projeto existente |
| [CHANGELOG.md](./CHANGELOG.md) | Histórico de mudanças entre versões | Para saber o que mudou |

---

## 📖 Arquitetura e Padrões

| Documento | Tópico | Nível |
|-----------|--------|-------|
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Clean Architecture e DDD | Intermediário |
| [MEDIATR_IMPLEMENTATION_SUMMARY.md](../MEDIATR_IMPLEMENTATION_SUMMARY.md) | Implementação do MediatR/CQRS | Avançado |
| [MEDIATR_MIGRATION.md](./MEDIATR_MIGRATION.md) | Migração para MediatR | Avançado |

### Conceitos Chave

- **Clean Architecture:** Separação em camadas (Domain, Application, Infrastructure, API)
- **DDD:** Entities, Aggregates, Value Objects, Domain Events
- **CQRS:** Commands e Queries separados
- **Repository Pattern:** Abstração de acesso a dados
- **Unit of Work:** Coordenação de transações

---

## 🛠️ Configuração e Setup

### Configurações por Recurso

| Recurso | Arquivo de Config | Seção appsettings.json |
|---------|-------------------|------------------------|
| Response Compression | `CompressionConfiguration.cs` | N/A (automático) |
| Output Caching | `CachingConfiguration.cs` | `"Caching"` |
| Feature Flags | `FeatureFlagsConfiguration.cs` | `"FeatureFlags"` |
| JWT Authentication | `SecurityConfiguration.cs` | `"Jwt"` |
| Rate Limiting | `RateLimitingConfiguration.cs` | `"IpRateLimiting"` |
| Serilog | `SerilogConfiguration.cs` | `"Serilog"` |
| OpenTelemetry | `OpenTelemetryConfiguration.cs` | `"OpenTelemetry"` |

### Middleware Pipeline

Ordem recomendada no `Program.cs`:

1. `UseResponseCompression()` - Compressão
2. `UseCachingConfiguration()` - Cache
3. `UseSerilogConfiguration()` - Logging
4. `UseMiddleware<RequestLoggingMiddleware>()` - Request logging
5. `UseMiddleware<RequestDeduplicationMiddleware>()` - Deduplicação
6. `UseMiddleware<IpWhitelistMiddleware>()` - IP filtering
7. `UseHttpsRedirection()` - HTTPS
8. `UseRouting()` - Routing
9. `UseSecurityConfiguration()` - CORS
10. `UseAuthentication()` - Auth
11. `UseAuthorization()` - Authz
12. `UseRateLimiting()` - Rate limit

---

## 🧪 Testes

| Documento | Descrição |
|-----------|-----------|
| [VALIDATION_CHECKLIST.md](./VALIDATION_CHECKLIST.md) | Testes manuais para cada recurso |

### Estrutura de Testes

```
tests/
├── UnitTests/          # Testes unitários (lógica de negócio)
├── IntegrationTests/   # Testes de integração (banco de dados)
├── E2ETests/           # Testes end-to-end (API completa)
└── CommonTests/        # Fixtures e helpers compartilhados
```

---

## 🐛 Troubleshooting

### Problemas Comuns

| Problema | Solução | Documentação |
|----------|---------|--------------|
| Build error: IHttpContextAccessor | Adicionar pacote `Microsoft.AspNetCore.Http.Abstractions` | [ADVANCED_FEATURES.md](./ADVANCED_FEATURES.md#troubleshooting) |
| Cache não funciona | Verificar `"Caching:Enabled": true` | [ADVANCED_FEATURES.md](./ADVANCED_FEATURES.md#troubleshooting) |
| Compression não ativa | Cliente deve enviar `Accept-Encoding` header | [ADVANCED_FEATURES.md](./ADVANCED_FEATURES.md#troubleshooting) |
| Audit Trail vazio | Verificar se usuário está autenticado | [ADVANCED_FEATURES.md](./ADVANCED_FEATURES.md#troubleshooting) |

---

## 📊 Métricas e Performance

### Benchmarks

| Recurso | Métrica | Melhoria |
|---------|---------|----------|
| Response Compression | Tamanho da resposta | -70% a -80% |
| Output Caching | Tempo de resposta | -90% (cache hit) |
| Request Deduplication | Processamentos duplicados | Eliminados |

### Monitoramento

- **Serilog:** Logs estruturados (Console, File, Seq)
- **OpenTelemetry:** Traces e métricas distribuídas
- **Health Checks:** Status de componentes (database, memory, etc)

---

## 📦 Pacotes e Dependências

### Principais Pacotes

| Pacote | Versão | Uso |
|--------|--------|-----|
| MediatR | 12.x | CQRS (Commands/Queries) |
| Polly | 8.x | Resiliência (Retry, Circuit Breaker) |
| Serilog | 8.x | Logging estruturado |
| OpenTelemetry | 1.9.x | Observabilidade |
| EF Core | 10.x | ORM |
| Microsoft.FeatureManagement | 4.4.0 | Feature Flags |

### Compatibilidade

- **.NET:** 10.0+
- **C#:** 12.0+
- **Entity Framework Core:** 10.0+

---

## 🚀 Deployment

### Ambientes

| Ambiente | appsettings | Recomendações |
|----------|-------------|---------------|
| Development | `appsettings.Development.json` | Cache desabilitado, logs verbosos |
| Staging | `appsettings.Staging.json` | Similar a produção |
| Production | `appsettings.Production.json` | Cache habilitado, logs otimizados |

### Docker

```bash
docker build -t product-template .
docker run -p 5000:8080 product-template
```

---

## 🤝 Contribuindo

| Documento | Descrição |
|-----------|-----------|
| [CONTRIBUTING.md](./CONTRIBUTING.md) | Guia de contribuição |
| [CHANGELOG.md](./CHANGELOG.md) | Histórico de mudanças |

### Como Contribuir

1. Fork o repositório
2. Criar branch de feature
3. Fazer commit das mudanças
4. Push para o branch
5. Abrir Pull Request

---

## 📞 Recursos Externos

### Documentação Microsoft

- [ASP.NET Core](https://learn.microsoft.com/aspnet/core)
- [Entity Framework Core](https://learn.microsoft.com/ef/core)
- [Output Caching](https://learn.microsoft.com/aspnet/core/performance/caching/output)
- [Feature Management](https://learn.microsoft.com/azure/azure-app-configuration/use-feature-flags-dotnet-core)
- [Response Compression](https://learn.microsoft.com/aspnet/core/performance/response-compression)

### Bibliotecas

- [MediatR](https://github.com/jbogard/MediatR)
- [Polly](https://github.com/App-vNext/Polly)
- [Serilog](https://serilog.net/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)

---

## 📝 Notas de Versão

### v1.1.0 (2026-01-17) - Recursos Avançados

✨ **Novidades:**
- Response Compression
- Output Caching
- Request Deduplication
- Feature Flags
- Audit Trail

📖 **Documentação:** [CHANGELOG.md](./CHANGELOG.md)

### v1.0.0 (2025-01-XX) - Release Inicial

🎉 **Lançamento inicial** com Clean Architecture, DDD, CQRS, e mais.

---

## 🗺️ Roadmap

Recursos planejados para versões futuras:

- [ ] Background Jobs (Hangfire/Quartz)
- [ ] Message Queue (RabbitMQ/Azure Service Bus)
- [ ] SignalR (Real-time)
- [ ] File Upload/Storage (MinIO/S3)
- [ ] Multi-Tenancy
- [ ] Localization/i18n
- [ ] GraphQL (HotChocolate)
- [ ] gRPC
- [ ] API Gateway (YARP)

---

## ❓ FAQ

### Como começar?

1. Ler [README.md](../README.md)
2. Instalar template: `dotnet new install .`
3. Criar projeto: `dotnet new product-template -n MeuProjeto`

### Como migrar da v1.0 para v1.1?

Seguir o [Guia de Migração](./MIGRATION_GUIDE_v1.0_to_v1.1.md)

### Como habilitar cache?

Ver [Output Caching](./ADVANCED_FEATURES.md#2-output-caching-)

### Como usar Feature Flags?

Ver [Feature Flags](./ADVANCED_FEATURES.md#4-feature-flags-)

---

**Última atualização:** 2026-01-17  
**Versão do Template:** 1.1.0  
**Mantido por:** Product Template Team

