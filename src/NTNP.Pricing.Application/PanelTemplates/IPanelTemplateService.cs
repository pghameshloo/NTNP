using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.PanelTemplates;

namespace NTNP.Pricing.Application.PanelTemplates;

public interface IPanelTemplateService
{
    Task<PagedResult<PanelTemplateDto>> SearchAsync(PagedQuery query, Guid? productFamilyId, Guid? panelTypeId, CancellationToken ct = default);
    Task<PanelTemplateDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<PanelTemplateDto> CreateAsync(CreatePanelTemplateRequest request, CancellationToken ct = default);
    Task<PanelTemplateDto> UpdateAsync(Guid id, UpdatePanelTemplateRequest request, CancellationToken ct = default);

    /// <summary>Section 10 — changing an approved template creates a new Draft revision instead of mutating it.</summary>
    Task<PanelTemplateDto> CreateNewRevisionAsync(Guid id, CancellationToken ct = default);

    Task<PanelTemplateDto> ApproveAsync(Guid id, ApproveTemplateRequest request, CancellationToken ct = default);
}
