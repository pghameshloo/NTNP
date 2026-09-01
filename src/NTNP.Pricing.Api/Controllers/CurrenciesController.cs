using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Application.Currencies;
using NTNP.Pricing.Contracts.Currencies;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>Section 8 — Currency and Exchange Rate module.</summary>
[ApiController]
[Route("api/currencies")]
public sealed class CurrenciesController : ControllerBase
{
    private readonly ICurrencyService _service;

    public CurrenciesController(ICurrencyService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<IReadOnlyList<CurrencyDto>>> List([FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await _service.ListAsync(includeInactive, ct));

    [HttpPost]
    [Authorize(Policy = PolicyNames.ManageCurrencies)]
    public async Task<ActionResult<CurrencyDto>> Create(CreateCurrencyRequest request, CancellationToken ct) =>
        Ok(await _service.CreateAsync(request, ct));

    [HttpGet("{currencyId:guid}/rates")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<IReadOnlyList<ExchangeRateDto>>> RateHistory(Guid currencyId, CancellationToken ct) =>
        Ok(await _service.GetRateHistoryAsync(currencyId, ct));

    [HttpPost("rates")]
    [Authorize(Policy = PolicyNames.ManageCurrencies)]
    public async Task<ActionResult<ExchangeRateDto>> AddRate(CreateExchangeRateRequest request, CancellationToken ct) =>
        Ok(await _service.AddRateAsync(request, ct));
}
