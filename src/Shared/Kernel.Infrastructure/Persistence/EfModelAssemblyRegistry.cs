using System.Reflection;

namespace Product.Template.Kernel.Infrastructure.Persistence;

public sealed class EfModelAssemblyRegistry
{
    private readonly List<Assembly> _assemblies = new();

    public void Register(Assembly assembly)
    {
        if (!_assemblies.Contains(assembly))
            _assemblies.Add(assembly);
    }

    public IReadOnlyList<Assembly> Assemblies => _assemblies.AsReadOnly();
}
