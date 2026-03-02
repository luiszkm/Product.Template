using Asp.Versioning;
using Kernel.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Product.Template.Api.Configurations;
using Product.Template.Core.Identity.Application.Handlers.Auth;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Core.Identity.Application.Handlers.Role.Commands;
using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Core.Identity.Application.Queries.Role;
using Product.Template.Core.Identity.Application.Queries.Users;
using Product.Template.Core.Identity.Application.Queries.Role.Commands;
using Product.Template.Kernel.Domain.SeedWorks;


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

    /// <summary>
    /// 🔌 Lista os provedores de autenticação disponíveis
    /// </summary>
    /// <returns>Lista de provedores ativos (jwt, microsoft, google, etc.)</returns>
    /// <remarks>
    /// ## Exemplo de Resposta (200 OK)
    /// ```json
    /// {
    ///   "providers": ["jwt", "microsoft"],
    ///   "count": 2
    /// }
    /// ```
    ///
    /// Use este endpoint para descobrir quais métodos de autenticação estão habilitados.
    /// </remarks>
    /// <response code="200">✅ Lista de provedores retornada</response>
    [HttpGet("providers")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetAvailableProviders()
    {
        var providers = _authProviderFactory.GetAvailableProviders().ToList();

        return Ok(new
        {
            providers,
            count = providers.Count
        });
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
    [Authorize(Policy = SecurityConfiguration.UserOnlyPolicy)] // 🔒 Endpoint protegido com RBAC
    [ProducesResponseType(typeof(UserOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserOutput>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!CanAccessUser(id))
        {
            _logger.LogWarning("Acesso negado ao usuário {CurrentUserId} para leitura do usuário {TargetUserId}", _currentUserService.UserId, id);
            return Forbid();
        }

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

        /// <summary>
        /// 🌐 Autenticação via provedor externo (Microsoft, Google, etc.)
        /// </summary>
        /// <param name="command">Dados de autenticação externa</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Token JWT para autenticação</returns>
        /// <remarks>
        /// ## Provedores Suportados
        /// - **microsoft**: Microsoft / Azure AD / Entra ID
        /// - **google**: Google OAuth (em desenvolvimento)
        ///
        /// ## Fluxo de Autenticação Microsoft
        /// 1. Redirecione o usuário para a URL de autorização do Azure AD:
        ///    ```
        ///    https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize
        ///      ?client_id={clientId}
        ///      &response_type=code
        ///      &redirect_uri={redirectUri}
        ///      &scope=openid%20profile%20email
        ///    ```
        /// 2. Após aprovação, o usuário é redirecionado de volta com um `code`
        /// 3. Envie o `code` para este endpoint junto com o `provider=microsoft`
        /// 4. Receba o token JWT para usar na API
        ///
        /// ## Exemplo de Requisição
        /// ```json
        /// {
        ///   "provider": "microsoft",
        ///   "code": "0.AX0A...",
        ///   "redirectUri": "https://localhost:7254/api/v1/identity/external-callback"
        /// }
        /// ```
        ///
        /// ## Exemplo de Resposta (200 OK)
        /// ```json
        /// {
        ///   "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ///   "tokenType": "Bearer",
        ///   "expiresIn": 3600,
        ///   "user": {
        ///     "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///     "email": "usuario@outlook.com",
        ///     "firstName": "João",
        ///     "roles": ["User"]
        ///   }
        /// }
        /// ```
        ///
        /// ⚠️ **Importante**: 
        /// - Configure as credenciais do Azure AD em `appsettings.json` → `MicrosoftAuth`
        /// - Use User Secrets em desenvolvimento para armazenar ClientSecret
        /// - O email do Microsoft deve ser verificado
        /// </remarks>
        /// <response code="200">✅ Autenticação externa bem-sucedida</response>
        /// <response code="400">⚠️ Dados de entrada inválidos ou provider não suportado</response>
        /// <response code="401">🔒 Falha na autenticação com o provedor externo</response>
        [HttpPost("external-login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthTokenOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthTokenOutput>> ExternalLogin(
            [FromBody] ExternalLoginCommand command,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Iniciando autenticação externa com provider: {Provider}",
                command.Provider);

            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation(
                "Autenticação externa bem-sucedida via provider: {Provider}",
                command.Provider);

            return Ok(result);
        }

        /// <summary>
        /// 📋 Lista todos os usuários com paginação
        /// </summary>
    /// <param name="pageNumber">Número da página (inicia em 1)</param>
    /// <param name="pageSize">Quantidade de itens por página (padrão: 10)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista paginada de usuários</returns>
    /// <remarks>
    /// ## Exemplo de Requisição
    /// ```http
    /// GET /api/v1/identity?pageNumber=1&amp;pageSize=10
    /// Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
    /// ```
    ///
    /// ## Exemplo de Resposta (200 OK)
    /// ```json
    /// {
    ///   "pageNumber": 1,
    ///   "pageSize": 10,
    ///   "totalCount": 45,
    ///   "data": [
    ///     {
    ///       "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "email": "usuario1@exemplo.com",
    ///       "name": "João Silva",
    ///       "createdAt": "2026-01-14T10:30:00Z"
    ///     },
    ///     {
    ///       "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    ///       "email": "usuario2@exemplo.com",
    ///       "name": "Maria Santos",
    ///       "createdAt": "2026-01-14T14:30:00Z"
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    /// <response code="200">✅ Lista de usuários retornada com sucesso</response>
    [HttpGet]
    [Authorize(Policy = SecurityConfiguration.UsersReadPolicy)]
    [ProducesResponseType(typeof(PaginatedListOutput<UserOutput>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedListOutput<UserOutput>>> ListUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listando usuários - Página: {PageNumber}, Tamanho: {PageSize}", pageNumber, pageSize);

        var query = new ListUserQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// ✏️ Atualiza os dados de um usuário existente
    /// </summary>
    /// <param name="id">ID único do usuário (GUID)</param>
    /// <param name="command">Dados atualizados do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Usuário atualizado</returns>
    /// <remarks>
    /// ## Exemplo de Requisição
    /// ```http
    /// PUT /api/v1/identity/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
    /// Content-Type: application/json
    ///
    /// {
    ///   "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    /// }
    /// ```
    ///
    /// ## Exemplo de Resposta (200 OK)
    /// ```json
    /// {
    ///   "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "email": "usuario@exemplo.com",
    ///   "name": "João Silva Atualizado",
    ///   "createdAt": "2026-01-14T10:30:00Z"
    /// }
    /// ```
    /// </remarks>
    /// <response code="200">✅ Usuário atualizado com sucesso</response>
    /// <response code="400">⚠️ Dados de entrada inválidos</response>
    /// <response code="401">🔒 Token JWT inválido ou ausente</response>
    /// <response code="404">❌ Usuário não encontrado</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = SecurityConfiguration.UserOnlyPolicy)]
    [ProducesResponseType(typeof(UserOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserOutput>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.UserId)
        {
            _logger.LogWarning("ID da URL ({UrlId}) não corresponde ao ID do comando ({CommandId})", id, command.UserId);
            return BadRequest("O ID da URL deve corresponder ao ID do usuário no corpo da requisição");
        }

        if (!CanAccessUser(id))
        {
            _logger.LogWarning("Acesso negado ao usuário {CurrentUserId} para atualização do usuário {TargetUserId}", _currentUserService.UserId, id);
            return Forbid();
        }

        _logger.LogInformation("Atualizando usuário com ID: {UserId}", id);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Usuário atualizado com sucesso: {UserId}", id);

        return Ok(result);
    }


    /// <summary>
    /// 🧩 Lista roles cadastradas
    /// </summary>
    [HttpGet("roles")]
    [Authorize(Policy = SecurityConfiguration.UsersReadPolicy)]
    [ProducesResponseType(typeof(PaginatedListOutput<RoleOutput>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedListOutput<RoleOutput>>> ListRoles(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new ListRolesQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 🔎 Busca role por id
    /// </summary>
    [HttpGet("roles/{roleId:guid}")]
    [Authorize(Policy = SecurityConfiguration.UsersReadPolicy)]
    [ProducesResponseType(typeof(RoleOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleOutput>> GetRoleById(Guid roleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery(roleId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// ➕ Cria nova role
    /// </summary>
    [HttpPost("roles")]
    [Authorize(Policy = SecurityConfiguration.UsersManagePolicy)]
    [ProducesResponseType(typeof(RoleOutput), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RoleOutput>> CreateRole([FromBody] CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetRoleById), new { roleId = result.Id, version = "1" }, result);
    }

    /// <summary>
    /// ✏️ Atualiza role
    /// </summary>
    [HttpPut("roles/{roleId:guid}")]
    [Authorize(Policy = SecurityConfiguration.UsersManagePolicy)]
    [ProducesResponseType(typeof(RoleOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleOutput>> UpdateRole(Guid roleId, [FromBody] UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        if (roleId != command.RoleId)
            return BadRequest("O ID da URL deve corresponder ao ID da role no corpo da requisição");

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 🗑️ Remove role
    /// </summary>
    [HttpDelete("roles/{roleId:guid}")]
    [Authorize(Policy = SecurityConfiguration.UsersManagePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(Guid roleId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteRoleCommand(roleId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// 🔐 Lista os papéis (roles) de um usuário
    /// </summary>
    [HttpGet("{id:guid}/roles")]
    [Authorize(Policy = SecurityConfiguration.UsersManagePolicy)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<string>>> GetUserRoles(Guid id, CancellationToken cancellationToken)
    {
        var roles = await _mediator.Send(new GetUserRolesQuery(id), cancellationToken);
        return Ok(roles);
    }

    /// <summary>
    /// ➕ Adiciona um papel (role) a um usuário
    /// </summary>
    [HttpPost("{id:guid}/roles")]
    [Authorize(Policy = SecurityConfiguration.UsersManagePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddUserRole(Guid id, [FromBody] ManageUserRoleRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName))
            return BadRequest("RoleName is required.");

        await _mediator.Send(new AddUserRoleCommand(id, request.RoleName), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// ➖ Remove um papel (role) de um usuário
    /// </summary>
    [HttpDelete("{id:guid}/roles/{roleName}")]
    [Authorize(Policy = SecurityConfiguration.UsersManagePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveUserRole(Guid id, string roleName, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveUserRoleCommand(id, roleName), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// 🗑️ Deleta um usuário do sistema
    /// </summary>
    /// <param name="id">ID único do usuário (GUID)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Confirmação de exclusão</returns>
    /// <remarks>
    /// ## Exemplo de Requisição
    /// ```http
    /// DELETE /api/v1/identity/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
    /// ```
    ///
    /// ## Resposta (204 No Content)
    /// Sem corpo na resposta - exclusão confirmada pelo status code
    ///
    /// ⚠️ **Atenção**: Esta ação é irreversível!
    /// </remarks>
    /// <response code="204">✅ Usuário deletado com sucesso</response>
    /// <response code="401">🔒 Token JWT inválido ou ausente</response>
    /// <response code="404">❌ Usuário não encontrado</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = SecurityConfiguration.UsersManagePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deletando usuário com ID: {UserId}", id);

        var command = new DeleteUserCommand(id);
        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Usuário deletado com sucesso: {UserId}", id);

        return NoContent();
    }
    public sealed record ManageUserRoleRequest(string RoleName);

    private bool CanAccessUser(Guid targetUserId)
    {
        if (IsAdmin())
            return true;

        return _currentUserService.UserId == targetUserId;
    }

    private bool IsAdmin()
    {
        return _currentUserService.Claims.Any(c =>
            c.Type == System.Security.Claims.ClaimTypes.Role &&
            string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase));
    }

}
