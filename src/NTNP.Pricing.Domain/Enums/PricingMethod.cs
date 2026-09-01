namespace NTNP.Pricing.Domain.Enums;

/// <summary>
/// Section 17: markup and gross margin are deliberately distinct methods and must never be
/// conflated. Markup multiplies cost; gross margin divides cost by (1 - margin).
/// </summary>
public enum PricingMethod
{
    Markup = 1,
    GrossMargin = 2,
}
