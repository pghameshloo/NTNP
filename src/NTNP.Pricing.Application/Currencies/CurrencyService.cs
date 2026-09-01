using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Exceptions;
using NTNP.Pricing.Contracts.Currencies;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Domain.Exceptions;

namespace NTNP.Pricing.Application.Currencies;

/// <summary>Section 8 — Currency + Exchange Rate module. Purchase and selling rates are independent
/// and every rate is an immutable, effective-dated history row (never overwritten).</summary>
public sealed class CurrencyService : ICurrencyService
{
    private const int DefaultStaleDays = 7;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public CurrencyService(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<IReadOnlyList<CurrencyDto>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var q = _db.Currencies.AsNoTracking().Include(c => c.Rates).AsQueryable();
        if (!includeInactive) q = q.Where(c => c.IsActive);

        var currencies = await q.ToListAsync(ct);
        var staleDays = await GetStaleDaysAsync(ct);

        return currencies.Select(c => ToDto(c, staleDays)).OrderBy(c => c.Code).ToList();
    }

    public async Task<CurrencyDto> CreateAsync(CreateCurrencyRequest request, CancellationToken ct = default)
    {
        if (await _db.Currencies.AnyAsync(c => c.Code == request.Code, ct))
            throw new DomainValidationException($"Currency code '{request.Code}' already exists.");

        var currency = new Currency
        {
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name,
            Symbol = request.Symbol,
            IsBaseCurrency = request.IsBaseCurrency,
            CreatedByUserId = _currentUser.UserId,
            CreatedByUserName = _currentUser.UserName,
        };
        _db.Currencies.Add(currency);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Created, nameof(Currency), currency.Id.ToString(), newValue: currency, cancellationToken: ct);

        return ToDto(currency, DefaultStaleDays);
    }

    public async Task<IReadOnlyList<ExchangeRateDto>> GetRateHistoryAsync(Guid currencyId, CancellationToken ct = default)
    {
        var currency = await _db.Currencies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == currencyId, ct)
            ?? throw new NotFoundException(nameof(Currency), currencyId);

        var rates = await _db.ExchangeRates.AsNoTracking()
            .Where(r => r.CurrencyId == currencyId)
            .OrderByDescending(r => r.EffectiveAtUtc)
            .ToListAsync(ct);

        var staleDays = await GetStaleDaysAsync(ct);
        return rates.Select(r => ToDto(r, currency.Code, staleDays)).ToList();
    }

    public async Task<ExchangeRateDto> AddRateAsync(CreateExchangeRateRequest request, CancellationToken ct = default)
    {
        var currency = await _db.Currencies.FirstOrDefaultAsync(c => c.Id == request.CurrencyId, ct)
            ?? throw new NotFoundException(nameof(Currency), request.CurrencyId);

        if (request.PurchaseRateToIrr <= 0 || request.SellingRateToIrr <= 0)
            throw new DomainValidationException("Purchase and selling rates must be greater than zero.");

        var rate = new ExchangeRate
        {
            CurrencyId = currency.Id,
            PurchaseRateToIrr = request.PurchaseRateToIrr,
            SellingRateToIrr = request.SellingRateToIrr,
            EffectiveAtUtc = request.EffectiveAtUtc,
            RateSource = request.RateSource,
            Notes = request.Notes,
            IsActive = true,
            CreatedByUserId = _currentUser.UserId,
            CreatedByUserName = _currentUser.UserName,
        };
        _db.ExchangeRates.Add(rate);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.ExchangeRateChanged, nameof(ExchangeRate), rate.Id.ToString(), newValue: rate, cancellationToken: ct);

        var staleDays = await GetStaleDaysAsync(ct);
        return ToDto(rate, currency.Code, staleDays);
    }

    private async Task<int> GetStaleDaysAsync(CancellationToken ct)
    {
        var settings = await _db.CompanySettingsSet.AsNoTracking().FirstOrDefaultAsync(ct);
        return settings?.StaleExchangeRateDays ?? DefaultStaleDays;
    }

    private CurrencyDto ToDto(Currency c, int staleDays)
    {
        var latest = c.Rates.OrderByDescending(r => r.EffectiveAtUtc).FirstOrDefault();
        return new CurrencyDto(c.Id, c.Code, c.Name, c.Symbol, c.IsBaseCurrency, c.IsActive,
            latest is null ? null : ToDto(latest, c.Code, staleDays), c.RowVersion);
    }

    private ExchangeRateDto ToDto(ExchangeRate r, string currencyCode, int staleDays)
    {
        var isStale = (_clock.UtcNow - r.EffectiveAtUtc).TotalDays > staleDays;
        return new ExchangeRateDto(r.Id, r.CurrencyId, currencyCode, r.PurchaseRateToIrr, r.SellingRateToIrr,
            r.EffectiveAtUtc, r.RateSource, r.Notes, r.IsActive, isStale, r.CreatedByUserName, r.CreatedAtUtc);
    }
}
