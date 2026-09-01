using NTNP.Pricing.Domain.Common;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Domain.Entities;

/// <summary>Section 6/13/21 — one Approve/Reject decision recorded against a project revision.</summary>
public class ApprovalRecord : Entity
{
    public Guid ProjectRevisionId { get; set; }
    public ProjectRevision ProjectRevision { get; set; } = null!;

    public bool IsApproved { get; set; }
    public string? Comments { get; set; }

    public Guid DecidedByUserId { get; set; }
    public string DecidedByUserName { get; set; } = string.Empty;
    public DateTimeOffset DecidedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // Snapshot of the totals reviewed at decision time, for a tamper-evident audit trail.
    public decimal TotalProjectCostIrrAtDecision { get; set; }
    public decimal TotalProjectSellingPriceIrrAtDecision { get; set; }
    public decimal ProjectGrossMarginAtDecision { get; set; }
}
