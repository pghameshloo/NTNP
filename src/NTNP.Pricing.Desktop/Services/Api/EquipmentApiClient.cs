using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Equipment;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class EquipmentApiClient : ApiClientBase
{
    public EquipmentApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<PagedResult<EquipmentDto>> SearchAsync(
        string? search, int page, int pageSize, bool includeInactive, string? category, bool missingPriceOnly, CancellationToken ct = default) =>
        GetAsync<PagedResult<EquipmentDto>>(
            $"api/equipment?search={Uri.EscapeDataString(search ?? "")}&page={page}&pageSize={pageSize}&includeInactive={includeInactive}" +
            $"&category={Uri.EscapeDataString(category ?? "")}&missingPriceOnly={missingPriceOnly}", ct);

    public Task<EquipmentDto> GetAsync(Guid id, CancellationToken ct = default) => GetAsync<EquipmentDto>($"api/equipment/{id}", ct);

    public Task<IReadOnlyList<EquipmentDto>> MissingOrExpiredPriceReportAsync(int staleDays = 180, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<EquipmentDto>>($"api/equipment/reports/missing-or-expired-prices?staleDays={staleDays}", ct);

    public Task<EquipmentDto> CreateAsync(CreateEquipmentRequest request, CancellationToken ct = default) => PostAsync<EquipmentDto>("api/equipment", request, ct);
    public Task<EquipmentDto> UpdateAsync(Guid id, UpdateEquipmentRequest request, CancellationToken ct = default) => PutAsync<EquipmentDto>($"api/equipment/{id}", request, ct);

    public Task BulkSetActiveAsync(IReadOnlyList<Guid> ids, bool isActive, CancellationToken ct = default) =>
        PostAsync("api/equipment/bulk-activate", new { Ids = ids, IsActive = isActive }, ct);

    public Task<IReadOnlyList<EquipmentPriceDto>> GetPriceHistoryAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<EquipmentPriceDto>>($"api/equipment/{id}/prices", ct);

    public Task<EquipmentPriceDto> AddPriceAsync(CreateEquipmentPriceRequest request, CancellationToken ct = default) =>
        PostAsync<EquipmentPriceDto>("api/equipment/prices", request, ct);

    public Task<EquipmentImportPreviewResult> PreviewImportAsync(byte[] fileContent, string fileName, CancellationToken ct = default) =>
        PostFileAsync<EquipmentImportPreviewResult>("api/equipment/import/preview", fileContent, fileName, ct);

    public Task<EquipmentImportCommitResult> CommitImportAsync(EquipmentImportCommitRequest request, CancellationToken ct = default) =>
        PostAsync<EquipmentImportCommitResult>("api/equipment/import/commit", request, ct);
}
