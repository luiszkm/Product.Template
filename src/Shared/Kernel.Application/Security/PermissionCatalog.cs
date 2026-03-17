using System.Collections.ObjectModel;

namespace Product.Template.Kernel.Application.Security;

public sealed class PermissionCatalog : IPermissionCatalog
{
    private readonly Dictionary<string, PermissionDescriptor> _permissions = new();
    private readonly object _sync = new();

    public void Register(IEnumerable<PermissionDescriptor> permissions)
    {
        foreach (var permission in permissions)
        {
            Register(permission);
        }
    }

    public void Register(params PermissionDescriptor[] permissions)
        => Register((IEnumerable<PermissionDescriptor>)permissions);

    public IReadOnlyCollection<PermissionDescriptor> GetAll()
    {
        lock (_sync)
        {
            return new ReadOnlyCollection<PermissionDescriptor>(_permissions.Values.ToList());
        }
    }

    public bool Contains(string code)
    {
        lock (_sync)
        {
            return _permissions.ContainsKey(Normalize(code));
        }
    }

    public bool TryGet(string code, out PermissionDescriptor? descriptor)
    {
        lock (_sync)
        {
            return _permissions.TryGetValue(Normalize(code), out descriptor);
        }
    }

    private void Register(PermissionDescriptor descriptor)
    {
        var code = Normalize(descriptor.Code);
        lock (_sync)
        {
            _permissions[code] = descriptor;
        }
    }

    private static string Normalize(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Permission code cannot be null or empty.", nameof(code));

        return code.Trim().ToLowerInvariant();
    }
}



