using NTNP.Pricing.Contracts.Settings;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class SettingsApiClient : ApiClientBase
{
    public SettingsApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<CompanySettingsDto> GetAsync(CancellationToken ct = default) => GetAsync<CompanySettingsDto>("api/settings/company", ct);
    public Task<CompanySettingsDto> UpdateAsync(UpdateCompanySettingsRequest request, CancellationToken ct = default) => PutAsync<CompanySettingsDto>("api/settings/company", request, ct);
}
