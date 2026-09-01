using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Domain.Calculation;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Infrastructure.Persistence;

namespace NTNP.Pricing.Infrastructure.Seed;

/// <summary>
/// Section 37 — "One sample pricing project using the mandatory calculation scenario" (Section 20).
/// Depends on <see cref="MasterDataSeeder"/> having already run.
/// </summary>
public static class SampleProjectSeeder
{
    private const string SeedUserName = "system-seed";
    private static readonly Guid SeedUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private const string ProjectCode = "PRJ-0001";

    public static async Task SeedAsync(ApplicationDbContext db, SeedResult masterData, CancellationToken ct = default)
    {
        if (await db.Projects.AnyAsync(p => p.ProjectCode == ProjectCode, ct)) return;

        var project = new Project
        {
            ProjectCode = ProjectCode,
            ProjectName = "Sample Industries — MV Switchgear Quotation",
            CustomerId = masterData.Customer.Id,
            RfqNumber = "RFQ-2026-0001",
            InquiryDate = DateOnly.FromDateTime(DateTime.UtcNow),
            QuotationCurrencyCode = "EUR",
            RialShare = 0.15m,
            ForeignShare = 0.85m,
            PricingProfileId = masterData.PricingProfile.Id,
            PricingMethod = PricingMethod.Markup,
            PricingRate = 0.30m,
            Status = ProjectStatus.Draft,
            CurrentRevisionNumber = 1,
            CreatedByUserId = SeedUserId,
            CreatedByUserName = SeedUserName,
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        var eurRate = await db.ExchangeRates
            .Where(r => r.Currency.Code == "EUR" && r.IsActive)
            .OrderByDescending(r => r.EffectiveAtUtc)
            .FirstAsync(ct);

        var revision = new ProjectRevision
        {
            ProjectId = project.Id,
            RevisionNumber = 1,
            Status = ProjectStatus.Draft,
            QuotationCurrencyCode = "EUR",
            RialShare = 0.15m,
            ForeignShare = 0.85m,
            PricingMethod = PricingMethod.Markup,
            PricingRate = 0.30m,
            IrrRoundingPolicy = RoundingMode.NearestThousand,
            ForeignRoundingPolicy = RoundingMode.NearestInteger,
            ForeignDecimalPlaces = 2,
            ReconciliationToleranceIrr = 1m,
            SellingExchangeRateId = eurRate.Id,
            SellingExchangeRateValue = eurRate.SellingRateToIrr,
            SellingExchangeRateEffectiveAtUtc = eurRate.EffectiveAtUtc,
            CreatedByUserId = SeedUserId,
            CreatedByUserName = SeedUserName,
        };

        var acbPrice = await db.EquipmentPrices.Where(p => p.EquipmentId == masterData.Acb.Id).OrderByDescending(p => p.EffectiveAtUtc).FirstAsync(ct);
        var relayPrice = await db.EquipmentPrices.Where(p => p.EquipmentId == masterData.Relay.Id).OrderByDescending(p => p.EffectiveAtUtc).FirstAsync(ct);

        var line = new ProjectLine
        {
            LineNumber = 1,
            CellCode = "C01",
            PanelTemplateId = masterData.PanelTemplate.Id,
            PanelTemplateCodeSnapshot = masterData.PanelTemplate.TemplateCode,
            PanelTemplateRevisionSnapshot = masterData.PanelTemplate.RevisionNumber,
            ProductFamilyNameSnapshot = masterData.UniSafe.Name,
            PanelTypeNameSnapshot = masterData.IncomingPanelType.Name,
            Description = masterData.PanelTemplate.TemplateName,
            VoltageLevel = masterData.PanelTemplate.VoltageLevel,
            QuantityOfPanels = 1m,
            OtherDirectCostPerPanel = 0m,
        };

        var acbItem = new ProjectLineBomItem
        {
            EquipmentId = masterData.Acb.Id,
            EquipmentCodeSnapshot = masterData.Acb.Code,
            DescriptionSnapshot = masterData.Acb.DescriptionEn,
            BrandSnapshot = masterData.Acb.Brand,
            ModelSnapshot = masterData.Acb.Model,
            Unit = masterData.Acb.Unit,
            QuantityPerPanel = 2m,
            WastePercentage = 0m,
            EquipmentPriceId = acbPrice.Id,
            PurchaseCurrencyCodeSnapshot = acbPrice.PurchaseCurrencyCode,
            PurchaseExchangeRateSnapshot = acbPrice.PurchaseExchangeRateSnapshot,
            UnitCostIrrSnapshot = acbPrice.FinalUnitCostIrr,
        };
        ProjectLineCalculator.CalculateBomItem(acbItem);
        line.BomItems.Add(acbItem);

        var relayItem = new ProjectLineBomItem
        {
            EquipmentId = masterData.Relay.Id,
            EquipmentCodeSnapshot = masterData.Relay.Code,
            DescriptionSnapshot = masterData.Relay.DescriptionEn,
            BrandSnapshot = masterData.Relay.Brand,
            ModelSnapshot = masterData.Relay.Model,
            Unit = masterData.Relay.Unit,
            QuantityPerPanel = 3m,
            WastePercentage = 0m,
            EquipmentPriceId = relayPrice.Id,
            PurchaseCurrencyCodeSnapshot = relayPrice.PurchaseCurrencyCode,
            PurchaseExchangeRateSnapshot = relayPrice.PurchaseExchangeRateSnapshot,
            UnitCostIrrSnapshot = relayPrice.FinalUnitCostIrr,
        };
        ProjectLineCalculator.CalculateBomItem(relayItem);
        line.BomItems.Add(relayItem);

        ProjectLineCalculator.CalculateLine(
            line, PricingMethod.Markup, 0.30m, 0.15m, 0.85m, eurRate.SellingRateToIrr, revision.ReconciliationToleranceIrr);

        revision.Lines.Add(line);
        ProjectTotalsCalculator.CalculateTotals(revision);

        db.ProjectRevisions.Add(revision);
        await db.SaveChangesAsync(ct);

        project.CurrentRevisionId = revision.Id;
        await db.SaveChangesAsync(ct);
    }
}
