# Identificadores de tenant

## Modelo actual

| Campo | Tipo | Onde | Função |
|-------|------|------|--------|
| `tenantId` | `Guid` | Host `Tenants`, App (`IMultiTenantEntity.TenantId`), JWT/claims internos | Chave de isolamento de dados e referência na API de gestão |
| `tenantKey` | `string` | Host `Tenants`, header `X-Tenant` | Identificador humano estável para resolução por request |

O agregado `Tenant` usa `Id` (herdado de `AggregateRoot`), persistido em `TenantConfig.TenantId`.

## Porque não `long` sequencial

- IDs `1`, `2`, `3` são previsíveis e facilitam enumeração em APIs administrativas.
- O template antigo aceitava `tenantId` no `POST`, delegando a numeração ao cliente.
- `Guid` gerado no servidor reduz superfície de adivinhação e mantém unicidade global.

## Resolução no request (app normal)

Utilizadores da aplicação **não** passam `tenantId` na URL do dia-a-dia:

1. Cliente envia `X-Tenant: <tenantKey>` (ex.: `public`).
2. `TenantResolutionMiddleware` carrega `TenantConfig` e preenche `ITenantContext`.
3. Filtros EF e `MultiTenantSaveChangesInterceptor` usam `TenantId` (`Guid`) nas entidades.

## API de gestão (painel admin)

- Listagem, detalhe, update e deactivate usam `tenantId` (`uuid`) na rota.
- Criação: body **sem** `tenantId`; a resposta `201` devolve o Guid criado.

## Seed e testes

```csharp
WellKnownTenants.Public // 00000000-0000-0000-0000-000000000001
```

Configuração em `appsettings` (`Tenants` section): cada entrada usa `tenantId` em formato Guid string.

## Migração desde `long`

Breaking changes para integradores:

| Antes | Depois |
|-------|--------|
| `GET /tenants/1` | `GET /tenants/{uuid}` |
| POST com `"tenantId": 10` | POST sem `tenantId` |
| TypeScript `tenantId: number` | `tenantId: string` |
| Coluna SQL `bigint` | `uuid` |

Requer migração EF / base de dados alinhada ao branch `cursor/tenants-guid-tests-541a`.

## Novos módulos

Entidades multi-tenant:

- Implementar `IMultiTenantEntity` com `Guid TenantId`.
- Factories recebem `Guid tenantId` (normalmente do `ITenantContext` após resolução).
- Não expor sequências numéricas de tenant em contratos públicos.
