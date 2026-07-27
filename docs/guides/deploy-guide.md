# Guia de deploy — Product.Template API

Configuração de ambiente para containers (Docker, Kubernetes, Azure Container Apps) e CI/CD.

## Variáveis obrigatórias

| Variável | Exemplo | Descrição |
|----------|---------|-----------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Ambiente ASP.NET Core |
| `ASPNETCORE_URLS` | `http://+:8080` | URL de escuta |
| `ConnectionStrings__HostDb` | `Host=...` | PostgreSQL do catálogo de tenants |
| `ConnectionStrings__AppDb` | `Host=...` | PostgreSQL da aplicação (shared DB) |
| `Jwt__Secret` | *(≥ 32 caracteres)* | Obrigatório quando `Jwt__Enabled=true` |
| `Jwt__Issuer` | *(string)* | Obrigatório fora de Development — startup falha se vazio |
| `Jwt__Audience` | *(string)* | Obrigatório fora de Development — startup falha se vazio |
| `Monitoring__ApiKey` | *(segredo)* | Obrigatório quando `Monitoring__RequireApiKey=true` (produção) |
| `Cors__AllowedOriginsEnv` | `https://app.example.com` | Origens CORS separadas por vírgula quando `Cors:AllowedOrigins` está vazio |
| `ConnectionStrings__HostDb` / `ConnectionStrings__AppDb` | *(sem a senha placeholder do template)* | Startup falha fora de Development se a string ainda contiver `YourStrong!Pass123` |

Sem `Jwt:Issuer`/`Jwt:Audience`, `ValidateIssuer`/`ValidateAudience` ficam **desligados** e um token de qualquer emissor validaria — por isso `JwtStartupValidation` exige ambos fora de Development.

## Variáveis recomendadas

| Variável | Default | Descrição |
|----------|---------|-----------|
| `Jwt__Enabled` | `true` | `false` desativa JWT e validação de secret/issuer/audience no startup |
| `ForwardedHeaders__KnownProxies` | `[]` | IPs do(s) reverse proxy(ies) confiável(is) (ALB, ingress, nginx) |
| `ForwardedHeaders__KnownNetworks` | `[]` | Redes CIDR confiáveis (ex.: `10.0.0.0/8`) alternativa a `KnownProxies` |
| `Monitoring__RequireApiKey` | `false` (dev), `true` (prod) | Protege `/metrics` e `/health/ready` |
| `Monitoring__RequireApiKeyInDevelopment` | `false` | Exige chave também em Development |
| `FeatureFlags__EnableAI` | `false` | Endpoints `/api/v1/ai/*` retornam 404 quando desligado |
| `FeatureFlags__EnableCaching` | `true` | Output cache desligado quando `false` |
| `FeatureFlags__EnableAuditTrail` | `true` | Interceptor `AuditLogInterceptor` omitido quando `false` |
| `FeatureFlags__EnableRequestDeduplication` | `true` | Middleware de deduplicação POST/PUT/PATCH |
| `FeatureFlags__EnableAdvancedLogging` | `true` | Middleware de log detalhado com body mascarado |
| `Redis__ConnectionString` | *(vazio)* | Redis para cache distribuído; vazio usa memória |
| `DisableDatabaseInitialization` | `false` | `true` em testes/E2E sem seed |
| `DisableTenantMiddleware` | `false` | `true` apenas em cenários de teste |

## CORS em produção

Em `appsettings.Production.json`, `Cors:AllowedOrigins` pode estar vazio. Defina origens via ambiente:

```bash
export Cors__AllowedOriginsEnv="https://app.example.com,https://admin.example.com"
```

