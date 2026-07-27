namespace Product.Template.Api.Http;

public static class ClientIpResolver
{
    /// <summary>
    /// Returns the resolved client IP. Relies on <c>UseForwardedHeaders</c> (configured in Program.cs)
    /// having already rewritten <see cref="HttpContext.Connection"/>.RemoteIpAddress from
    /// X-Forwarded-For/X-Real-IP — and only for requests whose immediate peer is a trusted
    /// proxy (KnownProxies/KnownNetworks). Reading those headers directly here would let any
    /// caller spoof its IP and bypass the whitelist/blacklist.
    /// </summary>
    public static string? GetClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString();
}
