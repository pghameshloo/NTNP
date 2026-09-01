namespace NTNP.Pricing.Desktop.Services;

/// <summary>
/// Mirrors the five role name strings from <c>NTNP.Pricing.Domain.Enums.Roles</c> (Section 6). The
/// Desktop project deliberately does not reference the Domain project (Section 33 — the desktop
/// client only ever talks to the server through Contracts DTOs over HTTP); these are just the
/// literal identity-role names the server issues as JWT role claims and returns from
/// <c>GET /api/roles</c>.
/// </summary>
public static class DesktopRoles
{
    public const string Admin = "Admin";
    public const string Engineering = "Engineering";
    public const string Commercial = "Commercial";
    public const string Approver = "Approver";
    public const string Viewer = "Viewer";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Engineering, Commercial, Approver, Viewer };
}
