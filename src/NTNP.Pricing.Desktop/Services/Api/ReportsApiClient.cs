namespace NTNP.Pricing.Desktop.Services.Api;

/// <summary>Section 16/19/21/26 — report generation. Every call returns the raw file bytes plus the server-suggested filename.</summary>
public sealed class ReportsApiClient : ApiClientBase
{
    public ReportsApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<(byte[] Bytes, string? FileName, string ContentType)> GetQuotationPdfAsync(Guid revisionId, string language, CancellationToken ct = default) =>
        GetBytesAsync($"api/project-revisions/{revisionId}/reports/quotation?language={language}", ct);

    public Task<(byte[] Bytes, string? FileName, string ContentType)> GetInternalCostingAsync(Guid revisionId, string format, CancellationToken ct = default) =>
        GetBytesAsync($"api/project-revisions/{revisionId}/reports/internal-costing?format={format}", ct);

    public Task<(byte[] Bytes, string? FileName, string ContentType)> GetMtoAsync(Guid revisionId, string kind, string format, CancellationToken ct = default) =>
        GetBytesAsync($"api/project-revisions/{revisionId}/reports/mto?kind={kind}&format={format}", ct);

    public Task<(byte[] Bytes, string? FileName, string ContentType)> GetRevisionComparisonAsync(Guid fromRevisionId, Guid toRevisionId, string format, CancellationToken ct = default) =>
        GetBytesAsync($"api/project-revisions/compare/report?fromRevisionId={fromRevisionId}&toRevisionId={toRevisionId}&format={format}", ct);
}
