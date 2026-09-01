namespace NTNP.Pricing.Domain.Enums;

/// <summary>
/// Output-stage rounding only (Section 4: stored calculation values are never rounded). Applied by
/// the Reporting layer and by explicit "display/rounded" read-model projections.
/// </summary>
public enum RoundingMode
{
    None = 0,
    NearestInteger = 1,
    NearestTen = 2,
    NearestHundred = 3,
    NearestThousand = 4,
}
