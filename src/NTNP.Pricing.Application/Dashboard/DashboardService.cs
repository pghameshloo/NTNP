using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Contracts.Dashboard;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Application.Dashboard;

/// <summary>Section 24 — decision-useful dashboard KPIs and charts (no decorative widgets).</summary>
public sealed class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public DashboardService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var settings = await _db.CompanySettingsSet.AsNoTracking().FirstOrDefaultAsync(ct);
        var staleDays = settings?.StaleExchangeRateDays ?? 7;

        var projects = await _db.Projects.AsNoTracking().Include(p => p.Customer).Include(p => p.CurrentRevision).ToListAsync(ct);
        var equipment = await _db.Equipment.AsNoTracking().Include(e => e.Prices).Where(e => e.IsActive).ToListAsync(ct);
        var rates = await _db.ExchangeRates.AsNoTracking().Where(r => r.IsActive).ToListAsync(ct);

        var activeStatuses = new[] { ProjectStatus.Draft, ProjectStatus.UnderEngineeringReview, ProjectStatus.UnderCommercialReview, ProjectStatus.PendingApproval };
        var approvedRevisions = projects.Where(p => p.CurrentRevision is { Status: ProjectStatus.Approved or ProjectStatus.Locked }).ToList();

        var recentProjects = projects
            .OrderByDescending(p => p.UpdatedAtUtc ?? p.CreatedAtUtc)
            .Take(8)
            .Select(p => new RecentProjectDto(p.Id, p.ProjectCode, p.ProjectName, p.Customer.CompanyName, p.Status.ToString(), p.UpdatedAtUtc ?? p.CreatedAtUtc))
            .ToList();

        var valueOverTime = projects
            .Where(p => p.CurrentRevision is not null)
            .GroupBy(p => DateOnly.FromDateTime(p.CreatedAtUtc.UtcDateTime))
            .OrderBy(g => g.Key)
            .Select(g => new QuotationValuePointDto(g.Key, g.Sum(p => p.CurrentRevision!.TotalProjectSellingPriceIrr)))
            .ToList();

        var statusCounts = projects
            .GroupBy(p => p.Status)
            .Select(g => new StatusCountDto(g.Key.ToString(), g.Count()))
            .ToList();

        var costComposition = new List<CostCompositionDto>
        {
            new("Equipment", projects.Sum(p => p.CurrentRevision?.TotalEquipmentCostIrr ?? 0m)),
            new("BODY+ES", projects.Sum(p => p.CurrentRevision?.TotalBodyEsCostIrr ?? 0m)),
            new("Other Direct", projects.Sum(p => p.CurrentRevision?.TotalOtherDirectCostIrr ?? 0m)),
        };

        var recentPriceChanges = await _db.EquipmentPrices.AsNoTracking()
            .Include(p => p.Equipment)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(10)
            .Select(p => new RecentPriceChangeDto(p.Equipment.Code, 0m, p.FinalUnitCostIrr, p.CreatedAtUtc))
            .ToListAsync(ct);

        return new DashboardSummaryDto(
            ActiveProjectsCount: projects.Count(p => activeStatuses.Contains(p.Status)),
            DraftQuotationsCount: projects.Count(p => p.Status == ProjectStatus.Draft),
            PendingApprovalsCount: projects.Count(p => p.Status == ProjectStatus.PendingApproval),
            ApprovedQuotationsCount: approvedRevisions.Count,
            EquipmentMissingPriceCount: equipment.Count(e => e.CurrentPrice is null),
            ExpiredExchangeRatesCount: rates.Count(r => (_clock.UtcNow - r.EffectiveAtUtc).TotalDays > staleDays),
            TotalQuotationValueIrr: approvedRevisions.Sum(p => p.CurrentRevision!.TotalProjectSellingPriceIrr),
            AverageGrossMargin: approvedRevisions.Count == 0 ? 0m : approvedRevisions.Average(p => p.CurrentRevision!.ProjectGrossMargin),
            RecentProjects: recentProjects,
            QuotationValueOverTime: valueOverTime,
            ProjectsByStatus: statusCounts,
            CostComposition: costComposition,
            RecentEquipmentPriceChanges: recentPriceChanges);
    }
}
