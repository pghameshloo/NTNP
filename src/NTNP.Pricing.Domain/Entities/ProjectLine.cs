using NTNP.Pricing.Domain.Common;

namespace NTNP.Pricing.Domain.Entities;

/// <summary>
/// Section 14 — one panel-line inside a project revision (a "cell"). Panel template/product-family
/// text fields are snapshotted at the time the line was generated so the line reads correctly even
/// if the source template is edited or archived later (Section 15).
/// </summary>
public class ProjectLine : Entity
{
    public Guid ProjectRevisionId { get; set; }
    public ProjectRevision ProjectRevision { get; set; } = null!;

    public int LineNumber { get; set; }
    public string CellCode { get; set; } = string.Empty;

    public Guid? PanelTemplateId { get; set; }
    public string PanelTemplateCodeSnapshot { get; set; } = string.Empty;
    public int PanelTemplateRevisionSnapshot { get; set; }
    public string ProductFamilyNameSnapshot { get; set; } = string.Empty;
    public string PanelTypeNameSnapshot { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? VoltageLevel { get; set; }

    public decimal QuantityOfPanels { get; set; }

    // --- Cost (Section 14/17) — all per single panel unless stated otherwise ---
    public decimal EquipmentCostPerPanel { get; set; }
    public decimal BodyEsCostPerPanel { get; set; }
    public decimal OtherDirectCostPerPanel { get; set; }
    public decimal TotalCostPerPanel { get; set; }
    public decimal TotalLineCost { get; set; }

    // --- Selling (Section 17/18) ---
    public decimal PricingRateApplied { get; set; }
    public decimal SellingPricePerPanel { get; set; }
    public decimal TotalLineSellingPrice { get; set; }

    public decimal RialShareApplied { get; set; }
    public decimal RialPayableAmount { get; set; }
    public decimal ForeignShareApplied { get; set; }
    public decimal SellingExchangeRateApplied { get; set; }
    public decimal ForeignPayableAmount { get; set; }

    public decimal ProfitIrr { get; set; }
    public decimal GrossMargin { get; set; }

    public bool ReconciliationPassed { get; set; }
    public decimal ReconciliationDifferenceIrr { get; set; }

    /// <summary>Set when any override was applied to this line (Section 14) — drives UI badges.</summary>
    public bool HasOverride { get; set; }

    /// <summary>Missing price/rate/template validation issues surfaced by the BOM generator (Section 15/19).</summary>
    public bool HasValidationErrors { get; set; }

    public ICollection<ProjectLineBomItem> BomItems { get; set; } = new List<ProjectLineBomItem>();
    public ICollection<ProjectLineBodyEsItem> BodyEsItems { get; set; } = new List<ProjectLineBodyEsItem>();
    public ICollection<ProjectLineOverride> Overrides { get; set; } = new List<ProjectLineOverride>();
}

/// <summary>
/// Section 15 — an immutable snapshot of one equipment BOM line as generated for this project line:
/// equipment identity, quantity, and the equipment price/exchange-rate version used, frozen at
/// generation time.
/// </summary>
public class ProjectLineBomItem : Entity
{
    public Guid ProjectLineId { get; set; }
    public ProjectLine ProjectLine { get; set; } = null!;

    public Guid EquipmentId { get; set; }
    public string EquipmentCodeSnapshot { get; set; } = string.Empty;
    public string DescriptionSnapshot { get; set; } = string.Empty;
    public string? PartNumberSnapshot { get; set; }
    public string? BrandSnapshot { get; set; }
    public string? ModelSnapshot { get; set; }
    public string Unit { get; set; } = "EA";

    public decimal QuantityPerPanel { get; set; }
    public decimal WastePercentage { get; set; }
    public decimal AdjustedQuantityPerPanel { get; set; }

    public Guid? EquipmentPriceId { get; set; }
    public string PurchaseCurrencyCodeSnapshot { get; set; } = "IRR";
    public decimal? PurchaseExchangeRateSnapshot { get; set; }
    public decimal UnitCostIrrSnapshot { get; set; }
    public decimal LineCostIrr { get; set; }

    public bool IsOverridden { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Section 15/11 — an immutable snapshot of one BODY+ES component line for this project line.</summary>
public class ProjectLineBodyEsItem : Entity
{
    public Guid ProjectLineId { get; set; }
    public ProjectLine ProjectLine { get; set; } = null!;

    public Guid? BodyEsTemplateItemId { get; set; }
    public string ComponentCodeSnapshot { get; set; } = string.Empty;
    public string DescriptionSnapshot { get; set; } = string.Empty;
    public string Unit { get; set; } = "EA";

    public decimal QuantityPerPanel { get; set; }
    public decimal WastePercentage { get; set; }
    public decimal AdjustedQuantityPerPanel { get; set; }
    public decimal UnitCostIrrSnapshot { get; set; }
    public decimal LineCostIrr { get; set; }

    public bool IsOverridden { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Section 14 — an authorized project-specific override, fully audited: reason, user, timestamp,
/// old/new value and the revision it belongs to.
/// </summary>
public class ProjectLineOverride : Entity
{
    public Guid ProjectLineId { get; set; }
    public ProjectLine ProjectLine { get; set; } = null!;

    public string FieldName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTimeOffset AtUtc { get; set; } = DateTimeOffset.UtcNow;
}