Alternativa: lista JSON em `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, etc.

Fora de Development, sem origens configuradas, o CORS nega todos os browsers (`SetIsOriginAllowed(_ => false)`).

## Forwarded headers (reverse proxy / load balancer)

Sem `ForwardedHeaders:KnownProxies`/`KnownNetworks` configurados, `ForwardedHeadersMiddleware` só confia em loopback — atrás de um reverse proxy real (nginx/ALB/ingress) os headers `X-Forwarded-For`/`X-Forwarded-Proto` são **ignorados silenciosamente** e todo cliente aparenta ter o IP do proxy, quebrando rate limiting e IP allowlisting por cliente.

```json
"ForwardedHeaders": {
  "KnownProxies": ["10.0.0.5"],
  "KnownNetworks": ["10.0.0.0/8"]
}
```

`ClientIpResolver` lê apenas `HttpContext.Connection.RemoteIpAddress` (já reescrito pelo middleware) — nunca os headers diretamente, para não permitir que qualquer chamador falsifique o próprio IP.

## Monitoring e health

| Endpoint | Auth | Notas |
|----------|------|-------|
| `/health/live` | Nenhuma | Liveness — sempre 200 se o processo responde |
| `/health/ready` | `X-Monitoring-Api-Key` em produção | Readiness — DB host + app; resposta JSON minimalista (`status`, `timestamp`) — **não** usa o formato Xabaril UI, que serializaria mensagens de exceção (detalhes de conexão/auth do DB) para quem chamar o endpoint sem autenticação |
| `/health` | Admin JWT (fora de Development) | Detalhe completo dos checks |
| `/metrics` | `X-Monitoring-Api-Key` em produção | Prometheus scrape |

```bash
curl -H "X-Monitoring-Api-Key: $MONITORING_KEY" https://api.example.com/health/ready
curl -H "X-Monitoring-Api-Key: $MONITORING_KEY" https://api.example.com/metrics
```

Query string alternativa: `?api_key=` (evitar em logs de proxy).

## Request deduplication

Ativa com `FeatureFlags__EnableRequestDeduplication=true` (default).

- Métodos: POST, PUT, PATCH
- Chave: `X-Idempotency-Key` ou hash do body
- Escopo: por tenant (`TenantId` resolvido ou header `X-Tenant`)
- Duplicata na janela de 1s: HTTP **409** com header `X-Duplicate-Request: true`
- Com Redis (`Redis__ConnectionString`), dedup é consistente entre réplicas

## Feature flags em endpoints

Controllers ou actions com `[FeatureGate(FeatureFlags.Nome)]` retornam **404** quando a flag está desligada em `FeatureFlags` (config ou `FeatureFlags__*` no ambiente).

Flags atuais: `EnableAI`, `EnableCaching`, `EnableAuditTrail`, `EnableRequestDeduplication`, `EnableAdvancedLogging`, `EnableExperimentalFeatures`.

Middleware (dedup, logging avançado, caching) usa as mesmas chaves via `IConfiguration` no pipeline.

Documentação completa: [feature-flags.md](./feature-flags.md).

## Health Checks UI

O dashboard `/healthchecks-ui` (pacote Xabaril) permanece **desligado**. A avaliação em `HealthChecksUiSupport` indica que a versão NuGet mais recente (9.0.0) ainda conflita com IdentityModel no .NET 10.

Monitorização recomendada: `/health/ready`, `/health`, Grafana/Prometheus e Seq. Acompanhar [AspNetCore.Diagnostics.HealthChecks](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks).

Reavaliar periodicamente (ex.: trimestralmente ou antes de upgrades de major .NET): executar `HealthChecksUiSupport.Evaluate()` ou consultar NuGet por uma versão do pacote `AspNetCore.HealthChecks.UI` compatível com .NET 10 sem conflito IdentityModel. O campo `LastCheckedUtc` regista a última verificação manual ou em CI.

## Checklist antes de produção

1. `Jwt__Secret` forte (não placeholder); `Jwt__Issuer`/`Jwt__Audience` definidos
2. `Monitoring__ApiKey` definido
3. `Cors__AllowedOriginsEnv` com domínios do frontend
4. Connection strings via secrets do orquestrador (sem a senha placeholder do template)
5. `ForwardedHeaders__KnownProxies`/`KnownNetworks` apontando para o(s) proxy(ies) real(is)
6. `FeatureFlags__EnableAI` conforme contrato Azure OpenAI
7. Redis se houver mais de uma réplica da API

Itens 1 e 4 são validados no startup (`JwtStartupValidation`, `ConnectionsConfigurations.ValidateConnectionStrings`) — a aplicação falha ao iniciar fora de Development se estiverem incorretos.

## Referências

- [Observabilidade](./observability-guide.md)
- [Tenant identifiers](./tenant-identifiers.md)
- Skill CI/CD: `/setup-cicd` (`.cursor/skills/setup-cicd/SKILL.md`)
- Regras: `.cursor/rules/cicd.mdc`
