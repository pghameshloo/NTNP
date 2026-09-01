using NTNP.Pricing.Contracts.PricingProfiles;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class PricingProfilesApiClient : ApiClientBase
{
    public PricingProfilesApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<IReadOnlyList<PricingProfileDto>> ListAsync(bool includeInactive = false, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PricingProfileDto>>($"api/pricing-profiles?includeInactive={includeInactive}", ct);

    public Task<PricingProfileDto> GetAsync(Guid id, CancellationToken ct = default) => GetAsync<PricingProfileDto>($"api/pricing-profiles/{id}", ct);
    public Task<PricingProfileDto> CreateAsync(UpsertPricingProfileRequest request, CancellationToken ct = default) => PostAsync<PricingProfileDto>("api/pricing-profiles", request, ct);
    public Task<PricingProfileDto> UpdateAsync(Guid id, UpsertPricingProfileRequest request, CancellationToken ct = default) => PutAsync<PricingProfileDto>($"api/pricing-profiles/{id}", request, ct);
}
