using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Product.Template.Core.Identity.Application.Handlers.Auth;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Core.Identity.Application.Queries;
using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Core.Identity.Application.Queries.Users;

namespace Product.Template.Api.Controllers.v1;

/// <summary>
/// Identity API - Autenticação e Registro de Usuários
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
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
    /// Busca um usuário por ID
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <returns>Dados do usuário</returns>
    /// <response code="200">Usuário encontrado</response>
    /// <response code="404">Usuário não encontrado</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserOutput>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Buscando usuário com ID: {UserId}", id);

        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Autentica um usuário e retorna um token JWT
    /// </summary>
    /// <param name="command">Credenciais de login</param>
    /// <returns>Token de autenticação</returns>
    /// <response code="200">Login realizado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="401">Credenciais inválidas</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokenOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokenOutput>> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando processo de login para email: {Email}", command.Email);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Login realizado com sucesso para email: {Email}", command.Email);

        return Ok(result);
    }

    /// <summary>
    /// Registra um novo usuário no sistema
    /// </summary>
    /// <param name="command">Dados do novo usuário</param>
    /// <returns>Usuário criado</returns>
    /// <response code="201">Usuário criado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="409">Email já cadastrado</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserOutput), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserOutput>> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando processo de registro para email: {Email}", command.Email);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Usuário registrado com sucesso: {UserId}", result.Id);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}

