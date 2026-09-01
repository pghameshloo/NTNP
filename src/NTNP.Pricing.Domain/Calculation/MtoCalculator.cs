using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Domain.Calculation;

/// <summary>One consolidated row of an Electrical/BODY+ES/Combined MTO (Section 16).</summary>
public sealed record MtoLine(
    string Code,
    string? PartNumber,
    string Description,
    string? Brand,
    string? Model,
    string Unit,
    decimal TotalRequiredQuantity,
    string? PurchaseCurrencyCode,
    decimal SnapshotUnitCostIrr,
    decimal TotalProcurementCostIrr,
    IReadOnlyList<string> RelatedPanelTypes,
    string Kind); // "Electrical" or "BodyEs"

/// <summary>
/// Section 16 — Automatic Consolidated MTO Generator.
/// <code>Required Equipment Quantity = SUM(BOM Quantity Per Panel × Number of Panels)</code>
/// Grouped by Equipment Code (electrical) / Component Code (BODY+ES) across every line of one
/// project revision. Uses the waste-adjusted per-panel quantity so the MTO reflects true
/// procurement need, consistent with the cost calculation in Sections 10/11.
/// </summary>
public static class MtoCalculator
{
    public static IReadOnlyList<MtoLine> CalculateElectricalMto(ProjectRevision revision)
    {
        return revision.Lines
            .SelectMany(line => line.BomItems.Select(item => (line, item)))
            .GroupBy(x => x.item.EquipmentCodeSnapshot)
            .Select(g =>
            {
                var first = g.First().item;
                var totalQty = g.Sum(x => x.item.AdjustedQuantityPerPanel * x.line.QuantityOfPanels);
                var totalCost = g.Sum(x => x.item.LineCostIrr * x.line.QuantityOfPanels);
                var panelTypes = g.Select(x => x.line.PanelTypeNameSnapshot).Distinct().ToList();
                return new MtoLine(
                    Code: g.Key,
                    PartNumber: first.PartNumberSnapshot,
                    Description: first.DescriptionSnapshot,
                    Brand: first.BrandSnapshot,
                    Model: first.ModelSnapshot,
                    Unit: first.Unit,
                    TotalRequiredQuantity: totalQty,
                    PurchaseCurrencyCode: first.PurchaseCurrencyCodeSnapshot,
                    SnapshotUnitCostIrr: first.UnitCostIrrSnapshot,
                    TotalProcurementCostIrr: totalCost,
                    RelatedPanelTypes: panelTypes,
                    Kind: "Electrical");
            })
            .OrderBy(m => m.Code)
            .ToList();
    }

    public static IReadOnlyList<MtoLine> CalculateBodyEsMto(ProjectRevision revision)
    {
        return revision.Lines
            .SelectMany(line => line.BodyEsItems.Select(item => (line, item)))
            .GroupBy(x => x.item.ComponentCodeSnapshot)
            .Select(g =>
            {
                var first = g.First().item;
                var totalQty = g.Sum(x => x.item.AdjustedQuantityPerPanel * x.line.QuantityOfPanels);
                var totalCost = g.Sum(x => x.item.LineCostIrr * x.line.QuantityOfPanels);
                var panelTypes = g.Select(x => x.line.PanelTypeNameSnapshot).Distinct().ToList();
                return new MtoLine(
                    Code: g.Key,
                    PartNumber: null,
                    Description: first.DescriptionSnapshot,
                    Brand: null,
                    Model: null,
                    Unit: first.Unit,
                    TotalRequiredQuantity: totalQty,
                    PurchaseCurrencyCode: "IRR",
                    SnapshotUnitCostIrr: first.UnitCostIrrSnapshot,
                    TotalProcurementCostIrr: totalCost,
                    RelatedPanelTypes: panelTypes,
                    Kind: "BodyEs");
            })
            .OrderBy(m => m.Code)
            .ToList();
    }

    public static IReadOnlyList<MtoLine> CalculateCombinedMto(ProjectRevision revision) =>
        CalculateElectricalMto(revision).Concat(CalculateBodyEsMto(revision)).OrderBy(m => m.Kind).ThenBy(m => m.Code).ToList();
}
