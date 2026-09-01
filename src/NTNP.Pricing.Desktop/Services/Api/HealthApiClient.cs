using NTNP.Pricing.Contracts.Common;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class HealthApiClient : ApiClientBase
{
    public HealthApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<ServerStatusDto> GetStatusAsync(CancellationToken ct = default) => GetAsync<ServerStatusDto>("api/health", ct);
}
