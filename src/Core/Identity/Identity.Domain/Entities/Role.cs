using Product.Template.Kernel.Domain.SeedWorks;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Core.Identity.Domain.Entities;

public class Role : Entity, IMultiTenantEntity
{
    public long TenantId { get; set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<UserRole> _userRoles = new();
    private readonly List<RolePermission> _rolePermissions = new();

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Role() { }

    private Role(Guid id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }

    public static Role Create(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty", nameof(name));

        return new Role(
            Guid.NewGuid(),
            name,
            description ?? string.Empty);
    }

    public void UpdateDescription(string description)
    {
        Description = description ?? string.Empty;
    }

    public void Update(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty", nameof(name));

        Name = name.Trim();
        Description = description ?? string.Empty;
    }
}
