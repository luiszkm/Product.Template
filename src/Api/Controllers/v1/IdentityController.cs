using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Product.Template.Core.Identity.Application.Handlers.Auth;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Core.Identity.Application.Queries.Users;


namespace Product.Template.Api.Controllers.v1;

/// <summary>
/// 🔐 Identity API - Autenticação e Registro de Usuários
/// </summary>
/// <remarks>
/// Esta API gerencia toda a autenticação da aplicação utilizando JWT Bearer Tokens.
/// 
/// ## Fluxo de Autenticação
/// 1. Registre um novo usuário via `/register`
/// 2. Faça login via `/login` para obter o token JWT
/// 3. Use o token no header `Authorization: Bearer {token}` nas chamadas protegidas
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Tags("Identity")] // 🏷️ Tag para agrupamento no Scalar
public class IdentityController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(IMediator mediator, ILogger<IdentityController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// 👤 Busca um usuário por ID
    /// </summary>
    /// <param name="id">ID único do usuário (GUID)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados completos do usuário</returns>
    /// <remarks>
    /// ## Exemplo de Requisição
    /// ```http
    /// GET /api/v1/identity/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
    /// ```
    /// 
    /// ## Exemplo de Resposta (200 OK)
    /// ```json
    /// {
    ///   "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "email": "usuario@exemplo.com",
    ///   "name": "João Silva",
    ///   "createdAt": "2026-01-14T10:30:00Z"
    /// }
    /// ```
    /// </remarks>
    /// <response code="200">✅ Usuário encontrado com sucesso</response>
    /// <response code="401">🔒 Token JWT inválido ou ausente</response>
    /// <response code="404">❌ Usuário não encontrado</response>
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [Authorize] // 🔒 Endpoint protegido
    [ProducesResponseType(typeof(UserOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserOutput>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Buscando usuário com ID: {UserId}", id);

        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// 🔑 Autentica um usuário e retorna um token JWT
    /// </summary>
    /// <param name="command">Credenciais de login (email e senha)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Token JWT para autenticação nas próximas requisições</returns>
    /// <remarks>
    /// ## Exemplo de Requisição
    /// ```json
    /// {
    ///   "email": "usuario@exemplo.com",
    ///   "password": "SenhaSegura123!"
    /// }
    /// ```
    /// 
    /// ## Exemplo de Resposta (200 OK)
    /// ```json
    /// {
    ///   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///   "expiresIn": 3600,
    ///   "user": {
    ///     "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///     "email": "usuario@exemplo.com",
    ///     "name": "João Silva"
    ///   }
    /// }
    /// ```
    /// 
    /// ⚠️ **Importante**: Guarde o token retornado para usar nos próximos requests!
    /// </remarks>
    /// <response code="200">✅ Login realizado com sucesso</response>
    /// <response code="400">⚠️ Dados de entrada inválidos (validação falhou)</response>
    /// <response code="401">🔒 Credenciais inválidas (email ou senha incorretos)</response>
    /// <response code="429">⏱️ Muitas tentativas de login - aguarde alguns minutos</response>
    [HttpPost("login")]
    [AllowAnonymous] // 🔓 Endpoint público
    [ProducesResponseType(typeof(AuthTokenOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthTokenOutput>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando processo de login para email: {Email}", command.Email);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Login realizado com sucesso para email: {Email}", command.Email);

        return Ok(result);
    }

    /// <summary>
    /// 📝 Registra um novo usuário no sistema
    /// </summary>
    /// <param name="command">Dados do novo usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Usuário criado com sucesso</returns>
    /// <remarks>
    /// ## Exemplo de Requisição
    /// ```json
    /// {
    ///   "email": "novousuario@exemplo.com",
    ///   "password": "SenhaSegura123!",
    ///   "confirmPassword": "SenhaSegura123!",
    ///   "name": "Maria Santos"
    /// }
    /// ```
    /// 
    /// ## Exemplo de Resposta (201 Created)
    /// ```json
    /// {
    ///   "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    ///   "email": "novousuario@exemplo.com",
    ///   "name": "Maria Santos",
    ///   "createdAt": "2026-01-14T14:30:00Z"
    /// }
    /// ```
    /// 
    /// ## Regras de Validação
    /// - ✅ Email deve ser válido e único
    /// - ✅ Senha deve ter no mínimo 8 caracteres
    /// - ✅ Senha deve conter maiúsculas, minúsculas, números e caracteres especiais
    /// - ✅ Senha e confirmação devem ser idênticas
    /// </remarks>
    /// <response code="201">✅ Usuário criado com sucesso</response>
    /// <response code="400">⚠️ Dados de entrada inválidos</response>
    /// <response code="409">❌ Email já cadastrado no sistema</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserOutput), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserOutput>> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando processo de registro para email: {Email}", command.Email);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Usuário registrado com sucesso: {UserId}", result.Id);

        return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1.0" }, result);
    }
}
