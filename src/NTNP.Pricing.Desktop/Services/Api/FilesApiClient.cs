using NTNP.Pricing.Contracts.Files;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class FilesApiClient : ApiClientBase
{
    public FilesApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<IReadOnlyList<StoredFileDto>> ListAsync(Guid? projectId = null, Guid? projectRevisionId = null, string? category = null, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<StoredFileDto>>($"api/files?projectId={projectId}&projectRevisionId={projectRevisionId}&category={Uri.EscapeDataString(category ?? "")}", ct);

    public Task<StoredFileDto> GetAsync(Guid id, CancellationToken ct = default) => GetAsync<StoredFileDto>($"api/files/{id}", ct);

    public Task<(byte[] Bytes, string? FileName, string ContentType)> DownloadAsync(Guid id, CancellationToken ct = default) =>
        GetBytesAsync($"api/files/{id}/download", ct);
}
