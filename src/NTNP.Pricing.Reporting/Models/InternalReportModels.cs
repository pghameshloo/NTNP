namespace NTNP.Pricing.Reporting.Models;

/// <summary>
/// Section 27 — internal reports. Separate from <see cref="CustomerQuotationModel"/> by
/// construction: this model type carries exactly the fields Section 27 says internal reports may
/// contain (cost, BODY+ES cost, purchase/selling rates, pricing method, profit/margin, overrides,
/// validation warnings) and is never used to render a customer-facing document.
/// </summary>
public sealed record InternalCostingReportModel(
    CompanyBranding Company,
    string ProjectCode,
    string ProjectName,
    string CustomerName,
    int RevisionNumber,
    string RevisionStatus,
    DateTimeOffset GeneratedAtUtc,
    string GeneratedByUserName,
    IReadOnlyList<InternalCostingLine> Lines,
    InternalCostingTotals Totals);

public sealed record InternalCostingLine(
    int Row,
    string CellCode,
    string PanelType,
    string Description,
    decimal Quantity,
    decimal EquipmentCostPerPanel,
    decimal BodyEsCostPerPanel,
    decimal OtherDirectCostPerPanel,
    decimal TotalCostPerPanel,
    decimal TotalLineCost,
    string PricingMethod,
    decimal PricingRate,
    decimal SellingPricePerPanel,
    decimal TotalLineSellingPriceIrr,
    decimal RialShare,
    decimal RialPayable,
    string QuotationCurrency,
    decimal ForeignShare,
    decimal SellingExchangeRate,
    decimal ForeignPayable,
    decimal Profit,
    decimal GrossMargin,
    bool ReconciliationPassed,
    bool HasOverride,
    bool HasValidationErrors);

public sealed record InternalCostingTotals(
    decimal TotalEquipmentCostIrr, decimal TotalBodyEsCostIrr, decimal TotalOtherDirectCostIrr,
    decimal TotalProjectCostIrr, decimal TotalProjectSellingPriceIrr, decimal TotalRialPayable,
    decimal TotalForeignPayable, decimal ProjectProfitIrr, decimal ProjectGrossMargin,
    bool ReconciliationPassed, IReadOnlyList<string> ApprovalBlockers);

/// <summary>Section 16 — Electrical / BODY+ES / Combined MTO, and per-panel / project-wide BOM, share this row shape.</summary>
public sealed record BomMtoReportRow(
    int Row, string Code, string? PartNumber, string Description, string? Brand, string? Model, string Unit,
    decimal Quantity, string? PurchaseCurrencyCode, decimal SnapshotUnitCostIrr, decimal TotalCostIrr,
    IReadOnlyList<string> RelatedPanelTypes, string? Notes);

public sealed record BomMtoReportModel(
    CompanyBranding Company, string Title, string ProjectCode, string ProjectName, int RevisionNumber,
    DateTimeOffset GeneratedAtUtc, IReadOnlyList<BomMtoReportRow> Rows, decimal GrandTotalIrr);

public sealed record RevisionComparisonReportModel(
    CompanyBranding Company, string ProjectCode, string ProjectName, int FromRevision, int ToRevision,
    decimal CostDeltaIrr, decimal SellingPriceDeltaIrr, decimal ProfitDeltaIrr, decimal GrossMarginDelta,
    IReadOnlyList<RevisionComparisonReportRow> ChangedFields);

public sealed record RevisionComparisonReportRow(string CellCode, string FieldName, string OldValue, string NewValue);
