using Product.Template.Api.Configurations;
using Product.Template.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// 1. Services Configuration
// ========================================

// Core Application Services (CQRS, Behaviors, Handlers)
builder.Services.AddApplicationCore();

// Use Cases and Repositories
builder.Services.AddUseCases();

// Database Connections
builder.Services.AddAppConnections(builder.Configuration);

// Controllers and API Documentation
builder.Services.AddControllersConfigurations();

// Resilience Policies (Retry, Circuit Breaker, Timeout)
builder.Services.AddResiliencePolicies(builder.Configuration);

// Rate Limiting
builder.Services.AddRateLimiting(builder.Configuration);

// Health Checks
builder.Services.AddHealthChecksConfiguration();

// Security (CORS, JWT Authentication)
builder.Services.AddSecurityConfiguration(builder.Configuration);

var app = builder.Build();

// ========================================
// 2. Middleware Pipeline Configuration
// ========================================

// Request Logging (deve vir primeiro para capturar todas as requisições)
app.UseMiddleware<RequestLoggingMiddleware>();

// IP Whitelist/Blacklist Validation
app.UseMiddleware<IpWhitelistMiddleware>();

// Rate Limiting
app.UseRateLimiting();

// Health Checks Endpoints
app.UseHealthChecksConfiguration();

// API Documentation (Swagger)
app.UseDocumentation();

// Security (CORS, Authentication, Authorization)
app.UseSecurityConfiguration();

// HTTPS Redirection
app.UseHttpsRedirection();

// Controllers
app.MapControllers();

app.Run();
