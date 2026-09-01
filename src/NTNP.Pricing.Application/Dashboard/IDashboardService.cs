using NTNP.Pricing.Contracts.Dashboard;

namespace NTNP.Pricing.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
}
