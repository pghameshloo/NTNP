using NTNP.Pricing.Domain.Common;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Domain.Entities;

/// <summary>
/// Section 13 — the project header. Working (mutable) settings live here as the "current draft"
/// values; every field that must survive approval immutably is snapshotted again onto the active
/// <see cref="ProjectRevision"/> at revision-creation time (Section 13: "Each revision must
/// snapshot..."). The header itself is never locked — only revisions are.
/// </summary>
public class Project : AuditableEntity
{
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string? RfqNumber { get; set; }
    public DateOnly? InquiryDate { get; set; }

    public string? QuotationNumber { get; set; }
    public DateOnly? QuotationDate { get; set; }
    public DateOnly? QuotationValidUntil { get; set; }

    public string? ProjectDescription { get; set; }
    public string? CommercialNotes { get; set; }
    public string? TechnicalNotes { get; set; }

    public string QuotationCurrencyCode { get; set; } = "EUR";
    public decimal RialShare { get; set; } = 0.15m;
    public decimal ForeignShare { get; set; } = 0.85m;

    public Guid? PricingProfileId { get; set; }
    public PricingProfile? PricingProfile { get; set; }
    public PricingMethod PricingMethod { get; set; } = PricingMethod.Markup;

    /// <summary>Fraction — markup % or gross-margin %, per <see cref="PricingMethod"/>.</summary>
    public decimal PricingRate { get; set; } = 0.30m;

    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;

    public int CurrentRevisionNumber { get; set; } = 1;
    public Guid? CurrentRevisionId { get; set; }
    public ProjectRevision? CurrentRevision { get; set; }

    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewedByUserName { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<ProjectRevision> Revisions { get; set; } = new List<ProjectRevision>();
}
