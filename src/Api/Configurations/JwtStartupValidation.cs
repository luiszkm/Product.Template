namespace Product.Template.Api.Configurations;

public static class JwtStartupValidation
{
    private const string PlaceholderSecret = "your-secret-key-min-32-characters-long-change-in-production-must-be-very-secure";

    public static void ValidateJwtConfiguration(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!configuration.GetValue<bool>("Jwt:Enabled", true))
            return;

        var secret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Jwt:Secret must be configured.");

        if (secret.Length < 32)
            throw new InvalidOperationException("Jwt:Secret must be at least 32 characters.");

        if (!environment.IsDevelopment() &&
            string.Equals(secret, PlaceholderSecret, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Jwt:Secret is still the template placeholder. Set a strong secret via environment or secrets manager.");
        }

        // ValidateIssuer/ValidateAudience are silently disabled when Issuer/Audience are unset
        // (see SecurityConfiguration.JwtOptions) — a token from any issuer/audience would then
        // validate. Fail fast outside Development instead of degrading validation silently.
        if (!environment.IsDevelopment())
        {
            if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]))
                throw new InvalidOperationException("Jwt:Issuer must be configured outside Development.");

            if (string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]))
                throw new InvalidOperationException("Jwt:Audience must be configured outside Development.");
        }
    }
}
