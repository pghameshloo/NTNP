using NTNP.Pricing.Domain.Calculation;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Domain.Exceptions;

namespace NTNP.Pricing.Domain.Tests;

public class PricingCalculationEngineTests
{
    [Fact]
    public void EquipmentCost_Irr_Currency_Uses_RialUnitPrice_Directly()
    {
        var cost = PricingCalculationEngine.CalculateEquipmentFinalUnitCostIrr("IRR", null, 12_000_000m, null);
        Assert.Equal(12_000_000m, cost);
    }

    [Fact]
    public void EquipmentCost_Irr_Currency_With_NonZero_ForeignPrice_Throws()
    {
        Assert.Throws<DomainValidationException>(() =>
            PricingCalculationEngine.CalculateEquipmentFinalUnitCostIrr("IRR", 10m, 12_000_000m, null));
    }

    [Fact]
    public void EquipmentCost_ForeignCurrency_Without_Rate_Throws()
    {
        Assert.Throws<DomainValidationException>(() =>
            PricingCalculationEngine.CalculateEquipmentFinalUnitCostIrr("EUR", 100m, null, null));
    }

    [Fact]
    public void EquipmentCost_ForeignCurrency_Without_ForeignPrice_Throws()
    {
        Assert.Throws<DomainValidationException>(() =>
            PricingCalculationEngine.CalculateEquipmentFinalUnitCostIrr("EUR", null, null, 1_800_000m));
    }

    [Theory]
    [InlineData(10, 0.10, 100, 11, 1100)] // 10% waste
    [InlineData(5, 0, 200, 5, 1000)]      // no waste
    public void CalculateLine_Applies_Waste_Then_UnitCost(
        decimal qty, decimal waste, decimal unitCost, decimal expectedAdjustedQty, decimal expectedLineCost)
    {
        var (adjustedQty, lineCost) = PricingCalculationEngine.CalculateLine(qty, waste, unitCost);

        Assert.Equal(expectedAdjustedQty, adjustedQty);
        Assert.Equal(expectedLineCost, lineCost);
    }

    [Fact]
    public void CalculateLine_Negative_Quantity_Throws()
    {
        Assert.Throws<DomainValidationException>(() => PricingCalculationEngine.CalculateLine(-1, 0, 100));
    }

    [Fact]
    public void Markup_And_GrossMargin_Produce_Different_SellingPrices_For_Same_Rate()
    {
        const decimal cost = 1000m;
        const decimal rate = 0.30m;

        var markupPrice = PricingCalculationEngine.CalculateSellingPricePerPanel(cost, PricingMethod.Markup, rate);
        var marginPrice = PricingCalculationEngine.CalculateSellingPricePerPanel(cost, PricingMethod.GrossMargin, rate);

        Assert.Equal(1300m, markupPrice);
        Assert.Equal(1428.5714285714285714285714286m, marginPrice); // 1000 / 0.70
        Assert.NotEqual(markupPrice, marginPrice);
    }

    [Fact]
    public void GrossMargin_100Percent_Throws()
    {
        Assert.Throws<DomainValidationException>(() =>
            PricingCalculationEngine.CalculateSellingPricePerPanel(1000m, PricingMethod.GrossMargin, 1m));
    }

    [Theory]
    [InlineData(0.5, 0.5)]
    [InlineData(0.15, 0.85)]
    [InlineData(1.0, 0.0)]
    public void ValidateShares_Accepts_Shares_Totalling_100Percent(decimal rial, decimal foreign)
    {
        var ex = Record.Exception(() => PricingCalculationEngine.ValidateShares(rial, foreign));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(0.5, 0.4)]
    [InlineData(0.15, 0.90)]
    public void ValidateShares_Rejects_Shares_Not_Totalling_100Percent(decimal rial, decimal foreign)
    {
        Assert.Throws<DomainValidationException>(() => PricingCalculationEngine.ValidateShares(rial, foreign));
    }

    [Fact]
    public void CalculateRialForeignSplit_Zero_SellingRate_Throws()
    {
        Assert.Throws<DomainValidationException>(() =>
            PricingCalculationEngine.CalculateRialForeignSplit(1000m, 0.5m, 0.5m, 0m));
    }

    [Fact]
    public void CalculateTotalLineCost_NonPositive_PanelQuantity_Throws()
    {
        Assert.Throws<DomainValidationException>(() => PricingCalculationEngine.CalculateTotalLineCost(0, 1000m));
    }

    [Fact]
    public void CalculateProfitAndMargin_Zero_SellingPrice_Does_Not_DivideByZero()
    {
        var (profit, margin) = PricingCalculationEngine.CalculateProfitAndMargin(0m, 100m);

        Assert.Equal(-100m, profit);
        Assert.Equal(0m, margin);
    }
}
