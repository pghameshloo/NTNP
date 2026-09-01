using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Infrastructure.Identity;
using NTNP.Pricing.Infrastructure.Persistence;

namespace NTNP.Pricing.Infrastructure.Seed;

/// <summary>Single entry point called once at Api startup (Section 37). Fully idempotent.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await IdentitySeeder.SeedAsync(roleManager, userManager);

        if (!await db.CompanySettingsSet.AnyAsync(ct))
        {
            db.CompanySettingsSet.Add(new CompanySettings
            {
                PreparedByName = "Commercial Team",
                PreparedByPosition = "Sales Engineer",
                CommercialManagerName = "Commercial Manager",
                CommercialManagerPosition = "Commercial Manager",
                ManagingDirectorName = "Managing Director",
                ManagingDirectorPosition = "Managing Director",
            });
            await db.SaveChangesAsync(ct);
        }

        var masterData = await MasterDataSeeder.SeedAsync(db, ct);
        await SampleProjectSeeder.SeedAsync(db, masterData, ct);
    }
}
