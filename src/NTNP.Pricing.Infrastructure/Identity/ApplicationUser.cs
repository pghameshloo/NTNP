using Microsoft.AspNetCore.Identity;

namespace NTNP.Pricing.Infrastructure.Identity;

/// <summary>
/// Section 6 — ASP.NET Core Identity user. Kept in Infrastructure (not Domain) so Domain stays
/// dependency-free; other layers reference users only by <c>Guid</c> id + denormalized display-name
/// snapshot (see <c>AuditableEntity</c>), never by a direct FK to this type.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAtUtc { get; set; }

    /// <summary>
    /// Section 6 — architecture reserved for future Active Directory / Windows-integrated auth
    /// (not mandatory for v1, see ASSUMPTIONS.md §7). Null for locally-managed accounts.
    /// </summary>
    public string? ActiveDirectorySid { get; set; }
}
