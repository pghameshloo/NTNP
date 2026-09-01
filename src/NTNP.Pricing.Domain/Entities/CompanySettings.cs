using NTNP.Pricing.Domain.Common;

namespace NTNP.Pricing.Domain.Entities;

/// <summary>
/// Section 26/33 — single-row company/branding/report configuration table (see ASSUMPTIONS.md §9).
/// Everything a customer quotation's header/footer/signature block needs must be editable here
/// without a source-code change.
/// </summary>
public class CompanySettings : Entity
{
    public string LegalNameEn { get; set; } = "Novin Tarh Niro Pars";
    public string LegalNameFa { get; set; } = "شرکت نوین طرح نیرو پارس";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoStoragePath { get; set; }
    public string? StampImageStoragePath { get; set; }

    public string DefaultQuotationTitleFa { get; set; } = "پیشنهاد فنی و مالی";
    public string DefaultQuotationTitleEn { get; set; } = "Technical & Commercial Proposal";
    public string ConfidentialityLabelFa { get; set; } = "داخلی – محرمانه";
    public string ConfidentialityLabelEn { get; set; } = "INTERNAL – CONFIDENTIAL";

    public string? DefaultDeliveryTerms { get; set; }
    public string? DefaultPaymentTerms { get; set; }
    public string? DefaultWarrantyTerms { get; set; }
    public string? DefaultInspectionTerms { get; set; }
    public string? DefaultPackingTerms { get; set; }
    public string? DefaultTransportationTerms { get; set; }
    public string? DefaultTaxesAndDutiesNote { get; set; }
    public string? DefaultScopeExclusions { get; set; }

    public string PreparedByName { get; set; } = string.Empty;
    public string PreparedByPosition { get; set; } = string.Empty;
    public string CommercialManagerName { get; set; } = string.Empty;
    public string CommercialManagerPosition { get; set; } = string.Empty;
    public string ManagingDirectorName { get; set; } = string.Empty;
    public string ManagingDirectorPosition { get; set; } = string.Empty;

    public bool EnableCustomerAcceptanceBlock { get; set; } = true;
    public int StaleExchangeRateDays { get; set; } = 7;

    public Guid? UpdatedByUserId { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
