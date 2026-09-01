using NTNP.Pricing.Domain.Common;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Domain.Entities;

/// <summary>Section 12 — a reusable default pricing configuration a project can start from.</summary>
public class PricingProfile : SoftDeletableAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public PricingMethod PricingMethod { get; set; }

    /// <summary>Fraction: 0.30 = 30% markup, or 0.30 = 30% gross margin, per <see cref="PricingMethod"/>.</summary>
    public decimal DefaultRate { get; set; }

    public decimal DefaultRialShare { get; set; }
    public decimal DefaultForeignShare { get; set; }
    public string DefaultQuotationCurrencyCode { get; set; } = "EUR";

    public RoundingMode IrrRoundingPolicy { get; set; } = RoundingMode.NearestThousand;
    public RoundingMode ForeignRoundingPolicy { get; set; } = RoundingMode.NearestInteger;
    public int ForeignDecimalPlaces { get; set; } = 2;

    /// <summary>IRR tolerance for TOTAL reconciliation (Section 18). Default 1 IRR — see ASSUMPTIONS.md §6.</summary>
    public decimal ReconciliationToleranceIrr { get; set; } = 1m;

    public DateTimeOffset EffectiveAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Equivalent multiplier for display purposes only (e.g. 1.30 for 30% markup) — derived, not authoritative.</summary>
    public decimal EquivalentMultiplier =>
        PricingMethod == PricingMethod.Markup
            ? 1 + DefaultRate
            : DefaultRate >= 1m ? 0m : 1m / (1m - DefaultRate);
}
