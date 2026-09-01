using NTNP.Pricing.Application.PricingProfiles;
using NTNP.Pricing.Application.Tests.TestDoubles;
using NTNP.Pricing.Contracts.PricingProfiles;
using NTNP.Pricing.Domain.Exceptions;
using NTNP.Pricing.Infrastructure.Persistence;

namespace NTNP.Pricing.Application.Tests;

public class PricingProfileServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();
    private readonly PricingProfileService _service;

    public PricingProfileServiceTests()
    {
        _service = new PricingProfileService(_db, new FakeCurrentUserService(), new FakeDateTimeProvider(), new NoOpAuditService());
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateAsync_Seeds_Default_85_15_Eur_Profile_Correctly()
    {
        var dto = await _service.CreateAsync(new UpsertPricingProfileRequest(
            "Default 85/15 EUR Profile", "Markup", 0.30m, 0.15m, 0.85m, "EUR", "NearestThousand", "NearestInteger", 2, 1m, null));

        Assert.Equal(1.30m, dto.EquivalentMultiplier);
    }

    [Fact]
    public async Task CreateAsync_Rejects_GrossMargin_100Percent_Or_More()
    {
        await Assert.ThrowsAsync<DomainValidationException>(() => _service.CreateAsync(new UpsertPricingProfileRequest(
            "Invalid Profile", "GrossMargin", 1.0m, 0.15m, 0.85m, "EUR", "None", "None", 2, 1m, null)));
    }

    [Fact]
    public async Task CreateAsync_Rejects_Shares_Not_Totalling_100Percent()
    {
        await Assert.ThrowsAsync<DomainValidationException>(() => _service.CreateAsync(new UpsertPricingProfileRequest(
            "Bad Shares", "Markup", 0.30m, 0.5m, 0.4m, "EUR", "None", "None", 2, 1m, null)));
    }
}
