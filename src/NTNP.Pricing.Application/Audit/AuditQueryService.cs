using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Contracts.Audit;
using NTNP.Pricing.Contracts.Common;

namespace NTNP.Pricing.Application.Audit;

/// <summary>Section 30 — read-only audit log search (Admin's "Audit Logs" screen).</summary>
public sealed class AuditQueryService : IAuditQueryService
{
    private readonly IApplicationDbContext _db;

    public AuditQueryService(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogEntryDto>> SearchAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        var q = _db.AuditLogEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.EntityType)) q = q.Where(a => a.EntityType == query.EntityType);
        if (!string.IsNullOrWhiteSpace(query.EntityId)) q = q.Where(a => a.EntityId == query.EntityId);
        if (query.UserId is { } userId) q = q.Where(a => a.UserId == userId);
        if (query.ProjectId is { } projectId) q = q.Where(a => a.ProjectId == projectId);
        if (query.FromUtc is { } from) q = q.Where(a => a.AtUtc >= from);
        if (query.ToUtc is { } to) q = q.Where(a => a.AtUtc <= to);

        q = q.OrderByDescending(a => a.AtUtc);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 500);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var dtos = items.Select(a => new AuditLogEntryDto(
            a.Id, a.UserName, a.Action.ToString(), a.EntityType, a.EntityId, a.OldValueJson, a.NewValueJson,
            a.AtUtc, a.Reason, a.ProjectId, a.ProjectRevisionId)).ToList();

        return new PagedResult<AuditLogEntryDto>(dtos, total, page, pageSize);
    }
}
