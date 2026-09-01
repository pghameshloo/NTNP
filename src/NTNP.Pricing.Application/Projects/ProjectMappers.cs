using NTNP.Pricing.Contracts.Projects;
using NTNP.Pricing.Domain.Calculation;
using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Application.Projects;

internal static class ProjectMappers
{
    public static ProjectDto ToDto(Project p) => new(
        p.Id, p.ProjectCode, p.ProjectName, p.CustomerId, p.Customer.CompanyName, p.RfqNumber, p.InquiryDate,
        p.QuotationNumber, p.QuotationDate, p.QuotationValidUntil, p.ProjectDescription, p.CommercialNotes,
        p.TechnicalNotes, p.QuotationCurrencyCode, p.RialShare, p.ForeignShare, p.PricingProfileId,
        p.PricingMethod.ToString(), p.PricingRate, p.Status.ToString(), p.CurrentRevisionNumber, p.CurrentRevisionId,
        p.CreatedByUserName, p.CreatedAtUtc, p.RowVersion);

    public static ProjectRevisionDto ToDto(ProjectRevision r)
    {
        var blockers = ProjectTotalsCalculator.GetApprovalBlockers(r);
        var totals = new ProjectRevisionTotalsDto(
            r.TotalEquipmentCostIrr, r.TotalBodyEsCostIrr, r.TotalOtherDirectCostIrr, r.TotalProjectCostIrr,
            r.TotalProjectSellingPriceIrr, r.TotalRialPayable, r.QuotationCurrencyCode, r.SellingExchangeRateValue,
            r.TotalForeignPayable, r.ProjectProfitIrr, r.ProjectGrossMargin, r.ReconciliationDifferenceIrr,
            r.ReconciliationPassed, blockers);

        return new ProjectRevisionDto(
            r.Id, r.ProjectId, r.RevisionNumber, r.Status.ToString(), r.QuotationCurrencyCode, r.RialShare,
            r.ForeignShare, r.PricingMethod.ToString(), r.PricingRate, r.SellingExchangeRateValue,
            r.SellingExchangeRateEffectiveAtUtc, r.Lines.OrderBy(l => l.LineNumber).Select(ToDto).ToList(), totals,
            null, r.SubmittedAtUtc, r.ApprovedByUserName, r.ApprovedAtUtc, r.RejectionReason, r.RowVersion);
    }

    public static ProjectLineDto ToDto(ProjectLine l) => new(
        l.Id, l.LineNumber, l.CellCode, l.PanelTemplateId, l.PanelTemplateCodeSnapshot, l.ProductFamilyNameSnapshot,
        l.PanelTypeNameSnapshot, l.Description, l.VoltageLevel, l.QuantityOfPanels, l.EquipmentCostPerPanel,
        l.BodyEsCostPerPanel, l.OtherDirectCostPerPanel, l.TotalCostPerPanel, l.TotalLineCost,
        l.ProjectRevision.PricingMethod.ToString(), l.PricingRateApplied, l.SellingPricePerPanel, l.TotalLineSellingPrice,
        l.RialShareApplied, l.RialPayableAmount, l.ProjectRevision.QuotationCurrencyCode, l.ForeignShareApplied,
        l.SellingExchangeRateApplied, l.ForeignPayableAmount, l.ProfitIrr, l.GrossMargin, l.ReconciliationPassed,
        l.ReconciliationDifferenceIrr, l.HasOverride, l.HasValidationErrors,
        l.BomItems.Select(ToDto).ToList(), l.BodyEsItems.Select(ToDto).ToList());

    public static ProjectLineBomItemDto ToDto(ProjectLineBomItem i) => new(
        i.Id, i.EquipmentCodeSnapshot, i.DescriptionSnapshot, i.PartNumberSnapshot, i.BrandSnapshot, i.ModelSnapshot,
        i.Unit, i.QuantityPerPanel, i.WastePercentage, i.AdjustedQuantityPerPanel, i.PurchaseCurrencyCodeSnapshot,
        i.PurchaseExchangeRateSnapshot, i.UnitCostIrrSnapshot, i.LineCostIrr, i.IsOverridden);

    public static ProjectLineBodyEsItemDto ToDto(ProjectLineBodyEsItem i) => new(
        i.Id, i.ComponentCodeSnapshot, i.DescriptionSnapshot, i.Unit, i.QuantityPerPanel, i.WastePercentage,
        i.AdjustedQuantityPerPanel, i.UnitCostIrrSnapshot, i.LineCostIrr, i.IsOverridden);
}
