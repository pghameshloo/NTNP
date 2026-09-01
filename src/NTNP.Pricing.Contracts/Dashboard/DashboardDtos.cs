namespace NTNP.Pricing.Contracts.Dashboard;

/// <summary>Section 24 — dashboard KPI cards.</summary>
public sealed record DashboardSummaryDto(
    int ActiveProjectsCount,
    int DraftQuotationsCount,
    int PendingApprovalsCount,
    int ApprovedQuotationsCount,
    int EquipmentMissingPriceCount,
    int ExpiredExchangeRatesCount,
    decimal TotalQuotationValueIrr,
    decimal AverageGrossMargin,
    IReadOnlyList<RecentProjectDto> RecentProjects,
    IReadOnlyList<QuotationValuePointDto> QuotationValueOverTime,
    IReadOnlyList<StatusCountDto> ProjectsByStatus,
    IReadOnlyList<CostCompositionDto> CostComposition,
    IReadOnlyList<RecentPriceChangeDto> RecentEquipmentPriceChanges);

public sealed record RecentProjectDto(Guid Id, string ProjectCode, string ProjectName, string CustomerName, string Status, DateTimeOffset UpdatedAtUtc);
public sealed record QuotationValuePointDto(DateOnly Date, decimal TotalSellingPriceIrr);
public sealed record StatusCountDto(string Status, int Count);
public sealed record CostCompositionDto(string Category, decimal AmountIrr);
public sealed record RecentPriceChangeDto(string EquipmentCode, decimal OldCostIrr, decimal NewCostIrr, DateTimeOffset ChangedAtUtc);
