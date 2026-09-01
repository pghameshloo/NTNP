using Microsoft.AspNetCore.Identity;

namespace NTNP.Pricing.Infrastructure.Identity;

/// <summary>Section 6 — the five roles are seeded from <c>NTNP.Pricing.Domain.Enums.Roles</c>.</summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }

    public string? Description { get; set; }
}
