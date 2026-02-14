using Microsoft.AspNetCore.HttpOverrides;
using Product.Template.Api.Configurations;
using Product.Template.Api.Middleware;
using Product.Template.Core.Identity.Infrastructure.Data;
using Product.Template.Kernel.Infrastructure.MultiTenancy;

var builder = WebApplication.CreateBuilder(args);
builder.AddSerilogConfiguration();

// Core Application Services (CQRS, Behaviors, Handlers)
builder.Services.AddApplicationCore(builder.Configuration);

// Use Cases and Repositories
builder.Services.AddUseCases();

// Database Configuration (InMemory)
builder.Services.AddDatabaseConfiguration(builder.Configuration);

// Database Connections
builder.Services.AddAppConnections(builder.Configuration);

// API Versioning
builder.Services.AddApiVersioningConfiguration();

// Controllers and API Documentation
builder.Services.AddControllersConfigurations();

// Response Compression (Brotli + Gzip)
builder.Services.AddCompressionConfiguration();

// Output Caching
builder.Services.AddCachingConfiguration(builder.Configuration);

// Feature Flags
builder.Services.AddFeatureFlagsConfiguration(builder.Configuration);

// Resilience Policies (Retry, Circuit Breaker, Timeout)
builder.Services.AddResiliencePolicies(builder.Configuration);

// Rate Limiting
builder.Services.AddRateLimiting(builder.Configuration);

// Health Checks
builder.Services.AddHealthChecksConfiguration();

// Security (CORS, JWT Authentication)
builder.Services.AddSecurityConfiguration(builder.Configuration, builder.Environment);

// OpenTelemetry (Traces e Metrics)
builder.Services.AddOpenTelemetryConfiguration(builder.Configuration);

var app = builder.Build();

// Initialize Database with Seeders
await app.Services.InitializeDatabaseAsync();

// Response Compression
app.UseResponseCompression();

// Output Caching
app.UseCachingConfiguration();

// Serilog Request Logging (captura todas as requisições de forma performática)
app.UseSerilogConfiguration();

// Request Logging Detalhado (com correlationId e mascaramento de dados sensíveis)
app.UseMiddleware<RequestLoggingMiddleware>();

// Request Deduplication (previne requisições duplicadas)
app.UseMiddleware<RequestDeduplicationMiddleware>();

// Tenant resolution (header/subdomain)
app.UseMiddleware<TenantResolutionMiddleware>();

// IP Whitelist/Blacklist Validation
app.UseMiddleware<IpWhitelistMiddleware>();

// Rate Limiting
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// HTTPS Redirection
app.UseHttpsRedirection();
app.UseRouting();

// Security (CORS, Authentication, Authorization)
app.UseSecurityConfiguration();

if (app.Configuration.GetValue<bool>("Jwt:Enabled"))
{
    app.UseAuthentication();
    app.UseAuthorization();
}
app.UseRateLimiting();

// Health Checks Endpoints
app.UseHealthChecksConfiguration();

// API Documentation (Swagger)
app.UseDocumentation();

// Controllers
app.MapControllers();

app.Run();
