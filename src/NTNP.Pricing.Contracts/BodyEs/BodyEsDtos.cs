namespace NTNP.Pricing.Contracts.BodyEs;

public sealed record BodyEsTemplateItemDto(
    Guid Id, string ComponentCode, string DescriptionFa, string? DescriptionEn, string? Category,
    string Unit, decimal QuantityPerPanel, decimal WastePercentage, decimal UnitCostIrr, decimal LineCostIrr,
    string? Notes, int SortOrder);

public sealed record BodyEsTemplateDto(
    Guid Id,
    string TemplateCode,
    string TemplateName,
    Guid ProductFamilyId,
    string ProductFamilyName,
    Guid PanelTypeId,
    string PanelTypeName,
    string? PanelDimensions,
    int RevisionNumber,
    string Status,
    string? Notes,
    IReadOnlyList<BodyEsTemplateItemDto> Items,
    decimal ComputedCostPerPanelIrr,
    byte[] RowVersion);

public sealed record UpsertBodyEsTemplateItemRequest(
    Guid? Id, string ComponentCode, string DescriptionFa, string? DescriptionEn, string? Category,
    string Unit, decimal QuantityPerPanel, decimal WastePercentage, decimal UnitCostIrr, string? Notes, int SortOrder);

public sealed record CreateBodyEsTemplateRequest(
    string TemplateCode, string TemplateName, Guid ProductFamilyId, Guid PanelTypeId, string? PanelDimensions,
    string? Notes, IReadOnlyList<UpsertBodyEsTemplateItemRequest> Items);

public sealed record UpdateBodyEsTemplateRequest(
    string TemplateName, string? PanelDimensions, string? Notes,
    IReadOnlyList<UpsertBodyEsTemplateItemRequest> Items, byte[] RowVersion);
