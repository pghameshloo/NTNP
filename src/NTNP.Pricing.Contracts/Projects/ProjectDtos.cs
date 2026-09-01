namespace NTNP.Pricing.Contracts.Projects;

public sealed record ProjectListItemDto(
    Guid Id, string ProjectCode, string ProjectName, string CustomerName, string Status,
    int CurrentRevisionNumber, decimal? TotalProjectSellingPriceIrr, string QuotationCurrencyCode,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record ProjectDto(
    Guid Id,
    string ProjectCode,
    string ProjectName,
    Guid CustomerId,
    string CustomerName,
    string? RfqNumber,
    DateOnly? InquiryDate,
    string? QuotationNumber,
    DateOnly? QuotationDate,
    DateOnly? QuotationValidUntil,
    string? ProjectDescription,
    string? CommercialNotes,
    string? TechnicalNotes,
    string QuotationCurrencyCode,
    decimal RialShare,
    decimal ForeignShare,
    Guid? PricingProfileId,
    string PricingMethod,
    decimal PricingRate,
    string Status,
    int CurrentRevisionNumber,
    Guid? CurrentRevisionId,
    string CreatedByUserName,
    DateTimeOffset CreatedAtUtc,
    byte[] RowVersion);

public sealed record CreateProjectRequest(
    string ProjectCode, string ProjectName, Guid CustomerId, string? RfqNumber, DateOnly? InquiryDate,
    string? ProjectDescription, string? CommercialNotes, string? TechnicalNotes,
    string QuotationCurrencyCode, decimal RialShare, decimal ForeignShare,
    Guid? PricingProfileId, string PricingMethod, decimal PricingRate);

public sealed record UpdateProjectInfoRequest(
    string ProjectName, string? RfqNumber, DateOnly? InquiryDate, string? QuotationNumber,
    DateOnly? QuotationDate, DateOnly? QuotationValidUntil, string? ProjectDescription,
    string? CommercialNotes, string? TechnicalNotes, byte[] RowVersion);

public sealed record UpdateProjectPricingSettingsRequest(
    string QuotationCurrencyCode, decimal RialShare, decimal ForeignShare,
    Guid? PricingProfileId, string PricingMethod, decimal PricingRate, byte[] RowVersion);

// --- Lineup / lines ---

public sealed record AddProjectLineRequest(Guid PanelTemplateId, string CellCode, decimal QuantityOfPanels, decimal OtherDirectCostPerPanel);

public sealed record UpdateProjectLineQuantityRequest(decimal QuantityOfPanels, decimal OtherDirectCostPerPanel, byte[] RowVersion);

public sealed record ProjectLineOverrideRequest(string FieldName, string NewValue, string Reason, byte[] RowVersion);

public sealed record ProjectLineBomItemDto(
    Guid Id, string EquipmentCode, string Description, string? PartNumber, string? Brand, string? Model, string Unit,
    decimal QuantityPerPanel, decimal WastePercentage, decimal AdjustedQuantityPerPanel,
    string PurchaseCurrencyCode, decimal? PurchaseExchangeRateSnapshot, decimal UnitCostIrrSnapshot, decimal LineCostIrr,
    bool IsOverridden);

public sealed record ProjectLineBodyEsItemDto(
    Guid Id, string ComponentCode, string Description, string Unit,
    decimal QuantityPerPanel, decimal WastePercentage, decimal AdjustedQuantityPerPanel,
    decimal UnitCostIrrSnapshot, decimal LineCostIrr, bool IsOverridden);

/// <summary>Section 14/19 — every TOTAL-screen column for one project line.</summary>
public sealed record ProjectLineDto(
    Guid Id,
    int LineNumber,
    string CellCode,
    Guid? PanelTemplateId,
    string PanelTemplateCode,
    string ProductFamilyName,
    string PanelTypeName,
    string Description,
    string? VoltageLevel,
    decimal QuantityOfPanels,
    decimal EquipmentCostPerPanel,
    decimal BodyEsCostPerPanel,
    decimal OtherDirectCostPerPanel,
    decimal TotalCostPerPanel,
    decimal TotalLineCost,
    string PricingMethod,
    decimal PricingRateApplied,
    decimal SellingPricePerPanel,
    decimal TotalLineSellingPrice,
    decimal RialShareApplied,
    decimal RialPayableAmount,
    string QuotationCurrencyCode,
    decimal ForeignShareApplied,
    decimal SellingExchangeRateApplied,
    decimal ForeignPayableAmount,
    decimal ProfitIrr,
    decimal GrossMargin,
    bool ReconciliationPassed,
    decimal ReconciliationDifferenceIrr,
    bool HasOverride,
    bool HasValidationErrors,
    IReadOnlyList<ProjectLineBomItemDto> BomItems,
    IReadOnlyList<ProjectLineBodyEsItemDto> BodyEsItems);

/// <summary>Section 19 — TOTAL screen's project-level summary and PASS/FAIL reconciliation control.</summary>
public sealed record ProjectRevisionTotalsDto(
    decimal TotalEquipmentCostIrr,
    decimal TotalBodyEsCostIrr,
    decimal TotalOtherDirectCostIrr,
    decimal TotalProjectCostIrr,
    decimal TotalProjectSellingPriceIrr,
    decimal TotalRialPayable,
    string QuotationCurrencyCode,
    decimal SellingExchangeRateValue,
    decimal TotalForeignPayable,
    decimal ProjectProfitIrr,
    decimal ProjectGrossMargin,
    decimal ReconciliationDifferenceIrr,
    bool ReconciliationPassed,
    IReadOnlyList<string> ApprovalBlockers);

public sealed record ProjectRevisionDto(
    Guid Id,
    Guid ProjectId,
    int RevisionNumber,
    string Status,
    string QuotationCurrencyCode,
    decimal RialShare,
    decimal ForeignShare,
    string PricingMethod,
    decimal PricingRate,
    decimal SellingExchangeRateValue,
    DateTimeOffset SellingExchangeRateEffectiveAtUtc,
    IReadOnlyList<ProjectLineDto> Lines,
    ProjectRevisionTotalsDto Totals,
    string? SubmittedByUserName,
    DateTimeOffset? SubmittedAtUtc,
    string? ApprovedByUserName,
    DateTimeOffset? ApprovedAtUtc,
    string? RejectionReason,
    byte[] RowVersion);

public sealed record RevisionListItemDto(Guid Id, int RevisionNumber, string Status, decimal TotalProjectSellingPriceIrr, decimal ProjectGrossMargin, DateTimeOffset CreatedAtUtc);

public sealed record RevisionComparisonLineDelta(string CellCode, string FieldName, string OldValue, string NewValue);

public sealed record RevisionComparisonDto(
    int FromRevisionNumber, int ToRevisionNumber,
    decimal CostDeltaIrr, decimal SellingPriceDeltaIrr, decimal ProfitDeltaIrr, decimal GrossMarginDelta,
    IReadOnlyList<RevisionComparisonLineDelta> ChangedFields);

// --- Approval ---

public sealed record SubmitForApprovalRequest(byte[] RowVersion);
public sealed record ApprovalDecisionRequest(bool Approve, string? Comments, byte[] RowVersion);
public sealed record LockRevisionRequest(byte[] RowVersion);

public sealed record ApprovalHistoryItemDto(
    Guid Id, bool IsApproved, string? Comments, string DecidedByUserName, DateTimeOffset DecidedAtUtc,
    decimal TotalProjectCostIrrAtDecision, decimal TotalProjectSellingPriceIrrAtDecision, decimal ProjectGrossMarginAtDecision);

// --- Overrides / audit ---

public sealed record ProjectLineOverrideHistoryDto(Guid Id, string FieldName, string OldValue, string NewValue, string Reason, string UserName, DateTimeOffset AtUtc);
