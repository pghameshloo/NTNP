using NTNP.Pricing.Domain.Common;

namespace NTNP.Pricing.Domain.Entities;

/// <summary>
/// Section 8. <see cref="Code"/> (ISO 4217, e.g. "IRR", "EUR") is the natural key used everywhere
/// else in the schema (Equipment purchase currency, Project quotation currency, ExchangeRate).
/// </summary>
public class Currency : SoftDeletableAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public bool IsBaseCurrency { get; set; }

    public ICollection<ExchangeRate> Rates { get; set; } = new List<ExchangeRate>();
}

/// <summary>
/// Section 8 — one historical rate observation. Purchase and selling rates are independent and
/// each has its own effective-dated history; a project revision snapshots the exact rate row it
/// used (via <see cref="Entities.ProjectRevision"/> snapshot fields), so later rate changes never
/// alter an approved revision.
/// </summary>
public class ExchangeRate : AuditableEntity
{
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    /// <summary>Used only to convert foreign equipment purchase prices into IRR cost (Section 8).</summary>
    public decimal PurchaseRateToIrr { get; set; }

    /// <summary>Used only to convert the foreign portion of a customer quotation (Section 8).</summary>
    public decimal SellingRateToIrr { get; set; }

    public DateTimeOffset EffectiveAtUtc { get; set; }
    public string? RateSource { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
