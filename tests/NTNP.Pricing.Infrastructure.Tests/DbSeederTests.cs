using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Infrastructure.Identity;
using NTNP.Pricing.Infrastructure.Persistence;
using NTNP.Pricing.Infrastructure.Seed;

namespace NTNP.Pricing.Infrastructure.Tests;

public class DbSeederTests : IClassFixture<SeededServiceProviderFixture>
{
    private readonly SeededServiceProviderFixture _fixture;

    public DbSeederTests(SeededServiceProviderFixture fixture)
    {
        _fixture = fixture;
    }

    private ApplicationDbContext CreateContext() =>
        _fixture.Provider.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>();

    [Fact]
    public async Task Seeds_All_Five_Roles()
    {
        await using var db = CreateContext();
        var roleNames = await db.Roles.Select(r => r.Name).ToListAsync();

        foreach (var role in Roles.All)
            Assert.Contains(role, roleNames);
    }

    [Fact]
    public async Task Seeds_Development_Admin_User_In_Admin_Role()
    {
        await using var db = CreateContext();
        var admin = await db.Users.SingleAsync(u => u.Email == IdentitySeeder.DevAdminEmail);
        var adminRole = await db.Roles.SingleAsync(r => r.Name == Roles.Admin);
        var isInRole = await db.UserRoles.AnyAsync(ur => ur.UserId == admin.Id && ur.RoleId == adminRole.Id);

        Assert.True(isInRole);
    }

    [Fact]
    public async Task Seeds_Five_Currencies_Including_IRR_EUR_USD_CNY_AED()
    {
        await using var db = CreateContext();
        var codes = await db.Currencies.Select(c => c.Code).ToListAsync();

        Assert.Equal(5, codes.Count);
        foreach (var expected in new[] { "IRR", "EUR", "USD", "CNY", "AED" })
            Assert.Contains(expected, codes);
    }

    [Fact]
    public async Task Seeds_Default_85_15_Eur_Pricing_Profile()
    {
        await using var db = CreateContext();
        var profile = await db.PricingProfiles.SingleAsync(p => p.Name == "Default 85/15 EUR Profile");

        Assert.Equal(PricingMethod.Markup, profile.PricingMethod);
        Assert.Equal(0.30m, profile.DefaultRate);
        Assert.Equal(0.85m, profile.DefaultForeignShare);
        Assert.Equal(0.15m, profile.DefaultRialShare);
        Assert.Equal("EUR", profile.DefaultQuotationCurrencyCode);
    }

    [Fact]
    public async Task Seeds_All_Seven_PanelTypes_And_Three_ProductFamilies()
    {
        await using var db = CreateContext();

        Assert.Equal(7, await db.PanelTypes.CountAsync());
        Assert.Equal(3, await db.ProductFamilies.CountAsync());
        Assert.True(await db.ProductFamilies.AnyAsync(f => f.Name == "UniSafe"));
        Assert.True(await db.ProductFamilies.AnyAsync(f => f.Name == "UniGear ZS3.2"));
        Assert.True(await db.ProductFamilies.AnyAsync(f => f.Name == "SIVACON 8PT"));
    }

    [Fact]
    public async Task Seeded_SampleProject_Reproduces_Section20_Totals_Exactly()
    {
        await using var db = CreateContext();

        var project = await db.Projects.SingleAsync(p => p.ProjectCode == "PRJ-0001");
        var revision = await db.ProjectRevisions
            .Include(r => r.Lines).ThenInclude(l => l.BomItems)
            .SingleAsync(r => r.ProjectId == project.Id);

        Assert.Equal(3_030_000_000m, revision.TotalProjectCostIrr);
        Assert.Equal(3_939_000_000m, revision.TotalProjectSellingPriceIrr);
        Assert.Equal(590_850_000m, revision.TotalRialPayable);
        Assert.Equal(1_860.083333m, Math.Round(revision.TotalForeignPayable, 6));
        Assert.Equal(909_000_000m, revision.ProjectProfitIrr);
        Assert.True(revision.ReconciliationPassed);
    }

    [Fact]
    public async Task Seeding_Twice_Is_Idempotent()
    {
        // The fixture already seeded once during InitializeAsync(); seeding again must not throw
        // or duplicate rows (Section 37: "safe to call on every application start").
        await DbSeeder.SeedAsync(_fixture.Provider);

        await using var db = CreateContext();
        Assert.Equal(1, await db.Projects.CountAsync());
        Assert.Equal(5, await db.Currencies.CountAsync());
        Assert.Equal(1, await db.Customers.CountAsync());
    }
}
