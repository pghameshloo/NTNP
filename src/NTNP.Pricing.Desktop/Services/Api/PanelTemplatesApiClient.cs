using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.PanelTemplates;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class PanelTemplatesApiClient : ApiClientBase
{
    public PanelTemplatesApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<PagedResult<PanelTemplateDto>> SearchAsync(string? search, int page, int pageSize, Guid? productFamilyId, Guid? panelTypeId, CancellationToken ct = default) =>
        GetAsync<PagedResult<PanelTemplateDto>>(
            $"api/panel-templates?search={Uri.EscapeDataString(search ?? "")}&page={page}&pageSize={pageSize}" +
            $"&productFamilyId={productFamilyId}&panelTypeId={panelTypeId}", ct);

    public Task<PanelTemplateDto> GetAsync(Guid id, CancellationToken ct = default) => GetAsync<PanelTemplateDto>($"api/panel-templates/{id}", ct);
    public Task<PanelTemplateDto> CreateAsync(CreatePanelTemplateRequest request, CancellationToken ct = default) => PostAsync<PanelTemplateDto>("api/panel-templates", request, ct);
    public Task<PanelTemplateDto> UpdateAsync(Guid id, UpdatePanelTemplateRequest request, CancellationToken ct = default) => PutAsync<PanelTemplateDto>($"api/panel-templates/{id}", request, ct);
    public Task<PanelTemplateDto> CreateNewRevisionAsync(Guid id, CancellationToken ct = default) => PostAsync<PanelTemplateDto>($"api/panel-templates/{id}/new-revision", null, ct);
    public Task<PanelTemplateDto> ApproveAsync(Guid id, ApproveTemplateRequest request, CancellationToken ct = default) => PostAsync<PanelTemplateDto>($"api/panel-templates/{id}/approve", request, ct);
}
