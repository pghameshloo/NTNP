namespace NTNP.Pricing.Contracts.Currencies;

public sealed record CurrencyDto(
    Guid Id, string Code, string Name, string Symbol, bool IsBaseCurrency, bool IsActive,
    ExchangeRateDto? LatestRate, byte[] RowVersion);

public sealed record CreateCurrencyRequest(string Code, string Name, string Symbol, bool IsBaseCurrency);

public sealed record ExchangeRateDto(
    Guid Id,
    Guid CurrencyId,
    string CurrencyCode,
    decimal PurchaseRateToIrr,
    decimal SellingRateToIrr,
    DateTimeOffset EffectiveAtUtc,
    string? RateSource,
    string? Notes,
    bool IsActive,
    bool IsStale,
    string CreatedByUserName,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateExchangeRateRequest(
    Guid CurrencyId,
    decimal PurchaseRateToIrr,
    decimal SellingRateToIrr,
    DateTimeOffset EffectiveAtUtc,
    string? RateSource,
    string? Notes);
