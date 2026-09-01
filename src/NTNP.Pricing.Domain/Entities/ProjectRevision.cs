using NTNP.Pricing.Domain.Common;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Domain.Entities;

/// <summary>
/// Section 13 — an immutable-once-approved snapshot of everything that determines a quotation's
/// numbers: pricing method/rate, currency shares, the selling exchange rate used, and the full
/// panel lineup with its BOM/BODY+ES snapshots (Section 15). "Create New Revision Using Latest
/// Prices" creates a new row here and never mutates an existing approved one (Section 13/40).
/// </summary>
public class ProjectRevision : AuditableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int RevisionNumber { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;

    // --- Snapshotted commercial/pricing settings ---
    public string QuotationCurrencyCode { get; set; } = "EUR";
    public decimal RialShare { get; set; }
    public decimal ForeignShare { get; set; }
    public PricingMethod PricingMethod { get; set; }
    public decimal PricingRate { get; set; }
    public RoundingMode IrrRoundingPolicy { get; set; }
    public RoundingMode ForeignRoundingPolicy { get; set; }
    public int ForeignDecimalPlaces { get; set; } = 2;
    public decimal ReconciliationToleranceIrr { get; set; } = 1m;

    // --- Snapshotted selling exchange rate used for the foreign quotation portion ---
    public Guid? SellingExchangeRateId { get; set; }
    public decimal SellingExchangeRateValue { get; set; }
    public DateTimeOffset SellingExchangeRateEffectiveAtUtc { get; set; }

    // --- Computed project-level totals (Section 19), persisted so history never recomputes from
    //     master data that may have since changed ---
    public decimal TotalEquipmentCostIrr { get; set; }
    public decimal TotalBodyEsCostIrr { get; set; }
    public decimal TotalOtherDirectCostIrr { get; set; }
    public decimal TotalProjectCostIrr { get; set; }
    public decimal TotalProjectSellingPriceIrr { get; set; }
    public decimal TotalRialPayable { get; set; }
    public decimal TotalForeignPayable { get; set; }
    public decimal ProjectProfitIrr { get; set; }
    public decimal ProjectGrossMargin { get; set; }
    public decimal ReconciliationDifferenceIrr { get; set; }
    public bool ReconciliationPassed { get; set; }

    public string? SupersededReason { get; set; }
    public Guid? SupersedesRevisionId { get; set; }

    public Guid? SubmittedByUserId { get; set; }
    public DateTimeOffset? SubmittedAtUtc { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public DateTimeOffset? RejectedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? LockedByUserId { get; set; }
    public DateTimeOffset? LockedAtUtc { get; set; }

    public bool IsImmutable => Status is ProjectStatus.Approved or ProjectStatus.Locked or ProjectStatus.Superseded;

    public ICollection<ProjectLine> Lines { get; set; } = new List<ProjectLine>();
    public ICollection<ApprovalRecord> ApprovalRecords { get; set; } = new List<ApprovalRecord>();
}
