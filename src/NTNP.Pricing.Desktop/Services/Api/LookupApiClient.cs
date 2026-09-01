using NTNP.Pricing.Contracts.PanelTemplates;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class LookupApiClient : ApiClientBase
{
    public LookupApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<IReadOnlyList<ProductFamilyDto>> ProductFamiliesAsync(bool includeInactive = false, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ProductFamilyDto>>($"api/lookups/product-families?includeInactive={includeInactive}", ct);

    public Task<ProductFamilyDto> CreateProductFamilyAsync(string code, string name, string? voltageRangeDescription, string? switchgearClass, CancellationToken ct = default) =>
        PostAsync<ProductFamilyDto>("api/lookups/product-families", new { Code = code, Name = name, VoltageRangeDescription = voltageRangeDescription, SwitchgearClass = switchgearClass }, ct);

    public Task<IReadOnlyList<PanelTypeDto>> PanelTypesAsync(bool includeInactive = false, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PanelTypeDto>>($"api/lookups/panel-types?includeInactive={includeInactive}", ct);

    public Task<PanelTypeDto> CreatePanelTypeAsync(string code, string name, string? description, int sortOrder, CancellationToken ct = default) =>
        PostAsync<PanelTypeDto>("api/lookups/panel-types", new { Code = code, Name = name, Description = description, SortOrder = sortOrder }, ct);
}
