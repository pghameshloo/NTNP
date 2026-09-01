using NTNP.Pricing.Contracts.Mto;
using NTNP.Pricing.Contracts.Projects;
using NTNP.Pricing.Contracts.Settings;
using NTNP.Pricing.Reporting.Models;

namespace NTNP.Pricing.Api.Reports;

/// <summary>
/// Composes the Api layer's Contracts DTOs into the Reporting project's report models. This is the
/// ONLY place internal DTO data is funneled toward a customer-facing model — <see cref="ToCustomerQuotationModel"/>
/// copies exactly the Section 26-approved fields and nothing else, by construction of
/// <see cref="CustomerQuotationModel"/>'s own shape (Section 26).
/// </summary>
public static class ReportModelMapper
{
    public static CompanyBranding ToBranding(CompanySettingsDto s, byte[]? logoBytes) => new(
        s.LegalNameEn, s.LegalNameFa, s.Address, s.Phone, s.Email, s.Website, logoBytes,
        s.ConfidentialityLabelFa, s.ConfidentialityLabelEn);

    public static CustomerQuotationModel ToCustomerQuotationModel(
        ProjectDto project, ProjectRevisionDto revision, CompanySettingsDto settings, byte[]? logoBytes, string languageCode)
    {
        var branding = ToBranding(settings, logoBytes);
        var title = languageCode == "en" ? settings.DefaultQuotationTitleEn : settings.DefaultQuotationTitleFa;

        var lines = revision.Lines.Select(l => new CustomerQuotationLine(
            Row: l.LineNumber,
            CellCode: l.CellCode,
            PanelDescription: l.Description,
            ProductFamily: l.ProductFamilyName,
            VoltageLevel: l.VoltageLevel,
            Quantity: l.QuantityOfPanels,
            Unit: "EA",
            UnitSellingPrice: l.SellingPricePerPanel,
            TotalLinePrice: l.TotalLineSellingPrice,
            Currency: l.QuotationCurrencyCode)).ToList();

        return new CustomerQuotationModel(
            Company: branding,
            LanguageCode: languageCode,
            QuotationTitle: title,
            QuotationNumber: project.QuotationNumber ?? project.ProjectCode,
            Revision: revision.RevisionNumber,
            IssueDate: project.QuotationDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            ValidUntil: project.QuotationValidUntil,
            CustomerCompanyName: project.CustomerName,
            ProjectName: project.ProjectName,
            RfqNumber: project.RfqNumber,
            ContactPerson: null,
            AttentionLine: null,
            Subject: project.ProjectName,
            QuotationCurrencyCode: revision.QuotationCurrencyCode,
            TotalRialPayable: revision.Totals.TotalRialPayable,
            TotalForeignPayable: revision.Totals.TotalForeignPayable,
            SellingRateBasisNote: $"1 {revision.QuotationCurrencyCode} = {revision.SellingExchangeRateValue:N0} IRR",
            Lines: lines,
            Terms: new CustomerCommercialTerms(
                DeliveryTerms: settings.DefaultDeliveryTerms, DeliveryPeriod: null, DeliveryLocation: null,
                PaymentTerms: settings.DefaultPaymentTerms, WarrantyTerms: settings.DefaultWarrantyTerms,
                InspectionTerms: settings.DefaultInspectionTerms, PackingTerms: settings.DefaultPackingTerms,
                TransportationTerms: settings.DefaultTransportationTerms, TaxesAndDutiesNote: settings.DefaultTaxesAndDutiesNote,
                CurrencyBasisNote: null, ExchangeRateConditionsNote: null, ScopeExclusions: settings.DefaultScopeExclusions,
                TechnicalNotes: project.TechnicalNotes, CommercialNotes: project.CommercialNotes),
            Signatures: new SignatureBlock(
                settings.PreparedByName, settings.PreparedByPosition, settings.CommercialManagerName, settings.CommercialManagerPosition,
                null, null, settings.ManagingDirectorName, settings.ManagingDirectorPosition, settings.EnableCustomerAcceptanceBlock),
            ConfidentialityLabelVisible: false);
    }

    public static InternalCostingReportModel ToInternalCostingReportModel(
        ProjectDto project, ProjectRevisionDto revision, CompanySettingsDto settings, byte[]? logoBytes, string generatedByUserName)
    {
        var branding = ToBranding(settings, logoBytes);
        var lines = revision.Lines.Select(l => new InternalCostingLine(
            l.LineNumber, l.CellCode, l.PanelTypeName, l.Description, l.QuantityOfPanels,
            l.EquipmentCostPerPanel, l.BodyEsCostPerPanel, l.OtherDirectCostPerPanel, l.TotalCostPerPanel, l.TotalLineCost,
            l.PricingMethod, l.PricingRateApplied, l.SellingPricePerPanel, l.TotalLineSellingPrice,
            l.RialShareApplied, l.RialPayableAmount, l.QuotationCurrencyCode, l.ForeignShareApplied,
            l.SellingExchangeRateApplied, l.ForeignPayableAmount, l.ProfitIrr, l.GrossMargin,
            l.ReconciliationPassed, l.HasOverride, l.HasValidationErrors)).ToList();

        var t = revision.Totals;
        return new InternalCostingReportModel(
            branding, project.ProjectCode, project.ProjectName, project.CustomerName, revision.RevisionNumber, revision.Status,
            DateTimeOffset.UtcNow, generatedByUserName, lines,
            new InternalCostingTotals(
                t.TotalEquipmentCostIrr, t.TotalBodyEsCostIrr, t.TotalOtherDirectCostIrr, t.TotalProjectCostIrr,
                t.TotalProjectSellingPriceIrr, t.TotalRialPayable, t.TotalForeignPayable, t.ProjectProfitIrr,
                t.ProjectGrossMargin, t.ReconciliationPassed, t.ApprovalBlockers));
    }

    public static BomMtoReportModel ToBomMtoReportModel(
        ProjectDto project, ProjectRevisionDto revision, CompanySettingsDto settings, byte[]? logoBytes,
        IReadOnlyList<MtoLineDto> rows, string title)
    {
        var branding = ToBranding(settings, logoBytes);
        var mapped = rows.Select(r => new BomMtoReportRow(
            r.Row, r.Code, r.PartNumber, r.Description, r.Brand, r.Model, r.Unit, r.TotalRequiredQuantity,
            r.PurchaseCurrencyCode, r.SnapshotUnitCostIrr, r.TotalProcurementCostIrr, r.RelatedPanelTypes, r.Notes)).ToList();

        return new BomMtoReportModel(
            branding, title, project.ProjectCode, project.ProjectName, revision.RevisionNumber,
            DateTimeOffset.UtcNow, mapped, mapped.Sum(r => r.TotalCostIrr));
    }

    public static RevisionComparisonReportModel ToRevisionComparisonReportModel(
        ProjectDto project, RevisionComparisonDto comparison, CompanySettingsDto settings, byte[]? logoBytes)
    {
        var branding = ToBranding(settings, logoBytes);
        var rows = comparison.ChangedFields.Select(c => new RevisionComparisonReportRow(c.CellCode, c.FieldName, c.OldValue, c.NewValue)).ToList();

        return new RevisionComparisonReportModel(
            branding, project.ProjectCode, project.ProjectName, comparison.FromRevisionNumber, comparison.ToRevisionNumber,
            comparison.CostDeltaIrr, comparison.SellingPriceDeltaIrr, comparison.ProfitDeltaIrr, comparison.GrossMarginDelta, rows);
    }
}
