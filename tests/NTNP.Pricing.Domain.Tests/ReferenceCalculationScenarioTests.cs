using NTNP.Pricing.Domain.Calculation;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Domain.Tests;

/// <summary>
/// Section 20 — MANDATORY REFERENCE CALCULATION TEST. Every number here is copied verbatim from
/// the master prompt's worked example and must match exactly (subject only to the explicit
/// tolerance on the never-terminating EUR payable / reconciliation division, per Section 20:
/// "explicit tolerances only where output rounding is intended").
///
/// Scenario: EUR purchase rate = EUR selling rate = 1,800,000 IRR; 85% foreign / 15% Rial split;
/// Markup pricing method at 30% (multiplier 1.30); one Air Circuit Breaker line (qty 2, 800 EUR
/// each) and one Relay line (qty 3, 50,000,000 IRR each), both on a single panel (quantity 1).
/// </summary>
public class ReferenceCalculationScenarioTests
{
    private const decimal EurPurchaseRate = 1_800_000m;
    private const decimal EurSellingRate = 1_800_000m;
    private const decimal ForeignShare = 0.85m;
    private const decimal RialShare = 0.15m;
    private const decimal Markup = 0.30m;

    private static ProjectRevision BuildScenarioRevision()
    {
        // Section 9: Final Unit Cost IRR for the Air Circuit Breaker (foreign-currency equipment).
        var acbUnitCostIrr = PricingCalculationEngine.CalculateEquipmentFinalUnitCostIrr(
            purchaseCurrencyCode: "EUR",
            foreignUnitPrice: 800m,
            rialUnitPrice: null,
            purchaseExchangeRate: EurPurchaseRate);

        // Section 9: Final Unit Cost IRR for the Relay (Rial equipment).
        var relayUnitCostIrr = PricingCalculationEngine.CalculateEquipmentFinalUnitCostIrr(
            purchaseCurrencyCode: "IRR",
            foreignUnitPrice: null,
            rialUnitPrice: 50_000_000m,
            purchaseExchangeRate: null);

        var acbItem = new ProjectLineBomItem
        {
            EquipmentCodeSnapshot = "ACB-001",
            DescriptionSnapshot = "Air Circuit Breaker",
            Unit = "EA",
            QuantityPerPanel = 2m,
            WastePercentage = 0m,
            PurchaseCurrencyCodeSnapshot = "EUR",
            PurchaseExchangeRateSnapshot = EurPurchaseRate,
            UnitCostIrrSnapshot = acbUnitCostIrr,
        };
        ProjectLineCalculator.CalculateBomItem(acbItem);

        var relayItem = new ProjectLineBomItem
        {
            EquipmentCodeSnapshot = "RLY-001",
            DescriptionSnapshot = "Relay",
            Unit = "EA",
            QuantityPerPanel = 3m,
            WastePercentage = 0m,
            PurchaseCurrencyCodeSnapshot = "IRR",
            PurchaseExchangeRateSnapshot = null,
            UnitCostIrrSnapshot = relayUnitCostIrr,
        };
        ProjectLineCalculator.CalculateBomItem(relayItem);

        var line = new ProjectLine
        {
            LineNumber = 1,
            CellCode = "C01",
            Description = "Reference scenario panel",
            QuantityOfPanels = 1m,
            OtherDirectCostPerPanel = 0m,
        };
        line.BomItems.Add(acbItem);
        line.BomItems.Add(relayItem);

        var revision = new ProjectRevision
        {
            RevisionNumber = 1,
            QuotationCurrencyCode = "EUR",
            RialShare = RialShare,
            ForeignShare = ForeignShare,
            PricingMethod = PricingMethod.Markup,
            PricingRate = Markup,
            SellingExchangeRateValue = EurSellingRate,
            ReconciliationToleranceIrr = 1m,
        };
        revision.Lines.Add(line);

        ProjectLineCalculator.CalculateLine(
            line, PricingMethod.Markup, Markup, RialShare, ForeignShare, EurSellingRate, revision.ReconciliationToleranceIrr);
        ProjectTotalsCalculator.CalculateTotals(revision);

        return revision;
    }

