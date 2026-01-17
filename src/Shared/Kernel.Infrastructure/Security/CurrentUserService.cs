using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Product.Template.Kernel.Application.Security;

namespace Kernel.Infrastructure.Security;

/// <summary>
/// Implementação do serviço de usuário atual
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return null;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User
        .FindFirst(ClaimTypes.Email)?.Value;

    public string? UserName => _httpContextAccessor.HttpContext?.User
        .FindFirst(ClaimTypes.Name)?.Value ?? Email;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<Claim> Claims => _httpContextAccessor.HttpContext?.User?.Claims ?? Enumerable.Empty<Claim>();
}

