using Product.Template.Core.Identity.Domain.Events;
using Product.Template.Core.Identity.Domain.ValueObjects;
using Product.Template.Kernel.Domain.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Identity.Domain.Entities;

public class User : AggregateRoot, IMultiTenantEntity
{
    public long TenantId { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public bool IsActive { get; private set; }
    public string SecurityStamp { get; private set; }

    private User() { Email = null!; PasswordHash = null!; FirstName = null!; LastName = null!; SecurityStamp = null!; }

    private User(Guid id, long tenantId, Email email, string passwordHash, string firstName, string lastName)
    {
        Id = id;
        SetTenant(tenantId);
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        EmailConfirmed = false;
        IsActive = true;
        SecurityStamp = Guid.NewGuid().ToString("N");
        CreatedAt = DateTime.UtcNow;
    }

    public static User Create(
        long tenantId,
        string email,
        string passwordHash,
        string firstName,
        string lastName)
    {
        var emailVo = Email.Create(email);
        var user = new User(
            Guid.NewGuid(),
            tenantId,
            emailVo,
            passwordHash,
            firstName,
            lastName);

        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email.Value));

        return user;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        AddDomainEvent(new UserLoggedInEvent(Id, Email.Value));
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void RegenerateSecurityStamp()
    {
        SecurityStamp = Guid.NewGuid().ToString("N");
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    private void SetTenant(long tenantId)
    {
        if (tenantId <= 0)
            throw new DomainException("TenantId must be provided for multi-tenant entities.");
        if (TenantId != 0 && TenantId != tenantId)
            throw new DomainException("TenantId cannot be changed once set.");
        TenantId = tenantId;
    }

    void IMultiTenantEntity.AssignTenant(long tenantId) => SetTenant(tenantId);
}
