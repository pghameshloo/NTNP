using NTNP.Pricing.Domain.Common;

namespace NTNP.Pricing.Domain.Entities;

/// <summary>
/// Section 9 — replaces the legacy "SOURCE PRICE DEVICES" sheet. <see cref="Code"/> is the unique
/// lookup key every panel/BODY+ES BOM line references; the equipment's price itself lives in the
/// versioned <see cref="EquipmentPrice"/> history, never duplicated into templates or BOM lines.
/// </summary>
public class Equipment : SoftDeletableAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string? TechnicalPartNumber { get; set; }
    public string DescriptionFa { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Subcategory { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public string? Supplier { get; set; }
    public string Unit { get; set; } = "EA";
    public int? LeadTimeDays { get; set; }
    public string? Notes { get; set; }

    public ICollection<EquipmentPrice> Prices { get; set; } = new List<EquipmentPrice>();

    /// <summary>Convenience accessor — the currently active price row (highest effective date).</summary>
    public EquipmentPrice? CurrentPrice =>
        Prices.Where(p => p.IsActive)
              .OrderByDescending(p => p.EffectiveAtUtc)
              .FirstOrDefault();
}

/// <summary>
/// Section 9 — an immutable historical price observation for one <see cref="Equipment"/>. A new
/// price never overwrites an old one; project revisions snapshot the exact row they used.
/// </summary>
public class EquipmentPrice : AuditableEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    public string PurchaseCurrencyCode { get; set; } = "IRR";
    public decimal? ForeignUnitPrice { get; set; }
    public decimal? RialUnitPrice { get; set; }

    /// <summary>The purchase exchange rate applied at entry time (null when currency == IRR).</summary>
    public decimal? PurchaseExchangeRateSnapshot { get; set; }

    /// <summary>
    /// Computed once at write time by the domain rule in <c>EquipmentPriceCalculator</c> and stored
    /// (not recomputed on read) so history is a faithful record of what was true at that time.
    /// </summary>
    public decimal FinalUnitCostIrr { get; set; }

    public DateTimeOffset EffectiveAtUtc { get; set; }
    public string? PriceSourceText { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
