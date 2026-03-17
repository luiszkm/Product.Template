using Asp.Versioning;
using Kernel.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Product.Template.Api.Configurations;
using Product.Template.Core.Identity.Application.Handlers.Auth;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Core.Identity.Application.Queries.Users;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Domain.SeedWorks;


namespace Product.Template.Api.Controllers.v1;

/// <summary>
/// Identity API — User authentication and management
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Tags("Identity")]
public class IdentityController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<IdentityController> _logger;
    private readonly IAuthenticationProviderFactory _authProviderFactory;
    private readonly ICurrentUserService _currentUserService;

    public IdentityController(
        IMediator mediator,
        ILogger<IdentityController> logger,
        IAuthenticationProviderFactory authProviderFactory,
        ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _logger = logger;
        _authProviderFactory = authProviderFactory;
        _currentUserService = currentUserService;
    }

    [HttpGet("providers")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetAvailableProviders()
    {
        var providers = _authProviderFactory.GetAvailableProviders().ToList();
        return Ok(new { providers, count = providers.Count });
    }

    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [Authorize(Policy = SecurityConfiguration.UserReadOrSelfPolicy)]
    [ProducesResponseType(typeof(UserOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserOutput>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Buscando usuário com ID: {UserId}", id);
        var result = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/roles")]
    [Authorize(Policy = SecurityConfiguration.UsersReadPolicy)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<string>>> GetUserRoles(Guid id, CancellationToken cancellationToken)
    {
        var roles = await _mediator.Send(new GetUserRolesQuery(id), cancellationToken);
        return Ok(roles);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokenOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokenOutput>> Login(
        [FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando processo de login para email: {Email}", command.Email);
        var result = await _mediator.Send(command, cancellationToken);
        _logger.LogInformation("Login realizado com sucesso para email: {Email}", command.Email);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokenOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokenOutput>> Refresh(
        [FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refresh token solicitado");
        var result = await _mediator.Send(command, cancellationToken);
        _logger.LogInformation("Refresh token concedido para usuário: {UserId}", result.User.Id);
        return Ok(result);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserOutput), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserOutput>> Register(
        [FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando processo de registro para email: {Email}", command.Email);
        var result = await _mediator.Send(command, cancellationToken);
        _logger.LogInformation("Usuário registrado com sucesso: {UserId}", result.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpPost("external-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokenOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokenOutput>> ExternalLogin(
        [FromBody] ExternalLoginCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando autenticação externa com provider: {Provider}", command.Provider);
        var result = await _mediator.Send(command, cancellationToken);
        _logger.LogInformation("Autenticação externa bem-sucedida via provider: {Provider}", command.Provider);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = SecurityConfiguration.UsersReadPolicy)]
    [ProducesResponseType(typeof(PaginatedListOutput<UserOutput>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedListOutput<UserOutput>>> ListUsers(
        [FromQuery] ListUserQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listando usuários — página {PageNumber}, tamanho {PageSize}", query.PageNumber, query.PageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = SecurityConfiguration.UserManageOrSelfPolicy)]
    [ProducesResponseType(typeof(UserOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserOutput>> UpdateUser(
        Guid id, [FromBody] UpdateUserCommand command, CancellationToken cancellationToken)
    {
        if (id != command.UserId)
            return BadRequest("O ID da URL deve corresponder ao ID do usuário no corpo da requisição");

        _logger.LogInformation("Atualizando usuário com ID: {UserId}", id);
        var result = await _mediator.Send(command, cancellationToken);
        _logger.LogInformation("Usuário atualizado com sucesso: {UserId}", id);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = SecurityConfiguration.UsersManagePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deletando usuário com ID: {UserId}", id);
        await _mediator.Send(new DeleteUserCommand(id), cancellationToken);
        _logger.LogInformation("Usuário deletado com sucesso: {UserId}", id);
        return NoContent();
    }
}
