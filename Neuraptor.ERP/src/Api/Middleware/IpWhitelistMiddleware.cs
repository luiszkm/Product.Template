using System.Net;

namespace Neuraptor.ERP.Api.Middleware;

public class IpWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpWhitelistMiddleware> _logger;
    private readonly HashSet<string> _allowedIPs;
    private readonly HashSet<string> _blockedIPs;
    private readonly bool _enableWhitelist;
    private readonly bool _enableBlacklist;

    public IpWhitelistMiddleware(
        RequestDelegate next,
        ILogger<IpWhitelistMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;

        _enableWhitelist = configuration.GetValue<bool>("IpSecurity:EnableWhitelist", false);
        _enableBlacklist = configuration.GetValue<bool>("IpSecurity:EnableBlacklist", false);

        var allowedIPs = configuration.GetSection("IpSecurity:AllowedIPs").Get<string[]>() ?? Array.Empty<string>();
        _allowedIPs = new HashSet<string>(allowedIPs, StringComparer.OrdinalIgnoreCase);

        var blockedIPs = configuration.GetSection("IpSecurity:BlockedIPs").Get<string[]>() ?? Array.Empty<string>();
        _blockedIPs = new HashSet<string>(blockedIPs, StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var remoteIp = GetRemoteIpAddress(context);

        if (remoteIp == null)
        {
            _logger.LogWarning("Unable to determine remote IP address");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Access denied",
                message = "Unable to determine IP address"
            });
            return;
        }

        // Verificar Blacklist primeiro (prioridade maior)
        if (_enableBlacklist && IsBlocked(remoteIp))
        {
            _logger.LogWarning("Request from blocked IP: {RemoteIp}", remoteIp);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Access denied",
                message = "Your IP address is blocked"
            });
            return;
        }

        // Verificar Whitelist
        if (_enableWhitelist && !IsAllowed(remoteIp))
        {
            _logger.LogWarning("Request from non-whitelisted IP: {RemoteIp}", remoteIp);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Access denied",
                message = "Your IP address is not whitelisted"
            });
            return;
        }

        await _next(context);
    }

    private string? GetRemoteIpAddress(HttpContext context)
    {
        // Tentar obter o IP de headers de proxy
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private bool IsAllowed(string ip)
    {
        // Sempre permitir localhost
        if (IsLocalhost(ip))
        {
            return true;
        }

        // Verificar se o IP está na whitelist
        if (_allowedIPs.Contains(ip))
        {
            return true;
        }

        // Verificar ranges CIDR (ex: 192.168.1.0/24)
        foreach (var allowedIp in _allowedIPs)
        {
            if (IsInCidrRange(ip, allowedIp))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsBlocked(string ip)
    {
        // Nunca bloquear localhost
        if (IsLocalhost(ip))
        {
            return false;
        }

        // Verificar se o IP está na blacklist
        if (_blockedIPs.Contains(ip))
        {
            return true;
        }

        // Verificar ranges CIDR
        foreach (var blockedIp in _blockedIPs)
        {
            if (IsInCidrRange(ip, blockedIp))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsLocalhost(string ip)
    {
        return ip == "127.0.0.1" ||
               ip == "::1" ||
               ip == "localhost" ||
               ip.StartsWith("127.") ||
               ip.StartsWith("::ffff:127.");
    }

    private bool IsInCidrRange(string ip, string cidr)
    {
        if (!cidr.Contains('/'))
        {
            return false;
        }

        try
        {
            var parts = cidr.Split('/');
            var baseAddress = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);

            var ipAddress = IPAddress.Parse(ip);

            // Converter para bytes
            var baseBytes = baseAddress.GetAddressBytes();
            var ipBytes = ipAddress.GetAddressBytes();

            if (baseBytes.Length != ipBytes.Length)
            {
                return false;
            }

            var maskBytes = GetMaskBytes(prefixLength, baseBytes.Length);

            for (int i = 0; i < baseBytes.Length; i++)
            {
                if ((baseBytes[i] & maskBytes[i]) != (ipBytes[i] & maskBytes[i]))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private byte[] GetMaskBytes(int prefixLength, int byteCount)
    {
        var mask = new byte[byteCount];
        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (int i = 0; i < fullBytes && i < byteCount; i++)
        {
            mask[i] = 0xFF;
        }

        if (fullBytes < byteCount && remainingBits > 0)
        {
            mask[fullBytes] = (byte)(0xFF << (8 - remainingBits));
        }

        return mask;
    }
}
