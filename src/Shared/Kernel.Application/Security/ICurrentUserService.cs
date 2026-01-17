using System.Security.Claims;

namespace Product.Template.Kernel.Application.Security;

/// <summary>
/// Interface para obter o usuário atual autenticado
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// ID do usuário atual
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Email do usuário atual
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Nome do usuário atual
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Verifica se o usuário está autenticado
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Claims do usuário atual
    /// </summary>
    IEnumerable<Claim> Claims { get; }
}

