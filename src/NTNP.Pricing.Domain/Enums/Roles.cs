namespace NTNP.Pricing.Domain.Enums;

/// <summary>
/// The five roles from Section 6. Plain string constants (not a CLR enum) because ASP.NET Core
/// Identity roles are string-keyed; kept in Domain so Application-layer authorization policies and
/// Infrastructure Identity seeding share one source of truth without Domain depending on Identity.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Engineering = "Engineering";
    public const string Commercial = "Commercial";
    public const string Approver = "Approver";
    public const string Viewer = "Viewer";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Engineering, Commercial, Approver, Viewer };
}
