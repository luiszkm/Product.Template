# API Instructions — Product.Template

> Regras da camada API derivadas do padrão real do `IdentityController` e `Program.cs`.

## Padrão de controller

Extraído de `src/Api/Controllers/v1/IdentityController.cs`:

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Tags("{Module}")]
public class {Module}Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<{Module}Controller> _logger;

    // Constructor injection
}
```

### Regras obrigatórias para controllers
1. Herdam de `ControllerBase`.
2. Anotados com `[ApiController]`, `[ApiVersion("1.0")]`, `[Produces("application/json")]`.
3. Rota: `api/v{version:apiVersion}/[controller]`.
4. `[Tags("{Module}")]` para agrupamento no Scalar.
5. Injetam `IMediator` e `ILogger<T>` no construtor.
6. Controllers vivem em `src/Api/Controllers/v1/`.
7. Um controller por módulo: `{Module}Controller`.

## Padrão de action

```csharp
/// <summary>
/// Descrição da action
/// </summary>
/// <response code="200">Sucesso</response>
/// <response code="401">Token inválido ou ausente</response>
[HttpGet("{id:guid}")]
[Authorize(Policy = SecurityConfiguration.UserOnlyPolicy)]
[ProducesResponseType(typeof(UserOutput), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<UserOutput>> GetById(Guid id, CancellationToken cancellationToken)
{
    var query = new GetUserByIdQuery(id);
    var result = await _mediator.Send(query, cancellationToken);
    return Ok(result);
}
```

### Regras obrigatórias para actions
1. `CancellationToken cancellationToken` é SEMPRE o último parâmetro.
2. `[Authorize(Policy = SecurityConfiguration.{PolicyName})]` — nunca `[Authorize]` sem Policy.
3. `[AllowAnonymous]` para endpoints públicos.
4. `[ProducesResponseType]` para CADA status code possível.
5. XML doc comments (`///`) com exemplos.
6. Corpo da action: dispatch para MediatR e retorno. Sem lógica.

## Contratos HTTP (extraídos do IdentityController)

| Operação | Verbo | Status sucesso | Body resposta |
|----------|-------|---------------|---------------|
| Buscar por ID | GET | 200 | `{Output}` |
| Listar (paginado) | GET | 200 | `PaginatedListOutput<{Output}>` |
| Criar | POST | 201 | `{Output}` + `CreatedAtAction` |
| Atualizar | PUT | 200 | `{Output}` |
| Deletar | DELETE | 204 | (vazio) |
| Login / Ação | POST | 200 | `{ActionOutput}` |

### POST que cria recurso
```csharp
var result = await _mediator.Send(command, cancellationToken);
return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1" }, result);
```

### DELETE
```csharp
await _mediator.Send(command, cancellationToken);
return NoContent();
```

## Paginação

Padrão real: query herda `ListInput`, handler retorna `PaginatedListOutput<T>`.

```csharp
// Request via query string
[HttpGet]
public async Task<ActionResult<PaginatedListOutput<UserOutput>>> ListAll(
    [FromQuery] ListUserQuery query,
    CancellationToken cancellationToken)
{
    var result = await _mediator.Send(query, cancellationToken);
    return Ok(result);
}
```

`ListInput` contém: `PageNumber`, `PageSize`, `SearchTerm`, `SortBy`, `SortDirection`.

## Tratamento de erros

Mapeamento real via `ApiGlobalExceptionFilter`:

| Exceção | HTTP Status | ProblemDetails.Type |
|---------|-------------|---------------------|
| `NotFoundException` | 404 | `Not Found` |
| `DomainException` | 422 | `UnProcessableEntity` |
| `BusinessRuleException` | 400 | `BusinessRuleViolation` |
| `ValidationException` | 400 | (FluentValidation errors) |
| `UnauthorizedAccessException` | 401 | (padrão ASP.NET) |
| Qualquer outra | 500 | `UnexpectedError` |

- Em Development, `StackTrace` é incluída no response.
- Handlers lançam as exceções tipadas. O filter converte para `ProblemDetails`.
- Controllers NÃO fazem try/catch.

## Segurança nos endpoints

Policies definidas em `SecurityConfiguration.cs`:
```csharp
SecurityConfiguration.AuthenticatedPolicy  // "Authenticated"
SecurityConfiguration.AdminOnlyPolicy      // "AdminOnly"
SecurityConfiguration.UserOnlyPolicy       // "UserOnly"
SecurityConfiguration.UsersReadPolicy      // "UsersRead"
SecurityConfiguration.UsersManagePolicy    // "UsersManage"
```

Toda policy nova:
1. Definir como `public const string` em `SecurityConfiguration.cs`.
2. Registrar em `AddAuthorization(options => ...)`.
3. Atualizar `docs/security/RBAC_MATRIX.md`.

## Middleware Pipeline (ordem real do Program.cs)

```
ResponseCompression → OutputCaching → SerilogRequestLogging
→ RequestLoggingMiddleware → RequestDeduplicationMiddleware
→ TenantResolutionMiddleware → IpWhitelistMiddleware
→ ForwardedHeaders → HttpsRedirection → Routing
→ CORS → Authentication → Authorization → RateLimiting
→ HealthChecks → Scalar Documentation → MapControllers
```

## Headers obrigatórios

- `X-Tenant: {tenant}` — obrigatório em toda request (resolvido pelo TenantResolutionMiddleware).
- `Authorization: Bearer {jwt}` — em endpoints protegidos.
- `X-Idempotency-Key` — opcional, para deduplicação de requests (POST/PUT/PATCH).

