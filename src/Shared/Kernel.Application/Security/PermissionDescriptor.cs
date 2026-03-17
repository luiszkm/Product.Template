namespace Product.Template.Kernel.Application.Security;

public sealed class PermissionDescriptor
{
    public PermissionDescriptor(
        string code,
        string module,
        string resource,
        string action,
        string description)
    {
        Code = Normalize(code);
        Module = Normalize(module);
        Resource = Normalize(resource);
        Action = Normalize(action);
        Description = description?.Trim() ?? string.Empty;
    }

    public string Code { get; }
    public string Module { get; }
    public string Resource { get; }
    public string Action { get; }
    public string Description { get; }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or empty.", nameof(value));

        return value.Trim().ToLowerInvariant();
    }
}


