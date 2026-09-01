using NTNP.Pricing.Contracts.Settings;

namespace NTNP.Pricing.Application.Settings;

public interface ICompanySettingsService
{
    Task<CompanySettingsDto> GetAsync(CancellationToken ct = default);
    Task<CompanySettingsDto> UpdateAsync(UpdateCompanySettingsRequest request, CancellationToken ct = default);
}
