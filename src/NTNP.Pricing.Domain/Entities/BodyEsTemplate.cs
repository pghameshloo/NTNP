using NTNP.Pricing.Domain.Common;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Domain.Entities;

/// <summary>Section 11 — body/sheet-metal/mechanical costing, kept separate from equipment BOM cost.</summary>
public class BodyEsTemplate : SoftDeletableAuditableEntity
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public Guid ProductFamilyId { get; set; }
    public ProductFamily ProductFamily { get; set; } = null!;
    public Guid PanelTypeId { get; set; }
    public PanelType PanelType { get; set; } = null!;
    public string? PanelDimensions { get; set; }
    public int RevisionNumber { get; set; } = 1;
    public TemplateStatus Status { get; set; } = TemplateStatus.Draft;
    public string? Notes { get; set; }

    public ICollection<BodyEsTemplateItem> Items { get; set; } = new List<BodyEsTemplateItem>();
}

/// <summary>Section 11 — one BODY+ES component line.</summary>
public class BodyEsTemplateItem : Entity
{
    public Guid BodyEsTemplateId { get; set; }
    public BodyEsTemplate BodyEsTemplate { get; set; } = null!;

    public string ComponentCode { get; set; } = string.Empty;
    public string DescriptionFa { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? Category { get; set; }
    public string Unit { get; set; } = "EA";
    public decimal QuantityPerPanel { get; set; }
    public decimal WastePercentage { get; set; }
    public decimal UnitCostIrr { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Adjusted Quantity × Unit Cost IRR (Section 11 formula) — computed, not stored twice.</summary>
    public decimal LineCostIrr => Math.Round(QuantityPerPanel * (1 + WastePercentage) * UnitCostIrr, 6);
}
