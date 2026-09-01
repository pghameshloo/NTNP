using NTNP.Pricing.Reporting.Models;

namespace NTNP.Pricing.Reporting.Tests;

/// <summary>Shared realistic test data for report-security and PDF-quality tests (Section 29).</summary>
internal static class ModelFactory
{
    public static CompanyBranding Company() => new(
        LegalNameEn: "Novin Tarh Niro Pars", LegalNameFa: "شرکت نوین طرح نیرو پارس",
        Address: "Tehran, Iran, Industrial Zone", Phone: "+98-21-11112222", Email: "info@ntnp.example",
        Website: "www.ntnp.example", LogoPngBytes: null,
        ConfidentialityLabelFa: "داخلی – محرمانه", ConfidentialityLabelEn: "INTERNAL – CONFIDENTIAL");

    /// <summary>A one-page quotation: a handful of lines with mixed Persian/English/numeric content.</summary>
    public static CustomerQuotationModel SmallQuotation(string languageCode = "fa") => BuildQuotation(languageCode, lineCount: 3);

    /// <summary>Section 29 PDF quality check #6: at least 30 panel lines (forces a multi-page document).</summary>
    public static CustomerQuotationModel LargeQuotation(string languageCode = "fa") => BuildQuotation(languageCode, lineCount: 32);

    private static CustomerQuotationModel BuildQuotation(string languageCode, int lineCount)
    {
        var lines = new List<CustomerQuotationLine>();
        for (var i = 1; i <= lineCount; i++)
        {
            lines.Add(new CustomerQuotationLine(
                Row: i,
                CellCode: $"C{i:D2}",
                // Persian long-description content only appears in fa/bilingual documents — an
                // English-only quotation would never carry it, and mixing scripts within a
                // single-language page confuses Chromium's PDF text-layer ordering for unrelated
                // text on the same page (observed empirically), which is itself a reason to keep
                // fa and en content on their own dedicated pages/sections rather than interleaved.
                PanelDescription: languageCode != "en" && i % 3 == 0
                    ? "تابلوی ورودی فشار متوسط با کلید هوایی خلاء قابل کشش و رله حفاظتی دیجیتال چندکاره برای کاربردهای صنعتی سنگین"
                    : $"UniSafe MV Withdrawable Panel — Incoming Feeder Type {i}",
                ProductFamily: i % 2 == 0 ? "UniSafe" : "UniGear ZS3.2",
                VoltageLevel: i % 2 == 0 ? "20 kV" : "33 kV",
                Quantity: 1 + i % 4,
                Unit: "EA",
                UnitSellingPrice: 1860.08m + i,
                TotalLinePrice: (1860.08m + i) * (1 + i % 4),
                Currency: "EUR"));
        }

        var totalForeign = lines.Sum(l => l.TotalLinePrice);
        var totalRial = totalForeign * 1_800_000m * (0.15m / 0.85m); // illustrative Rial portion at the 15/85 split basis

        return new CustomerQuotationModel(
            Company: Company(),
            LanguageCode: languageCode,
            QuotationTitle: languageCode == "en" ? "Technical & Commercial Proposal" : "پیشنهاد فنی و مالی",
            QuotationNumber: "Q-2026-0042",
            Revision: 1,
            IssueDate: new DateOnly(2026, 9, 1),
            ValidUntil: new DateOnly(2026, 10, 1),
            CustomerCompanyName: "Sample Industries Co.",
            ProjectName: "MV Switchgear Expansion Project",
            RfqNumber: "RFQ-2026-0001",
            ContactPerson: "Ali Rezaei",
            AttentionLine: "Procurement Department",
            Subject: languageCode == "en" ? "MV Switchgear Quotation" : "پیشنهاد قیمت تابلوهای فشار متوسط",
            QuotationCurrencyCode: "EUR",
            TotalRialPayable: Math.Round(totalRial, 0),
            TotalForeignPayable: Math.Round(totalForeign, 2),
            SellingRateBasisNote: "1 EUR = 1,800,000 IRR",
            Lines: lines,
            Terms: new CustomerCommercialTerms(
                DeliveryTerms: "EXW Tehran", DeliveryPeriod: "16 weeks ARO", DeliveryLocation: "NTNP Factory, Tehran",
                PaymentTerms: "30% advance, 70% before shipment", WarrantyTerms: "18 months from delivery",
                InspectionTerms: "FAT at manufacturer's premises", PackingTerms: "Seaworthy export packing",
                TransportationTerms: "By buyer", TaxesAndDutiesNote: "Excluded", CurrencyBasisNote: "EUR/IRR per Central Bank reference",
                ExchangeRateConditionsNote: "Rate fixed at quotation issue date", ScopeExclusions: "Civil works, cabling beyond panel terminals",
                TechnicalNotes: "All panels comply with IEC 62271-200.", CommercialNotes: "Prices valid for the quantities stated only."),
            Signatures: new SignatureBlock(
                PreparedByName: "Sara Ahmadi", PreparedByPosition: "Sales Engineer",
                CommercialManagerName: "Reza Karimi", CommercialManagerPosition: "Commercial Manager",
                ApprovedByName: "Mohammad Hosseini", ApprovedByPosition: "Approver",
                ManagingDirectorName: "Nima Jafari", ManagingDirectorPosition: "Managing Director",
                ShowCustomerAcceptance: true),
            ConfidentialityLabelVisible: false);
    }

