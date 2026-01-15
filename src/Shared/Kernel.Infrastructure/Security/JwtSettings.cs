namespace Kernel.Infrastructure.Security;

public class JwtSettings
{
    public bool Enabled { get; set; }
    public string Secret { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public int ExpirationMinutes { get; set; } = 60;
}
