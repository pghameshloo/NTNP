using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Domain.Calculation;

/// <summary>
/// Section 4/12 — output-stage-only rounding. Never applied to values persisted on
/// <see cref="Entities.ProjectLine"/>/<see cref="Entities.ProjectRevision"/>; applied by the
/// Reporting layer and by explicit "rounded for display" projections.
/// </summary>
public static class RoundingPolicy
{
    public static decimal Apply(decimal value, RoundingMode mode, int decimalPlaces = 0)
    {
        return mode switch
        {
            RoundingMode.None => value,
            RoundingMode.NearestInteger => Math.Round(value, 0, MidpointRounding.AwayFromZero),
            RoundingMode.NearestTen => Math.Round(value / 10m, 0, MidpointRounding.AwayFromZero) * 10m,
            RoundingMode.NearestHundred => Math.Round(value / 100m, 0, MidpointRounding.AwayFromZero) * 100m,
            RoundingMode.NearestThousand => Math.Round(value / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m,
            _ => value,
        };
    }

    /// <summary>Applies decimal-place rounding for foreign-currency display (e.g. 2 dp for EUR/USD).</summary>
    public static decimal ApplyForeign(decimal value, int decimalPlaces) =>
        Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);
}