    [Fact]
    public void AirCircuitBreaker_UnitCost_Is_800Eur_Times_PurchaseRate()
    {
        var revision = BuildScenarioRevision();
        var acb = revision.Lines.Single().BomItems.Single(i => i.EquipmentCodeSnapshot == "ACB-001");

        Assert.Equal(1_440_000_000m, acb.UnitCostIrrSnapshot);
    }

    [Fact]
    public void AirCircuitBreaker_TotalCost_Is_QuantityTimesUnitCost()
    {
        var revision = BuildScenarioRevision();
        var acb = revision.Lines.Single().BomItems.Single(i => i.EquipmentCodeSnapshot == "ACB-001");

        Assert.Equal(2_880_000_000m, acb.LineCostIrr);
    }

    [Fact]
    public void Relay_TotalCost_Is_QuantityTimesUnitCost()
    {
        var revision = BuildScenarioRevision();
        var relay = revision.Lines.Single().BomItems.Single(i => i.EquipmentCodeSnapshot == "RLY-001");

        Assert.Equal(150_000_000m, relay.LineCostIrr);
    }

    [Fact]
    public void TotalProjectCost_Is_SumOfItemCosts()
    {
        var revision = BuildScenarioRevision();

        Assert.Equal(3_030_000_000m, revision.TotalProjectCostIrr);
    }

    [Fact]
    public void TotalSellingPrice_Is_Cost_Times_130Percent_Markup()
    {
        var revision = BuildScenarioRevision();

        Assert.Equal(3_939_000_000m, revision.TotalProjectSellingPriceIrr);
    }

    [Fact]
    public void RialPayable_Is_15Percent_Of_SellingPrice()
    {
        var revision = BuildScenarioRevision();

        Assert.Equal(590_850_000m, revision.TotalRialPayable);
    }

    [Fact]
    public void ForeignPayable_Is_85Percent_Of_SellingPrice_Converted_At_SellingRate()
    {
        var revision = BuildScenarioRevision();

        // 1,860.083333... EUR — an infinitely repeating decimal; assert to 6 dp per Section 20's
        // "explicit tolerances only where output rounding is intended".
        Assert.Equal(1_860.083333m, Math.Round(revision.TotalForeignPayable, 6));
    }

    [Fact]
    public void Profit_Is_SellingPrice_Minus_Cost()
    {
        var revision = BuildScenarioRevision();

        Assert.Equal(909_000_000m, revision.ProjectProfitIrr);
    }

    [Fact]
    public void Markup_Is_30Percent_And_Distinct_From_GrossMargin()
    {
        var revision = BuildScenarioRevision();
        var line = revision.Lines.Single();

        Assert.Equal(0.30m, line.PricingRateApplied); // the configured markup
        Assert.Equal(0.230769m, Math.Round(revision.ProjectGrossMargin, 6)); // 23.0769...% gross margin — NOT 30%
        Assert.NotEqual(line.PricingRateApplied, revision.ProjectGrossMargin);
    }

    [Fact]
    public void Reconciliation_Passes_Within_Tolerance()
    {
        var revision = BuildScenarioRevision();

        // Reconciliation Difference = TotalSellingPrice - (RialPayable + ForeignPayable × SellingRate)
        // Must be (algebraically) zero; the ForeignPayable division is non-terminating so the
        // decimal difference is a negligible epsilon, well inside the 1 IRR tolerance.
        Assert.True(Math.Abs(revision.ReconciliationDifferenceIrr) < 0.001m,
            $"Reconciliation difference {revision.ReconciliationDifferenceIrr} exceeds negligible epsilon.");
        Assert.True(revision.ReconciliationPassed);
    }

    [Fact]
    public void ApprovalBlockers_Is_Empty_For_A_Valid_Reconciled_Scenario()
    {
        var revision = BuildScenarioRevision();

        var blockers = ProjectTotalsCalculator.GetApprovalBlockers(revision);

        Assert.Empty(blockers);
    }
}
