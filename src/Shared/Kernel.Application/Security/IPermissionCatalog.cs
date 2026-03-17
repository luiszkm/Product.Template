namespace Product.Template.Kernel.Application.Security;

public interface IPermissionCatalog
{
    void Register(IEnumerable<PermissionDescriptor> permissions);
    void Register(params PermissionDescriptor[] permissions);
    IReadOnlyCollection<PermissionDescriptor> GetAll();
    bool Contains(string code);
    bool TryGet(string code, out PermissionDescriptor? descriptor);
}

