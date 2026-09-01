using NTNP.Pricing.Contracts.Currencies;

namespace NTNP.Pricing.Application.Currencies;

public interface ICurrencyService
{
    Task<IReadOnlyList<CurrencyDto>> ListAsync(bool includeInactive, CancellationToken ct = default);
    Task<CurrencyDto> CreateAsync(CreateCurrencyRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ExchangeRateDto>> GetRateHistoryAsync(Guid currencyId, CancellationToken ct = default);
    Task<ExchangeRateDto> AddRateAsync(CreateExchangeRateRequest request, CancellationToken ct = default);
}
