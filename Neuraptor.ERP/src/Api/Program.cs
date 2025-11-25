using Neuraptor.ERP.Api.Configurations;
using Neuraptor.ERP.Api.Middleware;

// ========================================
// 0. Configure Serilog (Before Building)
// ========================================
var builder = WebApplication.CreateBuilder(args);
builder.AddSerilogConfiguration();

// ========================================
// 1. Services Configuration
// ========================================

// Core Application Services (CQRS, Behaviors, Handlers)
builder.Services.AddApplicationCore();

// Use Cases and Repositories
builder.Services.AddUseCases();

// Database Connections
builder.Services.AddAppConnections(builder.Configuration);

// API Versioning
builder.Services.AddApiVersioningConfiguration();

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

// OpenTelemetry (Traces e Metrics)
builder.Services.AddOpenTelemetryConfiguration(builder.Configuration);

var app = builder.Build();

// ========================================
// 2. Middleware Pipeline Configuration
// ========================================

// Serilog Request Logging (captura todas as requisições de forma performática)
app.UseSerilogConfiguration();

// Request Logging Detalhado (com correlationId e mascaramento de dados sensíveis)
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
