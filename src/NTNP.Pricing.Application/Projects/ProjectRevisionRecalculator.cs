using NTNP.Pricing.Domain.Calculation;
using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Application.Projects;

/// <summary>
/// Shared recalculation entry point used by every mutation that can change a revision's numbers
/// (adding/removing/overriding a line, changing pricing settings). Always recomputes every BOM/
/// BODY+ES item, every line, then the revision totals — cheap given realistic project sizes, and it
/// guarantees the TOTAL screen can never drift from its inputs (Section 19).
/// </summary>
public static class ProjectRevisionRecalculator
{
    public static void RecalculateLine(ProjectLine line, ProjectRevision revision)
    {
        foreach (var bomItem in line.BomItems) ProjectLineCalculator.CalculateBomItem(bomItem);
        foreach (var bodyEsItem in line.BodyEsItems) ProjectLineCalculator.CalculateBodyEsItem(bodyEsItem);

        ProjectLineCalculator.CalculateLine(
            line, revision.PricingMethod, revision.PricingRate, revision.RialShare, revision.ForeignShare,
            revision.SellingExchangeRateValue, revision.ReconciliationToleranceIrr);
    }

    public static void RecalculateRevision(ProjectRevision revision)
    {
        foreach (var line in revision.Lines) RecalculateLine(line, revision);
        ProjectTotalsCalculator.CalculateTotals(revision);
    }
}
