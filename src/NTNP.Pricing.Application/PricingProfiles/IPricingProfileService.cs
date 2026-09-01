using NTNP.Pricing.Contracts.PricingProfiles;

namespace NTNP.Pricing.Application.PricingProfiles;

public interface IPricingProfileService
{
    Task<IReadOnlyList<PricingProfileDto>> ListAsync(bool includeInactive, CancellationToken ct = default);
    Task<PricingProfileDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<PricingProfileDto> CreateAsync(UpsertPricingProfileRequest request, CancellationToken ct = default);
    Task<PricingProfileDto> UpdateAsync(Guid id, UpsertPricingProfileRequest request, CancellationToken ct = default);
}
