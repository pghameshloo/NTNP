using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Exceptions;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Equipment;
using NTNP.Pricing.Domain.Calculation;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Domain.Exceptions;

namespace NTNP.Pricing.Application.Equipment;

/// <summary>Section 9 — the central Equipment Database, replacing "SOURCE PRICE DEVICES".</summary>
public sealed class EquipmentService : IEquipmentService
{
    private const int DefaultStaleDays = 180;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public EquipmentService(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<PagedResult<EquipmentDto>> SearchAsync(
        PagedQuery query, bool includeInactive, string? category, bool missingPriceOnly, CancellationToken ct = default)
    {
        var q = _db.Equipment.AsNoTracking().Include(e => e.Prices).AsQueryable();
        if (!includeInactive) q = q.Where(e => e.IsActive);
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(e => e.Category == category);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(e =>
                e.Code.Contains(term) || e.DescriptionEn.Contains(term) || e.DescriptionFa.Contains(term) ||
                (e.TechnicalPartNumber != null && e.TechnicalPartNumber.Contains(term)) ||
                (e.Brand != null && e.Brand.Contains(term)));
        }

        q = query.SortBy switch
        {
            "code" => query.SortDescending ? q.OrderByDescending(e => e.Code) : q.OrderBy(e => e.Code),
            "category" => query.SortDescending ? q.OrderByDescending(e => e.Category) : q.OrderBy(e => e.Category),
            _ => query.SortDescending ? q.OrderByDescending(e => e.DescriptionEn) : q.OrderBy(e => e.DescriptionEn),
        };

        var all = await q.ToListAsync(ct);
        if (missingPriceOnly) all = all.Where(e => e.CurrentPrice is null).ToList();

        var total = all.Count;
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var pageItems = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<EquipmentDto>(pageItems.Select(e => ToDto(e, DefaultStaleDays)).ToList(), total, page, pageSize);
    }

