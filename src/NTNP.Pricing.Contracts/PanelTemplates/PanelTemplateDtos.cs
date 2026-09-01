namespace NTNP.Pricing.Contracts.PanelTemplates;

public sealed record ProductFamilyDto(Guid Id, string Code, string Name, string? VoltageRangeDescription, string? SwitchgearClass, bool IsActive);
public sealed record PanelTypeDto(Guid Id, string Code, string Name, string? Description, int SortOrder, bool IsActive);

public sealed record PanelTemplateBomItemDto(
    Guid Id, Guid EquipmentId, string EquipmentCode, string EquipmentDescription,
    decimal QuantityPerPanel, string Unit, decimal WastePercentage, decimal? CostMultiplier, string? Notes, int SortOrder);

public sealed record PanelTemplateDto(
    Guid Id,
    string TemplateCode,
    string TemplateName,
    Guid ProductFamilyId,
    string ProductFamilyName,
    string? VoltageLevel,
    Guid PanelTypeId,
    string PanelTypeName,
    string? TechnicalDescription,
    int RevisionNumber,
    string Status,
    Guid? BodyEsTemplateId,
    string? BodyEsTemplateName,
    string? Notes,
    string? ApprovedByUserName,
    DateTimeOffset? ApprovedAtUtc,
    IReadOnlyList<PanelTemplateBomItemDto> BomItems,
    decimal ComputedEquipmentCostPerPanelIrr,
    byte[] RowVersion);

public sealed record UpsertPanelTemplateBomItemRequest(
    Guid? Id, Guid EquipmentId, decimal QuantityPerPanel, string Unit, decimal WastePercentage,
    decimal? CostMultiplier, string? Notes, int SortOrder);

public sealed record CreatePanelTemplateRequest(
    string TemplateCode, string TemplateName, Guid ProductFamilyId, string? VoltageLevel, Guid PanelTypeId,
    string? TechnicalDescription, Guid? BodyEsTemplateId, string? Notes, IReadOnlyList<UpsertPanelTemplateBomItemRequest> BomItems);

public sealed record UpdatePanelTemplateRequest(
    string TemplateName, string? VoltageLevel, string? TechnicalDescription, Guid? BodyEsTemplateId,
    string? Notes, IReadOnlyList<UpsertPanelTemplateBomItemRequest> BomItems, byte[] RowVersion);

public sealed record ApproveTemplateRequest(byte[] RowVersion);
