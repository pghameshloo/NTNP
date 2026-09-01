namespace NTNP.Pricing.Contracts.Settings;

public sealed record CompanySettingsDto(
    string LegalNameEn, string LegalNameFa, string? Address, string? Phone, string? Email, string? Website,
    string? LogoStoragePath, string? StampImageStoragePath,
    string DefaultQuotationTitleFa, string DefaultQuotationTitleEn,
    string ConfidentialityLabelFa, string ConfidentialityLabelEn,
    string? DefaultDeliveryTerms, string? DefaultPaymentTerms, string? DefaultWarrantyTerms,
    string? DefaultInspectionTerms, string? DefaultPackingTerms, string? DefaultTransportationTerms,
    string? DefaultTaxesAndDutiesNote, string? DefaultScopeExclusions,
    string PreparedByName, string PreparedByPosition,
    string CommercialManagerName, string CommercialManagerPosition,
    string ManagingDirectorName, string ManagingDirectorPosition,
    bool EnableCustomerAcceptanceBlock, int StaleExchangeRateDays);

public sealed record UpdateCompanySettingsRequest(CompanySettingsDto Settings);

public sealed record ServerConnectionSettingsDto(string ApiBaseUrl);
