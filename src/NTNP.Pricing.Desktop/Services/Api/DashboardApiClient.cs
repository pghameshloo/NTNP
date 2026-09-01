using NTNP.Pricing.Contracts.Dashboard;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class DashboardApiClient : ApiClientBase
{
    public DashboardApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default) => GetAsync<DashboardSummaryDto>("api/dashboard/summary", ct);
}