    public static InternalCostingReportModel InternalCostingReport()
    {
        var lines = new List<InternalCostingLine>();
        for (var i = 1; i <= 5; i++)
        {
            var equipmentCost = 1_440_000_000m * i;
            var bodyEsCost = 320_000_000m;
            var totalCost = equipmentCost + bodyEsCost;
            var sellingPrice = totalCost * 1.30m;
            lines.Add(new InternalCostingLine(
                Row: i, CellCode: $"C{i:D2}", PanelType: "INCOMING", Description: $"Panel {i}", Quantity: 1,
                EquipmentCostPerPanel: equipmentCost, BodyEsCostPerPanel: bodyEsCost, OtherDirectCostPerPanel: 0,
                TotalCostPerPanel: totalCost, TotalLineCost: totalCost,
                PricingMethod: "Markup", PricingRate: 0.30m, SellingPricePerPanel: sellingPrice,
                TotalLineSellingPriceIrr: sellingPrice, RialShare: 0.15m, RialPayable: sellingPrice * 0.15m,
                QuotationCurrency: "EUR", ForeignShare: 0.85m, SellingExchangeRate: 1_800_000m,
                ForeignPayable: sellingPrice * 0.85m / 1_800_000m, Profit: sellingPrice - totalCost,
                GrossMargin: (sellingPrice - totalCost) / sellingPrice, ReconciliationPassed: true,
                HasOverride: i == 2, HasValidationErrors: false));
        }

        var totalCostAll = lines.Sum(l => l.TotalLineCost);
        var totalSellingAll = lines.Sum(l => l.TotalLineSellingPriceIrr);

        return new InternalCostingReportModel(
            Company: Company(), ProjectCode: "PRJ-0001", ProjectName: "MV Switchgear Expansion Project",
            CustomerName: "Sample Industries Co.", RevisionNumber: 1, RevisionStatus: "Draft",
            GeneratedAtUtc: DateTimeOffset.UtcNow, GeneratedByUserName: "test.user", Lines: lines,
            Totals: new InternalCostingTotals(
                lines.Sum(l => l.EquipmentCostPerPanel), lines.Sum(l => l.BodyEsCostPerPanel), 0,
                totalCostAll, totalSellingAll, lines.Sum(l => l.RialPayable), lines.Sum(l => l.ForeignPayable),
                totalSellingAll - totalCostAll, (totalSellingAll - totalCostAll) / totalSellingAll, true, Array.Empty<string>()));
    }
}
