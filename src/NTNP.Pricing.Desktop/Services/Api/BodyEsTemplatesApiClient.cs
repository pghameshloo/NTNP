using NTNP.Pricing.Contracts.BodyEs;
using NTNP.Pricing.Contracts.Common;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class BodyEsTemplatesApiClient : ApiClientBase
{
    public BodyEsTemplatesApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<PagedResult<BodyEsTemplateDto>> SearchAsync(string? search, int page, int pageSize, Guid? productFamilyId, Guid? panelTypeId, CancellationToken ct = default) =>
        GetAsync<PagedResult<BodyEsTemplateDto>>(
            $"api/body-es-templates?search={Uri.EscapeDataString(search ?? "")}&page={page}&pageSize={pageSize}" +
            $"&productFamilyId={productFamilyId}&panelTypeId={panelTypeId}", ct);

    public Task<BodyEsTemplateDto> GetAsync(Guid id, CancellationToken ct = default) => GetAsync<BodyEsTemplateDto>($"api/body-es-templates/{id}", ct);
    public Task<BodyEsTemplateDto> CreateAsync(CreateBodyEsTemplateRequest request, CancellationToken ct = default) => PostAsync<BodyEsTemplateDto>("api/body-es-templates", request, ct);
    public Task<BodyEsTemplateDto> UpdateAsync(Guid id, UpdateBodyEsTemplateRequest request, CancellationToken ct = default) => PutAsync<BodyEsTemplateDto>($"api/body-es-templates/{id}", request, ct);
    public Task<BodyEsTemplateDto> ApproveAsync(Guid id, byte[] rowVersion, CancellationToken ct = default) => PostAsync<BodyEsTemplateDto>($"api/body-es-templates/{id}/approve", rowVersion, ct);
}
