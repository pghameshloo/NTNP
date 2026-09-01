using NTNP.Pricing.Domain.Common;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Domain.Entities;

/// <summary>
/// Section 10 — a reusable, versioned panel-type sheet (INCOMING/OUTGOING/BUS COUPLER/...) replacing
/// one Excel panel-type sheet. Changing a master template creates a new revision; it never mutates
/// an approved project's already-copied snapshot (Section 10/13).
/// </summary>
public class PanelTemplate : SoftDeletableAuditableEntity
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public Guid ProductFamilyId { get; set; }
    public ProductFamily ProductFamily { get; set; } = null!;
    public string? VoltageLevel { get; set; }
    public Guid PanelTypeId { get; set; }
    public PanelType PanelType { get; set; } = null!;
    public string? TechnicalDescription { get; set; }
    public int RevisionNumber { get; set; } = 1;
    public TemplateStatus Status { get; set; } = TemplateStatus.Draft;
    public Guid? BodyEsTemplateId { get; set; }
    public BodyEsTemplate? BodyEsTemplate { get; set; }
    public string? Notes { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }

    public ICollection<PanelTemplateBomItem> BomItems { get; set; } = new List<PanelTemplateBomItem>();
}

/// <summary>Section 10 — one BOM line inside a panel template.</summary>
public class PanelTemplateBomItem : Entity
{
    public Guid PanelTemplateId { get; set; }
    public PanelTemplate PanelTemplate { get; set; } = null!;

    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    public decimal QuantityPerPanel { get; set; }
    public string Unit { get; set; } = "EA";

    /// <summary>Fraction, e.g. 0.03 = 3% waste. Section 10 formula.</summary>
    public decimal WastePercentage { get; set; }

    /// <summary>Optional, controlled per-line cost multiplier (Section 10) — audited when non-default.</summary>
    public decimal? CostMultiplier { get; set; }

    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}
