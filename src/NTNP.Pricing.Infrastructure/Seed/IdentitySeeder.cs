using Microsoft.AspNetCore.Identity;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Infrastructure.Identity;

namespace NTNP.Pricing.Infrastructure.Seed;

/// <summary>
/// Section 37 — seeds the five roles and one development Admin user. Development-only credentials;
/// documented in README.md, never used for a production deployment (a production deployment instead
/// runs the "Initial Admin creation utility", see docs/deployment.md).
/// </summary>
public static class IdentitySeeder
{
    public const string DevAdminEmail = "admin@ntnp.local";
    public const string DevAdminPassword = "Ntnp!Admin123";

    public static async Task SeedAsync(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName) { Description = $"{roleName} role" });
            }
        }

        var existingAdmin = await userManager.FindByEmailAsync(DevAdminEmail);
        if (existingAdmin is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = DevAdminEmail,
            Email = DevAdminEmail,
            EmailConfirmed = true,
            DisplayName = "System Administrator",
            IsActive = true,
        };

        var result = await userManager.CreateAsync(admin, DevAdminPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Failed to seed the development Admin user: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(admin, Roles.Admin);
    }
}
