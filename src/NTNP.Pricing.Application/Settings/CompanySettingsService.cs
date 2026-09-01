using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Contracts.Settings;
using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Application.Settings;

/// <summary>Section 26/33 — all report/branding text is admin-configurable, never hardcoded.</summary>
public sealed class CompanySettingsService : ICompanySettingsService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public CompanySettingsService(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<CompanySettingsDto> GetAsync(CancellationToken ct = default)
    {
        var settings = await _db.CompanySettingsSet.AsNoTracking().FirstOrDefaultAsync(ct) ?? new CompanySettings();
        return ToDto(settings);
    }

    public async Task<CompanySettingsDto> UpdateAsync(UpdateCompanySettingsRequest request, CancellationToken ct = default)
    {
        var settings = await _db.CompanySettingsSet.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new CompanySettings();
            _db.CompanySettingsSet.Add(settings);
        }

        var s = request.Settings;
        settings.LegalNameEn = s.LegalNameEn;
        settings.LegalNameFa = s.LegalNameFa;
        settings.Address = s.Address;
        settings.Phone = s.Phone;
        settings.Email = s.Email;
        settings.Website = s.Website;
        settings.LogoStoragePath = s.LogoStoragePath;
        settings.StampImageStoragePath = s.StampImageStoragePath;
        settings.DefaultQuotationTitleFa = s.DefaultQuotationTitleFa;
        settings.DefaultQuotationTitleEn = s.DefaultQuotationTitleEn;
        settings.ConfidentialityLabelFa = s.ConfidentialityLabelFa;
        settings.ConfidentialityLabelEn = s.ConfidentialityLabelEn;
        settings.DefaultDeliveryTerms = s.DefaultDeliveryTerms;
        settings.DefaultPaymentTerms = s.DefaultPaymentTerms;
        settings.DefaultWarrantyTerms = s.DefaultWarrantyTerms;
        settings.DefaultInspectionTerms = s.DefaultInspectionTerms;
        settings.DefaultPackingTerms = s.DefaultPackingTerms;
        settings.DefaultTransportationTerms = s.DefaultTransportationTerms;
        settings.DefaultTaxesAndDutiesNote = s.DefaultTaxesAndDutiesNote;
        settings.DefaultScopeExclusions = s.DefaultScopeExclusions;
        settings.PreparedByName = s.PreparedByName;
        settings.PreparedByPosition = s.PreparedByPosition;
        settings.CommercialManagerName = s.CommercialManagerName;
        settings.CommercialManagerPosition = s.CommercialManagerPosition;
        settings.ManagingDirectorName = s.ManagingDirectorName;
        settings.ManagingDirectorPosition = s.ManagingDirectorPosition;
        settings.EnableCustomerAcceptanceBlock = s.EnableCustomerAcceptanceBlock;
        settings.StaleExchangeRateDays = s.StaleExchangeRateDays;
        settings.UpdatedByUserId = _currentUser.UserId;
        settings.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(Domain.Enums.AuditAction.Updated, nameof(CompanySettings), settings.Id.ToString(), cancellationToken: ct);

        return ToDto(settings);
    }

    private static CompanySettingsDto ToDto(CompanySettings s) => new(
        s.LegalNameEn, s.LegalNameFa, s.Address, s.Phone, s.Email, s.Website, s.LogoStoragePath, s.StampImageStoragePath,
        s.DefaultQuotationTitleFa, s.DefaultQuotationTitleEn, s.ConfidentialityLabelFa, s.ConfidentialityLabelEn,
        s.DefaultDeliveryTerms, s.DefaultPaymentTerms, s.DefaultWarrantyTerms, s.DefaultInspectionTerms,
        s.DefaultPackingTerms, s.DefaultTransportationTerms, s.DefaultTaxesAndDutiesNote, s.DefaultScopeExclusions,
        s.PreparedByName, s.PreparedByPosition, s.CommercialManagerName, s.CommercialManagerPosition,
        s.ManagingDirectorName, s.ManagingDirectorPosition, s.EnableCustomerAcceptanceBlock, s.StaleExchangeRateDays);
}
