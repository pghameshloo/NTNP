namespace NTNP.Pricing.Contracts.PricingProfiles;

public sealed record PricingProfileDto(
    Guid Id,
    string Name,
    string PricingMethod,
    decimal DefaultRate,
    decimal EquivalentMultiplier,
    decimal DefaultRialShare,
    decimal DefaultForeignShare,
    string DefaultQuotationCurrencyCode,
    string IrrRoundingPolicy,
    string ForeignRoundingPolicy,
    int ForeignDecimalPlaces,
    decimal ReconciliationToleranceIrr,
    bool IsActive,
    byte[] RowVersion);

public sealed record UpsertPricingProfileRequest(
    string Name,
    string PricingMethod,
    decimal DefaultRate,
    decimal DefaultRialShare,
    decimal DefaultForeignShare,
    string DefaultQuotationCurrencyCode,
    string IrrRoundingPolicy,
    string ForeignRoundingPolicy,
    int ForeignDecimalPlaces,
    decimal ReconciliationToleranceIrr,
    byte[]? RowVersion);
