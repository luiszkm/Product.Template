using Microsoft.FeatureManagement;

namespace Product.Template.Api.Attributes;

/// <summary>
/// Atributo para habilitar/desabilitar endpoints baseado em Feature Flags
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class FeatureGateAttribute : Attribute
{
    public string FeatureName { get; }

    public FeatureGateAttribute(string featureName)
    {
        FeatureName = featureName;
    }
}

/// <summary>
/// Feature Flags disponíveis na aplicação
/// </summary>
public static class FeatureFlags
{
    public const string EnableCaching = "EnableCaching";
    public const string EnableAuditTrail = "EnableAuditTrail";
    public const string EnableRequestDeduplication = "EnableRequestDeduplication";
    public const string EnableAdvancedLogging = "EnableAdvancedLogging";
    public const string EnableExperimentalFeatures = "EnableExperimentalFeatures";
}

