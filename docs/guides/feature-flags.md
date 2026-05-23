# Feature flags — Product.Template API

Feature flags control optional behaviour at runtime via `FeatureFlags` in configuration or environment variables (`FeatureFlags__*`).

## Configuration

### appsettings.json

```json
"FeatureFlags": {
  "EnableCaching": true,
  "EnableAuditTrail": true,
  "EnableRequestDeduplication": true,
  "EnableAdvancedLogging": true,
  "EnableExperimentalFeatures": false,
  "EnableAI": false
}
```

### Environment variables

ASP.NET Core maps nested keys with double underscore:

| Config key | Environment variable | Default |
|------------|---------------------|---------|
| `FeatureFlags:EnableCaching` | `FeatureFlags__EnableCaching` | `true` |
| `FeatureFlags:EnableAuditTrail` | `FeatureFlags__EnableAuditTrail` | `true` |
| `FeatureFlags:EnableRequestDeduplication` | `FeatureFlags__EnableRequestDeduplication` | `true` |
| `FeatureFlags:EnableAdvancedLogging` | `FeatureFlags__EnableAdvancedLogging` | `true` |
| `FeatureFlags:EnableExperimentalFeatures` | `FeatureFlags__EnableExperimentalFeatures` | `false` |
| `FeatureFlags:EnableAI` | `FeatureFlags__EnableAI` | `false` |

Example:

```bash
export FeatureFlags__EnableAI=true
export FeatureFlags__EnableAuditTrail=false
```

Registration: `AddFeatureFlagsConfiguration` wires `Microsoft.FeatureManagement` from the `FeatureFlags` section (`src/Api/Configurations/FeatureFlagsConfiguration.cs`).

## Flag reference

| Flag | Default | Effect when disabled |
|------|---------|----------------------|
| `EnableCaching` | `true` | Output cache services and `UseOutputCache` middleware are skipped (`CachingConfiguration`, `Program.cs`). Also requires `Caching:Enabled=true`. |
| `EnableAuditTrail` | `true` | `AuditLogInterceptor` is not registered; entity changes are not written to `AuditLogs`. `AuditableEntityInterceptor` (Created/Updated metadata) still runs. |
| `EnableRequestDeduplication` | `true` | `RequestDeduplicationMiddleware` is not added to the pipeline. |
| `EnableAdvancedLogging` | `true` | `RequestLoggingMiddleware` (detailed request log with masked body) is not added. |
| `EnableExperimentalFeatures` | `false` | Reserved for endpoints marked with `[FeatureGate(FeatureFlags.EnableExperimentalFeatures)]`. |
| `EnableAI` | `false` | AI endpoints return 404; null AI service implementations are registered instead of Azure OpenAI clients. |

## `[FeatureGate]` on controllers

The `FeatureGateActionFilter` runs on every controller action. When the named flag is off, the response is **404** with a ProblemDetails body.

Constants live in `FeatureFlags` (`src/Api/Attributes/FeatureGateAttribute.cs`).

Current usage:

| Controller | Attribute | Flag |
|------------|-----------|------|
| `AiController` | `[FeatureGate(FeatureFlags.EnableAI)]` | `EnableAI` |

To gate a new endpoint, add `[FeatureGate(FeatureFlags.NomeDaFlag)]` on the controller or action and register the flag name in `appsettings.json`.

## Middleware gates in Program.cs

Some flags are enforced directly in the pipeline (not only via `[FeatureGate]`):

| Flag | Location | Behaviour |
|------|----------|-----------|
| `EnableCaching` | `Program.cs` | Calls `UseCachingConfiguration()` only when true |
| `EnableAdvancedLogging` | `Program.cs` | Adds `RequestLoggingMiddleware` when true |
| `EnableRequestDeduplication` | `Program.cs` | Adds `RequestDeduplicationMiddleware` when true |

## Service registration gates

| Flag | Location | Behaviour |
|------|----------|-----------|
| `EnableCaching` | `CachingConfiguration.AddCachingConfiguration` | Skips `AddOutputCache` when false or when `Caching:Enabled` is false |
| `EnableAuditTrail` | `DatabaseConfiguration`, `Kernel.Infrastructure.DependencyInjection` | Skips `AuditLogInterceptor` registration when false |
| `EnableAI` | `AiConfiguration.AddAiConfiguration` | Registers null AI services when false |

## Related configuration

| Key | Notes |
|-----|-------|
| `Caching:Enabled` | Works together with `EnableCaching`; both must be true for output caching |

## References

- [Deploy guide](./deploy-guide.md) — production env vars and checklist
- [AI integration guide](./ai-integration-guide.md) — `EnableAI` and Azure OpenAI setup
