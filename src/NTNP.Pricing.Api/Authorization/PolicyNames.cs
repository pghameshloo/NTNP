namespace NTNP.Pricing.Api.Authorization;

/// <summary>
/// Section 6 — every API endpoint enforces its own authorization policy server-side ("Hiding
/// buttons is not authorization"). Policy names map to the responsibilities table in Section 6.
/// </summary>
public static class PolicyNames
{
    public const string AdminOnly = "AdminOnly";
    public const string ManageUsers = "ManageUsers";
    public const string ManageCurrencies = "ManageCurrencies";
    public const string ManageEquipment = "ManageEquipment";
    public const string ManageTemplates = "ManageTemplates"; // Panel templates + BODY+ES (Admin + Engineering)
    public const string ManageCustomers = "ManageCustomers"; // Admin + Commercial
    public const string ManageProjects = "ManageProjects"; // create/edit projects+lineup (Admin + Commercial)
    public const string ManagePricingProfiles = "ManagePricingProfiles"; // Admin
    public const string Approve = "Approve"; // Admin + Approver
    public const string ViewAuditLog = "ViewAuditLog"; // Admin
    public const string ManageSettings = "ManageSettings"; // Admin
    public const string ViewOnly = "ViewOnly"; // any authenticated role — read access
}
