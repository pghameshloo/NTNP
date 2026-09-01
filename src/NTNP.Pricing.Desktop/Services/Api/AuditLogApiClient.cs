using NTNP.Pricing.Contracts.Audit;
using NTNP.Pricing.Contracts.Common;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class AuditLogApiClient : ApiClientBase
{
    public AuditLogApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<PagedResult<AuditLogEntryDto>> SearchAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        var url = "api/audit-log?" +
                   $"entityType={Uri.EscapeDataString(query.EntityType ?? "")}&entityId={Uri.EscapeDataString(query.EntityId ?? "")}" +
                   $"&userId={query.UserId}&projectId={query.ProjectId}" +
                   $"&fromUtc={Uri.EscapeDataString(query.FromUtc?.ToString("O") ?? "")}&toUtc={Uri.EscapeDataString(query.ToUtc?.ToString("O") ?? "")}" +
                   $"&page={query.Page}&pageSize={query.PageSize}";
        return GetAsync<PagedResult<AuditLogEntryDto>>(url, ct);
    }
}
