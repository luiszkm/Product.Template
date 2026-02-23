using System.Diagnostics.Metrics;

namespace Product.Template.Core.Identity.Application.Security;

public static class RbacMetrics
{
    private static readonly Meter Meter = new("Product.Template.RBAC", "1.0.0");

    public static readonly Counter<long> RoleAssignments = Meter.CreateCounter<long>(
        "rbac_role_assignments_total",
        description: "Total number of role assignment operations.");

    public static readonly Counter<long> RoleRevocations = Meter.CreateCounter<long>(
        "rbac_role_revocations_total",
        description: "Total number of role revocation operations.");

    public static readonly Counter<long> RoleChangesDenied = Meter.CreateCounter<long>(
        "rbac_role_changes_denied_total",
        description: "Total number of denied role management operations.");
}
