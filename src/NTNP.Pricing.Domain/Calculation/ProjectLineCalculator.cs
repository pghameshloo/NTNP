using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Domain.Calculation;

/// <summary>
/// Orchestrates <see cref="PricingCalculationEngine"/> over a single <see cref="ProjectLine"/> and
/// its BOM/BODY+ES snapshot items, writing every computed field required by Sections 14/17/18/19
/// back onto the entity. Called by the Application-layer BOM/MTO generator immediately after BOM
/// items are (re)populated, and again whenever pricing settings change.
/// </summary>
public static class ProjectLineCalculator
{
    /// <summary>Section 10 formula, applied to one equipment BOM snapshot line.</summary>
    public static void CalculateBomItem(ProjectLineBomItem item)
    {
        var (adjustedQuantity, lineCost) = PricingCalculationEngine.CalculateLine(
            item.QuantityPerPanel, item.WastePercentage, item.UnitCostIrrSnapshot);
        item.AdjustedQuantityPerPanel = adjustedQuantity;
        item.LineCostIrr = lineCost;
    }

    /// <summary>Section 11 formula, applied to one BODY+ES snapshot line.</summary>
    public static void CalculateBodyEsItem(ProjectLineBodyEsItem item)
    {
        var (adjustedQuantity, lineCost) = PricingCalculationEngine.CalculateLine(
            item.QuantityPerPanel, item.WastePercentage, item.UnitCostIrrSnapshot);
        item.AdjustedQuantityPerPanel = adjustedQuantity;
        item.LineCostIrr = lineCost;
    }

    /// <summary>
    /// Full Section 14/17/18/19 line-level calculation. Assumes <see cref="ProjectLine.BomItems"/>
    /// and <see cref="ProjectLine.BodyEsItems"/> already carry correct <c>LineCostIrr</c> values
    /// (via <see cref="CalculateBomItem"/>/<see cref="CalculateBodyEsItem"/>) and that
    /// <see cref="ProjectLine.OtherDirectCostPerPanel"/> has already been set (default 0 — see
    /// ASSUMPTIONS.md §4).
    /// </summary>
    public static void CalculateLine(
        ProjectLine line,
        PricingMethod pricingMethod,
        decimal pricingRate,
        decimal rialShare,
        decimal foreignShare,
        decimal sellingExchangeRate,
        decimal reconciliationToleranceIrr)
    {
        line.EquipmentCostPerPanel = PricingCalculationEngine.SumLineCosts(line.BomItems.Select(i => i.LineCostIrr));
        line.BodyEsCostPerPanel = PricingCalculationEngine.SumLineCosts(line.BodyEsItems.Select(i => i.LineCostIrr));

        line.TotalCostPerPanel = PricingCalculationEngine.CalculateTotalCostPerPanel(
            line.EquipmentCostPerPanel, line.BodyEsCostPerPanel, line.OtherDirectCostPerPanel);

        line.TotalLineCost = PricingCalculationEngine.CalculateTotalLineCost(line.QuantityOfPanels, line.TotalCostPerPanel);

        line.PricingRateApplied = pricingRate;
        line.SellingPricePerPanel = PricingCalculationEngine.CalculateSellingPricePerPanel(
            line.TotalCostPerPanel, pricingMethod, pricingRate);

        line.TotalLineSellingPrice = PricingCalculationEngine.CalculateTotalLineSellingPrice(
            line.QuantityOfPanels, line.SellingPricePerPanel);

        var (rialPayable, _, foreignPayable) = PricingCalculationEngine.CalculateRialForeignSplit(
            line.TotalLineSellingPrice, rialShare, foreignShare, sellingExchangeRate);

        line.RialShareApplied = rialShare;
        line.RialPayableAmount = rialPayable;
        line.ForeignShareApplied = foreignShare;
        line.SellingExchangeRateApplied = sellingExchangeRate;
        line.ForeignPayableAmount = foreignPayable;

        var (profit, margin) = PricingCalculationEngine.CalculateProfitAndMargin(line.TotalLineSellingPrice, line.TotalLineCost);
        line.ProfitIrr = profit;
        line.GrossMargin = margin;

        line.ReconciliationDifferenceIrr = PricingCalculationEngine.CalculateReconciliationDifference(
            line.TotalLineSellingPrice, rialPayable, foreignPayable, sellingExchangeRate);
        line.ReconciliationPassed = PricingCalculationEngine.IsWithinTolerance(line.ReconciliationDifferenceIrr, reconciliationToleranceIrr);
    }
}
