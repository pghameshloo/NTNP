using NTNP.Pricing.Contracts.Audit;
using NTNP.Pricing.Contracts.Common;

namespace NTNP.Pricing.Application.Audit;

public interface IAuditQueryService
{
    Task<PagedResult<AuditLogEntryDto>> SearchAsync(AuditLogQuery query, CancellationToken ct = default);
}
