using NTNP.Pricing.Domain.Calculation;
using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Domain.Tests;

public class MtoCalculatorTests
{
    private static ProjectRevision BuildTwoLineRevision()
    {
        // Same equipment code (ACB-001) used on two different panel lines, with different panel
        // quantities — the consolidated MTO must sum the two into one row (Section 16).
        var line1 = new ProjectLine { LineNumber = 1, CellCode = "C01", PanelTypeNameSnapshot = "INCOMING", QuantityOfPanels = 2 };
        var acb1 = new ProjectLineBomItem
        {
            EquipmentCodeSnapshot = "ACB-001", DescriptionSnapshot = "Air Circuit Breaker", Unit = "EA",
            QuantityPerPanel = 1, WastePercentage = 0, UnitCostIrrSnapshot = 1_440_000_000m,
        };
        ProjectLineCalculator.CalculateBomItem(acb1);
        line1.BomItems.Add(acb1);

        var line2 = new ProjectLine { LineNumber = 2, CellCode = "C02", PanelTypeNameSnapshot = "OUTGOING", QuantityOfPanels = 3 };
        var acb2 = new ProjectLineBomItem
        {
            EquipmentCodeSnapshot = "ACB-001", DescriptionSnapshot = "Air Circuit Breaker", Unit = "EA",
            QuantityPerPanel = 1, WastePercentage = 0, UnitCostIrrSnapshot = 1_440_000_000m,
        };
        ProjectLineCalculator.CalculateBomItem(acb2);
        line2.BomItems.Add(acb2);

        var revision = new ProjectRevision();
        revision.Lines.Add(line1);
        revision.Lines.Add(line2);
        return revision;
    }

    [Fact]
    public void ElectricalMto_Consolidates_SameEquipmentCode_Across_Lines()
    {
        var revision = BuildTwoLineRevision();

        var mto = MtoCalculator.CalculateElectricalMto(revision);

        var row = Assert.Single(mto);
        Assert.Equal("ACB-001", row.Code);
        // 1 per panel × 2 panels + 1 per panel × 3 panels = 5
        Assert.Equal(5m, row.TotalRequiredQuantity);
        Assert.Equal(2, row.RelatedPanelTypes.Count);
        Assert.Contains("INCOMING", row.RelatedPanelTypes);
        Assert.Contains("OUTGOING", row.RelatedPanelTypes);
    }

    [Fact]
    public void ElectricalMto_TotalProcurementCost_Sums_Correctly()
    {
        var revision = BuildTwoLineRevision();

        var mto = MtoCalculator.CalculateElectricalMto(revision);

        Assert.Equal(5m * 1_440_000_000m, mto.Single().TotalProcurementCostIrr);
    }
}
