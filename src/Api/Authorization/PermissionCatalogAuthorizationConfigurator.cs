using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Product.Template.Api.Configurations;
using Product.Template.Core.Authorization.Application.Permissions;
using Product.Template.Core.Identity.Application.Permissions;
using Product.Template.Core.Tenants.Application.Permissions;
using Product.Template.Kernel.Application.Security;

namespace Product.Template.Api.Authorization;

internal sealed class PermissionCatalogAuthorizationConfigurator : IConfigureOptions<AuthorizationOptions>
{
    private readonly IPermissionCatalog _catalog;

    public PermissionCatalogAuthorizationConfigurator(IPermissionCatalog catalog)
    {
        _catalog = catalog;
    }

    public void Configure(AuthorizationOptions options)
    {
        ConfigureBasePolicies(options);
        ConfigurePermissionPolicies(options);
    }

    private void ConfigureBasePolicies(AuthorizationOptions options)
    {
        options.AddPolicy(SecurityConfiguration.AuthenticatedPolicy, policy => policy.RequireAuthenticatedUser());
        options.AddPolicy(SecurityConfiguration.AdminOnlyPolicy, policy => policy.RequireRole("Admin"));
        options.AddPolicy(SecurityConfiguration.UserOnlyPolicy, policy => policy.RequireRole("User", "Admin", "Manager"));
    }

    private void ConfigurePermissionPolicies(AuthorizationOptions options)
    {
        // Identity
        EnsurePermissionRegistered(IdentityPermissions.UserRead);
        EnsurePermissionRegistered(IdentityPermissions.UserManage);

        options.AddPolicy(SecurityConfiguration.UsersReadPolicy, policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("Admin") ||
                context.User.HasClaim(SecurityConfiguration.PermissionClaimType, IdentityPermissions.UserRead)));

        options.AddPolicy(SecurityConfiguration.UsersManagePolicy, policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("Admin") ||
                context.User.HasClaim(SecurityConfiguration.PermissionClaimType, IdentityPermissions.UserManage)));

        options.AddPolicy(SecurityConfiguration.UserReadOrSelfPolicy, policy =>
            policy.AddRequirements(new UserOwnershipRequirement(IdentityPermissions.UserRead)));

        options.AddPolicy(SecurityConfiguration.UserManageOrSelfPolicy, policy =>
            policy.AddRequirements(new UserOwnershipRequirement(IdentityPermissions.UserManage)));

        // Authorization module
        EnsurePermissionRegistered(AuthorizationPermissions.RoleRead);
        EnsurePermissionRegistered(AuthorizationPermissions.RoleManage);
        EnsurePermissionRegistered(AuthorizationPermissions.PermissionRead);
        EnsurePermissionRegistered(AuthorizationPermissions.PermissionManage);

        options.AddPolicy(SecurityConfiguration.AuthorizationRolesReadPolicy, policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("Admin") ||
                context.User.HasClaim(SecurityConfiguration.PermissionClaimType, AuthorizationPermissions.RoleRead)));

        options.AddPolicy(SecurityConfiguration.AuthorizationRolesManagePolicy, policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("Admin") ||
                context.User.HasClaim(SecurityConfiguration.PermissionClaimType, AuthorizationPermissions.RoleManage)));

        options.AddPolicy(SecurityConfiguration.AuthorizationPermissionsReadPolicy, policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("Admin") ||
                context.User.HasClaim(SecurityConfiguration.PermissionClaimType, AuthorizationPermissions.PermissionRead)));

        options.AddPolicy(SecurityConfiguration.AuthorizationPermissionsManagePolicy, policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("Admin") ||
                context.User.HasClaim(SecurityConfiguration.PermissionClaimType, AuthorizationPermissions.PermissionManage)));

        // Tenants module
        EnsurePermissionRegistered(TenantsPermissions.Read);
        EnsurePermissionRegistered(TenantsPermissions.Manage);

        options.AddPolicy(SecurityConfiguration.TenantsReadPolicy, policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("Admin") ||
                context.User.HasClaim(SecurityConfiguration.PermissionClaimType, TenantsPermissions.Read)));

        options.AddPolicy(SecurityConfiguration.TenantsManagePolicy, policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("Admin") ||
                context.User.HasClaim(SecurityConfiguration.PermissionClaimType, TenantsPermissions.Manage)));
    }

    private void EnsurePermissionRegistered(string permissionCode)
    {
        if (!_catalog.Contains(permissionCode))
        {
            throw new InvalidOperationException(
                $"Permission '{permissionCode}' must be registered in the catalog before configuring authorization policies.");
        }
    }
}
