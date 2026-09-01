namespace NTNP.Pricing.Reporting.Models;

/// <summary>
/// Section 26 — the ONLY data available to the customer-facing quotation renderer. This is a
/// deliberately separate, minimal type from any internal report model: it has no property that
/// could carry purchase price, purchase exchange rate, internal BOM/BODY+ES cost, multiplier,
/// markup, gross margin, profit, supplier, price-source, internal notes, override reasons or audit
/// data, so that data can never leak into a customer document even by accident — there is simply no
/// field to put it in.
/// </summary>
public sealed record CustomerQuotationModel(
    CompanyBranding Company,
    string LanguageCode, // "fa", "en", or "bilingual"
    string QuotationTitle,
    string QuotationNumber,
    int Revision,
    DateOnly IssueDate,
    DateOnly? ValidUntil,
    string CustomerCompanyName,
    string ProjectName,
    string? RfqNumber,
    string? ContactPerson,
    string? AttentionLine,
    string? Subject,
    string QuotationCurrencyCode,
    decimal TotalRialPayable,
    decimal TotalForeignPayable,
    string? SellingRateBasisNote,
    IReadOnlyList<CustomerQuotationLine> Lines,
    CustomerCommercialTerms Terms,
    SignatureBlock Signatures,
    bool ConfidentialityLabelVisible);

public sealed record CustomerQuotationLine(
    int Row,
    string CellCode,
    string PanelDescription,
    string ProductFamily,
    string? VoltageLevel,
    decimal Quantity,
    string Unit,
    decimal UnitSellingPrice,
    decimal TotalLinePrice,
    string Currency);

/// <summary>Only non-null terms are printed; Section 26 forbids printing an empty heading.</summary>
public sealed record CustomerCommercialTerms(
    string? DeliveryTerms,
    string? DeliveryPeriod,
    string? DeliveryLocation,
    string? PaymentTerms,
    string? WarrantyTerms,
    string? InspectionTerms,
    string? PackingTerms,
    string? TransportationTerms,
    string? TaxesAndDutiesNote,
    string? CurrencyBasisNote,
    string? ExchangeRateConditionsNote,
    string? ScopeExclusions,
    string? TechnicalNotes,
    string? CommercialNotes);

public sealed record SignatureBlock(
    string PreparedByName, string PreparedByPosition,
    string CommercialManagerName, string CommercialManagerPosition,
    string? ApprovedByName, string? ApprovedByPosition,
    string ManagingDirectorName, string ManagingDirectorPosition,
    bool ShowCustomerAcceptance);

public sealed record CompanyBranding(
    string LegalNameEn, string LegalNameFa, string? Address, string? Phone, string? Email, string? Website,
    byte[]? LogoPngBytes, string ConfidentialityLabelFa, string ConfidentialityLabelEn);