    public async Task<EquipmentDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var equipment = await _db.Equipment.AsNoTracking().Include(e => e.Prices).FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException(nameof(Equipment), id);
        return ToDto(equipment, DefaultStaleDays);
    }

    public async Task<EquipmentDto> CreateAsync(CreateEquipmentRequest request, CancellationToken ct = default)
    {
        if (await _db.Equipment.AnyAsync(e => e.Code == request.Code, ct))
            throw new DomainValidationException($"Equipment Code '{request.Code}' already exists.");

        var equipment = new Domain.Entities.Equipment
        {
            Code = request.Code,
            TechnicalPartNumber = request.TechnicalPartNumber,
            DescriptionFa = request.DescriptionFa,
            DescriptionEn = request.DescriptionEn,
            Category = request.Category,
            Subcategory = request.Subcategory,
            Brand = request.Brand,
            Model = request.Model,
            Manufacturer = request.Manufacturer,
            Supplier = request.Supplier,
            Unit = request.Unit,
            LeadTimeDays = request.LeadTimeDays,
            Notes = request.Notes,
            CreatedByUserId = _currentUser.UserId,
            CreatedByUserName = _currentUser.UserName,
        };
        _db.Equipment.Add(equipment);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Created, nameof(Domain.Entities.Equipment), equipment.Id.ToString(), newValue: equipment, cancellationToken: ct);

        return ToDto(equipment, DefaultStaleDays);
    }

    public async Task<EquipmentDto> UpdateAsync(Guid id, UpdateEquipmentRequest request, CancellationToken ct = default)
    {
        var equipment = await _db.Equipment.Include(e => e.Prices).FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Equipment), id);

        _db.Entry(equipment).Property(e => e.RowVersion).OriginalValue = request.RowVersion;

        equipment.TechnicalPartNumber = request.TechnicalPartNumber;
        equipment.DescriptionFa = request.DescriptionFa;
        equipment.DescriptionEn = request.DescriptionEn;
        equipment.Category = request.Category;
        equipment.Subcategory = request.Subcategory;
        equipment.Brand = request.Brand;
        equipment.Model = request.Model;
        equipment.Manufacturer = request.Manufacturer;
        equipment.Supplier = request.Supplier;
        equipment.Unit = request.Unit;
        equipment.LeadTimeDays = request.LeadTimeDays;
        equipment.Notes = request.Notes;
        equipment.IsActive = request.IsActive;
        equipment.UpdatedByUserId = _currentUser.UserId;
        equipment.UpdatedByUserName = _currentUser.UserName;
        equipment.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Updated, nameof(Domain.Entities.Equipment), equipment.Id.ToString(), cancellationToken: ct);

        return ToDto(equipment, DefaultStaleDays);
    }

    public async Task BulkSetActiveAsync(IReadOnlyList<Guid> ids, bool isActive, CancellationToken ct = default)
    {
        var items = await _db.Equipment.Where(e => ids.Contains(e.Id)).ToListAsync(ct);
        foreach (var item in items)
        {
            item.IsActive = isActive;
            item.UpdatedByUserId = _currentUser.UserId;
            item.UpdatedByUserName = _currentUser.UserName;
            item.UpdatedAtUtc = _clock.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(isActive ? AuditAction.Activated : AuditAction.Deactivated, nameof(Domain.Entities.Equipment),
            string.Join(",", ids), cancellationToken: ct);
    }

    public async Task<IReadOnlyList<EquipmentPriceDto>> GetPriceHistoryAsync(Guid equipmentId, CancellationToken ct = default)
    {
        if (!await _db.Equipment.AnyAsync(e => e.Id == equipmentId, ct))
            throw new NotFoundException(nameof(Domain.Entities.Equipment), equipmentId);

        var prices = await _db.EquipmentPrices.AsNoTracking()
            .Where(p => p.EquipmentId == equipmentId)
            .OrderByDescending(p => p.EffectiveAtUtc)
            .ToListAsync(ct);

        return prices.Select(ToDto).ToList();
    }

    public async Task<EquipmentPriceDto> AddPriceAsync(CreateEquipmentPriceRequest request, CancellationToken ct = default)
    {
        var equipment = await _db.Equipment.FirstOrDefaultAsync(e => e.Id == request.EquipmentId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Equipment), request.EquipmentId);

        decimal? purchaseRate = null;
        if (!string.Equals(request.PurchaseCurrencyCode, "IRR", StringComparison.OrdinalIgnoreCase))
        {
            var latestRate = await _db.ExchangeRates.AsNoTracking()
                .Where(r => r.Currency.Code == request.PurchaseCurrencyCode && r.IsActive)
                .OrderByDescending(r => r.EffectiveAtUtc)
                .FirstOrDefaultAsync(ct)
                ?? throw new DomainValidationException($"No active exchange rate found for currency '{request.PurchaseCurrencyCode}'.");
            purchaseRate = latestRate.PurchaseRateToIrr;
        }

        var finalCostIrr = PricingCalculationEngine.CalculateEquipmentFinalUnitCostIrr(
            request.PurchaseCurrencyCode, request.ForeignUnitPrice, request.RialUnitPrice, purchaseRate);

        var oldCost = equipment.CurrentPrice?.FinalUnitCostIrr;

        var price = new EquipmentPrice
        {
            EquipmentId = equipment.Id,
            PurchaseCurrencyCode = request.PurchaseCurrencyCode,
            ForeignUnitPrice = request.ForeignUnitPrice,
            RialUnitPrice = request.RialUnitPrice,
            PurchaseExchangeRateSnapshot = purchaseRate,
            FinalUnitCostIrr = finalCostIrr,
            EffectiveAtUtc = request.EffectiveAtUtc,
            PriceSourceText = request.PriceSourceText,
            Notes = request.Notes,
            IsActive = true,
            CreatedByUserId = _currentUser.UserId,
            CreatedByUserName = _currentUser.UserName,
        };
        _db.EquipmentPrices.Add(price);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.PriceChanged, nameof(Domain.Entities.Equipment), equipment.Id.ToString(),
            oldValue: new { OldCostIrr = oldCost }, newValue: new { NewCostIrr = finalCostIrr }, cancellationToken: ct);

        return ToDto(price);
    }

    public async Task<IReadOnlyList<EquipmentDto>> GetMissingOrExpiredPriceReportAsync(int staleDays, CancellationToken ct = default)
    {
        var all = await _db.Equipment.AsNoTracking().Include(e => e.Prices).Where(e => e.IsActive).ToListAsync(ct);
        return all.Where(e => e.CurrentPrice is null || (_clock.UtcNow - e.CurrentPrice.EffectiveAtUtc).TotalDays > staleDays)
            .Select(e => ToDto(e, staleDays))
            .ToList();
    }

    private EquipmentDto ToDto(Domain.Entities.Equipment e, int staleDays)
    {
        var current = e.CurrentPrice;
        var isExpired = current is not null && (_clock.UtcNow - current.EffectiveAtUtc).TotalDays > staleDays;
        return new EquipmentDto(
            e.Id, e.Code, e.TechnicalPartNumber, e.DescriptionFa, e.DescriptionEn, e.Category, e.Subcategory,
            e.Brand, e.Model, e.Manufacturer, e.Supplier, e.Unit, e.LeadTimeDays, e.Notes, e.IsActive,
            current is null ? null : ToDto(current), current is null, isExpired, e.RowVersion);
    }

    private static EquipmentPriceDto ToDto(EquipmentPrice p) => new(
        p.Id, p.EquipmentId, p.PurchaseCurrencyCode, p.ForeignUnitPrice, p.RialUnitPrice,
        p.PurchaseExchangeRateSnapshot, p.FinalUnitCostIrr, p.EffectiveAtUtc, p.PriceSourceText, p.Notes,
        p.IsActive, p.CreatedByUserName, p.CreatedAtUtc);
}
