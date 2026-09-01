using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Exceptions;
using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Application.Projects;

/// <summary>
/// Section 15 steps 1-5: copy a panel template's BOM (and BODY+ES, if linked) into a fresh,
/// immutable <see cref="ProjectLine"/> snapshot, resolving each equipment item's current master
/// price/purchase-rate at build time. Shared by <see cref="ProjectLineService"/> (adding a line) and
/// <see cref="ProjectRevisionService"/> ("Create New Revision Using Latest Prices").
/// </summary>
public sealed class BomSnapshotBuilder
{
    private readonly IApplicationDbContext _db;

    public BomSnapshotBuilder(IApplicationDbContext db) => _db = db;

    public async Task<ProjectLine> BuildAsync(
        Guid panelTemplateId, string cellCode, decimal quantityOfPanels, decimal otherDirectCostPerPanel,
        int lineNumber, CancellationToken ct)
    {
        var template = await _db.PanelTemplates
            .Include(t => t.ProductFamily).Include(t => t.PanelType).Include(t => t.BodyEsTemplate).ThenInclude(b => b!.Items)
            .Include(t => t.BomItems).ThenInclude(i => i.Equipment).ThenInclude(e => e.Prices)
            .FirstOrDefaultAsync(t => t.Id == panelTemplateId, ct)
            ?? throw new NotFoundException(nameof(PanelTemplate), panelTemplateId);

        var line = new ProjectLine
        {
            LineNumber = lineNumber,
            CellCode = cellCode,
            PanelTemplateId = template.Id,
            PanelTemplateCodeSnapshot = template.TemplateCode,
            PanelTemplateRevisionSnapshot = template.RevisionNumber,
            ProductFamilyNameSnapshot = template.ProductFamily.Name,
            PanelTypeNameSnapshot = template.PanelType.Name,
            Description = template.TemplateName,
            VoltageLevel = template.VoltageLevel,
            QuantityOfPanels = quantityOfPanels,
            OtherDirectCostPerPanel = otherDirectCostPerPanel,
        };

        var hasErrors = false;

        foreach (var templateItem in template.BomItems)
        {
            var currentPrice = templateItem.Equipment.CurrentPrice;
            if (currentPrice is null) hasErrors = true;

            line.BomItems.Add(new ProjectLineBomItem
            {
                EquipmentId = templateItem.EquipmentId,
                EquipmentCodeSnapshot = templateItem.Equipment.Code,
                DescriptionSnapshot = templateItem.Equipment.DescriptionEn,
                PartNumberSnapshot = templateItem.Equipment.TechnicalPartNumber,
                BrandSnapshot = templateItem.Equipment.Brand,
                ModelSnapshot = templateItem.Equipment.Model,
                Unit = templateItem.Unit,
                QuantityPerPanel = templateItem.QuantityPerPanel,
                WastePercentage = templateItem.WastePercentage,
                EquipmentPriceId = currentPrice?.Id,
                PurchaseCurrencyCodeSnapshot = currentPrice?.PurchaseCurrencyCode ?? "IRR",
                PurchaseExchangeRateSnapshot = currentPrice?.PurchaseExchangeRateSnapshot,
                UnitCostIrrSnapshot = currentPrice?.FinalUnitCostIrr ?? 0m,
                Notes = currentPrice is null ? "MISSING PRICE — Equipment Database has no active price for this item." : null,
            });
        }

        if (template.BodyEsTemplate is not null)
        {
            foreach (var templateItem in template.BodyEsTemplate.Items)
            {
                line.BodyEsItems.Add(new ProjectLineBodyEsItem
                {
                    BodyEsTemplateItemId = templateItem.Id,
                    ComponentCodeSnapshot = templateItem.ComponentCode,
                    DescriptionSnapshot = templateItem.DescriptionFa,
                    Unit = templateItem.Unit,
                    QuantityPerPanel = templateItem.QuantityPerPanel,
                    WastePercentage = templateItem.WastePercentage,
                    UnitCostIrrSnapshot = templateItem.UnitCostIrr,
                });
            }
        }

        line.HasValidationErrors = hasErrors;
        return line;
    }
}
