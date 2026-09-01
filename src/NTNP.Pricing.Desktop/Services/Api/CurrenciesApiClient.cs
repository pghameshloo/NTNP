using NTNP.Pricing.Contracts.Currencies;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class CurrenciesApiClient : ApiClientBase
{
    public CurrenciesApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<IReadOnlyList<CurrencyDto>> ListAsync(bool includeInactive = false, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<CurrencyDto>>($"api/currencies?includeInactive={includeInactive}", ct);

    public Task<CurrencyDto> CreateAsync(CreateCurrencyRequest request, CancellationToken ct = default) => PostAsync<CurrencyDto>("api/currencies", request, ct);

    public Task<IReadOnlyList<ExchangeRateDto>> GetRateHistoryAsync(Guid currencyId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ExchangeRateDto>>($"api/currencies/{currencyId}/rates", ct);

    public Task<ExchangeRateDto> AddRateAsync(CreateExchangeRateRequest request, CancellationToken ct = default) => PostAsync<ExchangeRateDto>("api/currencies/rates", request, ct);
}
