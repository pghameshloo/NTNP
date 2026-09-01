using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Exceptions;
using NTNP.Pricing.Contracts.PricingProfiles;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Domain.Exceptions;

namespace NTNP.Pricing.Application.PricingProfiles;

/// <summary>Section 12 — Pricing Profiles and Settings.</summary>
public sealed class PricingProfileService : IPricingProfileService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public PricingProfileService(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<IReadOnlyList<PricingProfileDto>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var q = _db.PricingProfiles.AsNoTracking().AsQueryable();
        if (!includeInactive) q = q.Where(p => p.IsActive);
        var items = await q.OrderBy(p => p.Name).ToListAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<PricingProfileDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var profile = await _db.PricingProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(PricingProfile), id);
        return ToDto(profile);
    }

    public async Task<PricingProfileDto> CreateAsync(UpsertPricingProfileRequest request, CancellationToken ct = default)
    {
        if (await _db.PricingProfiles.AnyAsync(p => p.Name == request.Name, ct))
            throw new DomainValidationException($"A pricing profile named '{request.Name}' already exists.");

        Validate(request);

        var profile = new PricingProfile
        {
            Name = request.Name,
            PricingMethod = Enum.Parse<PricingMethod>(request.PricingMethod),
            DefaultRate = request.DefaultRate,
            DefaultRialShare = request.DefaultRialShare,
            DefaultForeignShare = request.DefaultForeignShare,
            DefaultQuotationCurrencyCode = request.DefaultQuotationCurrencyCode,
            IrrRoundingPolicy = Enum.Parse<RoundingMode>(request.IrrRoundingPolicy),
            ForeignRoundingPolicy = Enum.Parse<RoundingMode>(request.ForeignRoundingPolicy),
            ForeignDecimalPlaces = request.ForeignDecimalPlaces,
            ReconciliationToleranceIrr = request.ReconciliationToleranceIrr,
            CreatedByUserId = _currentUser.UserId,
            CreatedByUserName = _currentUser.UserName,
        };
        _db.PricingProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Created, nameof(PricingProfile), profile.Id.ToString(), newValue: profile, cancellationToken: ct);

        return ToDto(profile);
    }

    public async Task<PricingProfileDto> UpdateAsync(Guid id, UpsertPricingProfileRequest request, CancellationToken ct = default)
    {
        var profile = await _db.PricingProfiles.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(PricingProfile), id);

        Validate(request);
        if (request.RowVersion is not null)
            _db.Entry(profile).Property(p => p.RowVersion).OriginalValue = request.RowVersion;

        profile.Name = request.Name;
        profile.PricingMethod = Enum.Parse<PricingMethod>(request.PricingMethod);
        profile.DefaultRate = request.DefaultRate;
        profile.DefaultRialShare = request.DefaultRialShare;
        profile.DefaultForeignShare = request.DefaultForeignShare;
        profile.DefaultQuotationCurrencyCode = request.DefaultQuotationCurrencyCode;
        profile.IrrRoundingPolicy = Enum.Parse<RoundingMode>(request.IrrRoundingPolicy);
        profile.ForeignRoundingPolicy = Enum.Parse<RoundingMode>(request.ForeignRoundingPolicy);
        profile.ForeignDecimalPlaces = request.ForeignDecimalPlaces;
        profile.ReconciliationToleranceIrr = request.ReconciliationToleranceIrr;
        profile.UpdatedByUserId = _currentUser.UserId;
        profile.UpdatedByUserName = _currentUser.UserName;
        profile.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Updated, nameof(PricingProfile), profile.Id.ToString(), cancellationToken: ct);

        return ToDto(profile);
    }

    private static void Validate(UpsertPricingProfileRequest request)
    {
        Domain.Calculation.PricingCalculationEngine.ValidateShares(request.DefaultRialShare, request.DefaultForeignShare);
        if (request.PricingMethod == nameof(PricingMethod.GrossMargin) && request.DefaultRate >= 1m)
            throw new DomainValidationException("Gross margin must be below 100%.");
    }

    private static PricingProfileDto ToDto(PricingProfile p) => new(
        p.Id, p.Name, p.PricingMethod.ToString(), p.DefaultRate, p.EquivalentMultiplier, p.DefaultRialShare,
        p.DefaultForeignShare, p.DefaultQuotationCurrencyCode, p.IrrRoundingPolicy.ToString(), p.ForeignRoundingPolicy.ToString(),
        p.ForeignDecimalPlaces, p.ReconciliationToleranceIrr, p.IsActive, p.RowVersion);
}
