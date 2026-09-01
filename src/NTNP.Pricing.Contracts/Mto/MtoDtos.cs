namespace NTNP.Pricing.Contracts.Mto;

/// <summary>Section 16 — one consolidated MTO row.</summary>
public sealed record MtoLineDto(
    int Row,
    string Code,
    string? PartNumber,
    string Description,
    string? Brand,
    string? Model,
    string Unit,
    decimal TotalRequiredQuantity,
    string? PurchaseCurrencyCode,
    decimal SnapshotUnitCostIrr,
    decimal TotalProcurementCostIrr,
    IReadOnlyList<string> RelatedPanelTypes,
    string? Notes,
    string Kind);

public sealed record MtoResultDto(
    IReadOnlyList<MtoLineDto> Electrical,
    IReadOnlyList<MtoLineDto> BodyEs,
    IReadOnlyList<MtoLineDto> Combined);
