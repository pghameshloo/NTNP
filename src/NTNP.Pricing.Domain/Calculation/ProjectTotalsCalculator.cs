using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Domain.Calculation;

/// <summary>
/// Section 19 — aggregates already-calculated <see cref="ProjectLine"/> rows (see
/// <see cref="ProjectLineCalculator"/>) into the project revision's TOTAL summary and the
/// blocking PASS/FAIL reconciliation control.
/// </summary>
public static class ProjectTotalsCalculator
{
    public static void CalculateTotals(ProjectRevision revision)
    {
        var lines = revision.Lines.ToList();

        revision.TotalEquipmentCostIrr = lines.Sum(l => l.EquipmentCostPerPanel * l.QuantityOfPanels);
        revision.TotalBodyEsCostIrr = lines.Sum(l => l.BodyEsCostPerPanel * l.QuantityOfPanels);
        revision.TotalOtherDirectCostIrr = lines.Sum(l => l.OtherDirectCostPerPanel * l.QuantityOfPanels);
        revision.TotalProjectCostIrr = lines.Sum(l => l.TotalLineCost);
        revision.TotalProjectSellingPriceIrr = lines.Sum(l => l.TotalLineSellingPrice);
        revision.TotalRialPayable = lines.Sum(l => l.RialPayableAmount);
        revision.TotalForeignPayable = lines.Sum(l => l.ForeignPayableAmount);

        var (profit, margin) = PricingCalculationEngine.CalculateProfitAndMargin(
            revision.TotalProjectSellingPriceIrr, revision.TotalProjectCostIrr);
        revision.ProjectProfitIrr = profit;
        revision.ProjectGrossMargin = margin;

        revision.ReconciliationDifferenceIrr = PricingCalculationEngine.CalculateReconciliationDifference(
            revision.TotalProjectSellingPriceIrr, revision.TotalRialPayable, revision.TotalForeignPayable, revision.SellingExchangeRateValue);
        revision.ReconciliationPassed =
            lines.Count > 0
            && lines.All(l => l.ReconciliationPassed && !l.HasValidationErrors)
            && PricingCalculationEngine.IsWithinTolerance(revision.ReconciliationDifferenceIrr, revision.ReconciliationToleranceIrr);
    }

    /// <summary>
    /// Section 19 — the closed set of conditions that block approval. Returns a human-readable
    /// reason for every blocking condition found (empty when the revision may be approved).
    /// </summary>
    public static IReadOnlyList<string> GetApprovalBlockers(ProjectRevision revision)
    {
        var blockers = new List<string>();

        if (revision.Lines.Count == 0)
            blockers.Add("Project revision has no panel lines.");

        if (revision.SellingExchangeRateValue <= 0)
            blockers.Add("Selling exchange rate is missing or zero.");

        try
        {
            PricingCalculationEngine.ValidateShares(revision.RialShare, revision.ForeignShare);
        }
        catch (Exceptions.DomainValidationException ex)
        {
            blockers.Add(ex.Message);
        }

        foreach (var line in revision.Lines)
        {
            if (line.HasValidationErrors)
                blockers.Add($"Line {line.LineNumber} ({line.CellCode}) has unresolved missing price/rate/template errors.");
            if (line.QuantityOfPanels <= 0)
                blockers.Add($"Line {line.LineNumber} ({line.CellCode}) has an invalid panel quantity.");
            if (!line.ReconciliationPassed)
                blockers.Add($"Line {line.LineNumber} ({line.CellCode}) fails reconciliation.");
            if (line.GrossMargin >= 1m)
                blockers.Add($"Line {line.LineNumber} ({line.CellCode}) has an invalid (>=100%) gross margin.");
        }

        if (!revision.ReconciliationPassed && revision.Lines.Count > 0)
            blockers.Add("Project-level reconciliation is outside the configured tolerance.");

        return blockers;
    }
}
