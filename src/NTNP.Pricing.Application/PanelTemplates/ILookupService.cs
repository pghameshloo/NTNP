using NTNP.Pricing.Contracts.PanelTemplates;

namespace NTNP.Pricing.Application.PanelTemplates;

/// <summary>Section 3 — product families and panel types are admin-editable lookup data, not hardcoded.</summary>
public interface ILookupService
{
    Task<IReadOnlyList<ProductFamilyDto>> ListProductFamiliesAsync(bool includeInactive, CancellationToken ct = default);
    Task<ProductFamilyDto> CreateProductFamilyAsync(string code, string name, string? voltageRange, string? switchgearClass, CancellationToken ct = default);
    Task<IReadOnlyList<PanelTypeDto>> ListPanelTypesAsync(bool includeInactive, CancellationToken ct = default);
    Task<PanelTypeDto> CreatePanelTypeAsync(string code, string name, string? description, int sortOrder, CancellationToken ct = default);
}
