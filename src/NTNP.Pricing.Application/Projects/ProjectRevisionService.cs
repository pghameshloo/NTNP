using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Exceptions;
using NTNP.Pricing.Contracts.Projects;
using NTNP.Pricing.Domain.Calculation;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Domain.Exceptions;

namespace NTNP.Pricing.Application.Projects;

/// <summary>Section 13/21 — revisions, comparison, and the Approval workflow (Section 6/21 step 8).</summary>
public sealed class ProjectRevisionService : IProjectRevisionService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;
    private readonly BomSnapshotBuilder _snapshotBuilder;

    public ProjectRevisionService(
        IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, IAuditService audit, BomSnapshotBuilder snapshotBuilder)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
        _snapshotBuilder = snapshotBuilder;
    }

    public async Task<ProjectRevisionDto> GetAsync(Guid revisionId, CancellationToken ct = default)
    {
        var revision = await LoadAsync(revisionId, tracking: false, ct) ?? throw new NotFoundException(nameof(ProjectRevision), revisionId);
        return ProjectMappers.ToDto(revision);
    }

    public async Task<IReadOnlyList<RevisionListItemDto>> ListAsync(Guid projectId, CancellationToken ct = default)
    {
        var revisions = await _db.ProjectRevisions.AsNoTracking()
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.RevisionNumber)
            .ToListAsync(ct);

        return revisions.Select(r => new RevisionListItemDto(
            r.Id, r.RevisionNumber, r.Status.ToString(), r.TotalProjectSellingPriceIrr, r.ProjectGrossMargin, r.CreatedAtUtc)).ToList();
    }

    public async Task<ProjectRevisionDto> CreateNewRevisionUsingLatestPricesAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct) ?? throw new NotFoundException(nameof(Project), projectId);
        var oldRevision = await LoadAsync(project.CurrentRevisionId ?? Guid.Empty, tracking: true, ct)
            ?? throw new DomainValidationException("Project has no active revision to base a new one on.");

        var maxRevisionNumber = await _db.ProjectRevisions.Where(r => r.ProjectId == projectId).MaxAsync(r => r.RevisionNumber, ct);

        var sellingRate = await GetLatestSellingRateAsync(oldRevision.QuotationCurrencyCode, ct);

        var newRevision = new ProjectRevision
        {
            ProjectId = projectId,
            RevisionNumber = maxRevisionNumber + 1,
            Status = ProjectStatus.Draft,
            QuotationCurrencyCode = oldRevision.QuotationCurrencyCode,
            RialShare = oldRevision.RialShare,
            ForeignShare = oldRevision.ForeignShare,
            PricingMethod = oldRevision.PricingMethod,
            PricingRate = oldRevision.PricingRate,
            IrrRoundingPolicy = oldRevision.IrrRoundingPolicy,
            ForeignRoundingPolicy = oldRevision.ForeignRoundingPolicy,
            ForeignDecimalPlaces = oldRevision.ForeignDecimalPlaces,
            ReconciliationToleranceIrr = oldRevision.ReconciliationToleranceIrr,
            SellingExchangeRateId = sellingRate?.Id,
            SellingExchangeRateValue = sellingRate?.SellingRateToIrr ?? oldRevision.SellingExchangeRateValue,
            SellingExchangeRateEffectiveAtUtc = sellingRate?.EffectiveAtUtc ?? _clock.UtcNow,
            SupersedesRevisionId = oldRevision.Id,
            CreatedByUserId = _currentUser.UserId,
            CreatedByUserName = _currentUser.UserName,
        };

        var lineNumber = 1;
        foreach (var oldLine in oldRevision.Lines.OrderBy(l => l.LineNumber))
        {
            if (oldLine.PanelTemplateId is not { } templateId) continue; // manually-added lines without a template cannot be re-generated
            var newLine = await _snapshotBuilder.BuildAsync(
                templateId, oldLine.CellCode, oldLine.QuantityOfPanels, oldLine.OtherDirectCostPerPanel, lineNumber++, ct);
            newRevision.Lines.Add(newLine);
        }

        ProjectRevisionRecalculator.RecalculateRevision(newRevision);
        _db.ProjectRevisions.Add(newRevision);

        // Section 13: an approved/locked revision is never mutated; only a non-immutable "in-flight"
        // revision is marked Superseded when replaced by a fresh one.
        if (!oldRevision.IsImmutable)
            oldRevision.Status = ProjectStatus.Superseded;

        project.CurrentRevisionId = newRevision.Id;
        project.CurrentRevisionNumber = newRevision.RevisionNumber;
        project.Status = ProjectStatus.Draft;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.RevisionCreated, nameof(ProjectRevision), newRevision.Id.ToString(),
            reason: $"New revision {newRevision.RevisionNumber} created using latest prices (from revision {oldRevision.RevisionNumber})",
            projectId: projectId, projectRevisionId: newRevision.Id, cancellationToken: ct);

        var loaded = await LoadAsync(newRevision.Id, tracking: false, ct);
        return ProjectMappers.ToDto(loaded!);
    }

    public async Task<RevisionComparisonDto> CompareAsync(Guid fromRevisionId, Guid toRevisionId, CancellationToken ct = default)
    {
        var from = await LoadAsync(fromRevisionId, tracking: false, ct) ?? throw new NotFoundException(nameof(ProjectRevision), fromRevisionId);
        var to = await LoadAsync(toRevisionId, tracking: false, ct) ?? throw new NotFoundException(nameof(ProjectRevision), toRevisionId);

        var changes = new List<RevisionComparisonLineDelta>();
        var fromByCode = from.Lines.ToDictionary(l => l.CellCode);
        var toByCode = to.Lines.ToDictionary(l => l.CellCode);

        foreach (var code in fromByCode.Keys.Union(toByCode.Keys).OrderBy(c => c))
        {
            var hasOld = fromByCode.TryGetValue(code, out var oldLine);
            var hasNew = toByCode.TryGetValue(code, out var newLine);

            if (!hasOld && hasNew) { changes.Add(new RevisionComparisonLineDelta(code, "Line", "(none)", "Added")); continue; }
            if (hasOld && !hasNew) { changes.Add(new RevisionComparisonLineDelta(code, "Line", "Present", "(removed)")); continue; }

            if (oldLine!.QuantityOfPanels != newLine!.QuantityOfPanels)
                changes.Add(new RevisionComparisonLineDelta(code, nameof(ProjectLine.QuantityOfPanels),
                    oldLine.QuantityOfPanels.ToString(CultureInfo.InvariantCulture), newLine.QuantityOfPanels.ToString(CultureInfo.InvariantCulture)));

            if (oldLine.TotalLineCost != newLine.TotalLineCost)
                changes.Add(new RevisionComparisonLineDelta(code, nameof(ProjectLine.TotalLineCost),
                    oldLine.TotalLineCost.ToString("N0", CultureInfo.InvariantCulture), newLine.TotalLineCost.ToString("N0", CultureInfo.InvariantCulture)));

            if (oldLine.TotalLineSellingPrice != newLine.TotalLineSellingPrice)
                changes.Add(new RevisionComparisonLineDelta(code, nameof(ProjectLine.TotalLineSellingPrice),
                    oldLine.TotalLineSellingPrice.ToString("N0", CultureInfo.InvariantCulture), newLine.TotalLineSellingPrice.ToString("N0", CultureInfo.InvariantCulture)));
        }

        return new RevisionComparisonDto(
            from.RevisionNumber, to.RevisionNumber,
            to.TotalProjectCostIrr - from.TotalProjectCostIrr,
            to.TotalProjectSellingPriceIrr - from.TotalProjectSellingPriceIrr,
            to.ProjectProfitIrr - from.ProjectProfitIrr,
            to.ProjectGrossMargin - from.ProjectGrossMargin,
            changes);
    }

    public async Task<ProjectRevisionDto> SubmitForApprovalAsync(Guid revisionId, SubmitForApprovalRequest request, CancellationToken ct = default)
    {
        var revision = await LoadAsync(revisionId, tracking: true, ct) ?? throw new NotFoundException(nameof(ProjectRevision), revisionId);
        if (revision.IsImmutable)
            throw new DomainValidationException("Revision is already approved/locked.");

        var blockers = ProjectTotalsCalculator.GetApprovalBlockers(revision);
        if (blockers.Count > 0)
            throw new DomainValidationException(blockers);

        _db.Entry(revision).Property(r => r.RowVersion).OriginalValue = request.RowVersion;

        revision.Status = ProjectStatus.PendingApproval;
        revision.SubmittedByUserId = _currentUser.UserId;
        revision.SubmittedAtUtc = _clock.UtcNow;

        var project = await _db.Projects.FirstAsync(p => p.Id == revision.ProjectId, ct);
        project.Status = ProjectStatus.PendingApproval;
        project.ReviewedByUserId = _currentUser.UserId;
        project.ReviewedByUserName = _currentUser.UserName;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Updated, nameof(ProjectRevision), revision.Id.ToString(),
            reason: "Submitted for approval", projectId: revision.ProjectId, projectRevisionId: revision.Id, cancellationToken: ct);

        return ProjectMappers.ToDto(revision);
    }

    public async Task<ProjectRevisionDto> DecideApprovalAsync(Guid revisionId, ApprovalDecisionRequest request, CancellationToken ct = default)
    {
        var revision = await LoadAsync(revisionId, tracking: true, ct) ?? throw new NotFoundException(nameof(ProjectRevision), revisionId);
        if (revision.Status != ProjectStatus.PendingApproval)
            throw new DomainValidationException("Only a revision Pending Approval can be approved or rejected.");

        _db.Entry(revision).Property(r => r.RowVersion).OriginalValue = request.RowVersion;
        var project = await _db.Projects.FirstAsync(p => p.Id == revision.ProjectId, ct);

        if (request.Approve)
        {
            var blockers = ProjectTotalsCalculator.GetApprovalBlockers(revision);
            if (blockers.Count > 0)
                throw new DomainValidationException(blockers); // Section 19 — block approval of an invalid revision.

            revision.Status = ProjectStatus.Approved;
            revision.ApprovedByUserId = _currentUser.UserId;
            revision.ApprovedByUserName = _currentUser.UserName;
            revision.ApprovedAtUtc = _clock.UtcNow;

            project.Status = ProjectStatus.Approved;
            project.ApprovedByUserId = _currentUser.UserId;
            project.ApprovedByUserName = _currentUser.UserName;
            project.ApprovedAtUtc = _clock.UtcNow;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Comments))
                throw new DomainValidationException("A rejection reason is required.");

            revision.Status = ProjectStatus.Rejected;
            revision.RejectedByUserId = _currentUser.UserId;
            revision.RejectedAtUtc = _clock.UtcNow;
            revision.RejectionReason = request.Comments;

            project.Status = ProjectStatus.Rejected;
        }

        _db.ApprovalRecords.Add(new ApprovalRecord
        {
            ProjectRevisionId = revision.Id,
            IsApproved = request.Approve,
            Comments = request.Comments,
            DecidedByUserId = _currentUser.UserId,
            DecidedByUserName = _currentUser.UserName,
            DecidedAtUtc = _clock.UtcNow,
            TotalProjectCostIrrAtDecision = revision.TotalProjectCostIrr,
            TotalProjectSellingPriceIrrAtDecision = revision.TotalProjectSellingPriceIrr,
            ProjectGrossMarginAtDecision = revision.ProjectGrossMargin,
        });

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(request.Approve ? AuditAction.Approved : AuditAction.Rejected, nameof(ProjectRevision), revision.Id.ToString(),
            reason: request.Comments, projectId: revision.ProjectId, projectRevisionId: revision.Id, cancellationToken: ct);

        return ProjectMappers.ToDto(revision);
    }

    public async Task<ProjectRevisionDto> LockAsync(Guid revisionId, LockRevisionRequest request, CancellationToken ct = default)
    {
        var revision = await LoadAsync(revisionId, tracking: true, ct) ?? throw new NotFoundException(nameof(ProjectRevision), revisionId);
        if (revision.Status != ProjectStatus.Approved)
            throw new DomainValidationException("Only an Approved revision can be locked.");

        _db.Entry(revision).Property(r => r.RowVersion).OriginalValue = request.RowVersion;

        revision.Status = ProjectStatus.Locked;
        revision.LockedByUserId = _currentUser.UserId;
        revision.LockedAtUtc = _clock.UtcNow;

        var project = await _db.Projects.FirstAsync(p => p.Id == revision.ProjectId, ct);
        project.Status = ProjectStatus.Locked;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Locked, nameof(ProjectRevision), revision.Id.ToString(),
            projectId: revision.ProjectId, projectRevisionId: revision.Id, cancellationToken: ct);

        return ProjectMappers.ToDto(revision);
    }

    public async Task<IReadOnlyList<ApprovalHistoryItemDto>> GetApprovalHistoryAsync(Guid revisionId, CancellationToken ct = default)
    {
        var records = await _db.ApprovalRecords.AsNoTracking()
            .Where(a => a.ProjectRevisionId == revisionId)
            .OrderByDescending(a => a.DecidedAtUtc)
            .ToListAsync(ct);

        return records.Select(a => new ApprovalHistoryItemDto(
            a.Id, a.IsApproved, a.Comments, a.DecidedByUserName, a.DecidedAtUtc,
            a.TotalProjectCostIrrAtDecision, a.TotalProjectSellingPriceIrrAtDecision, a.ProjectGrossMarginAtDecision)).ToList();
    }

    private async Task<ExchangeRate?> GetLatestSellingRateAsync(string currencyCode, CancellationToken ct)
    {
        if (string.Equals(currencyCode, "IRR", StringComparison.OrdinalIgnoreCase)) return null;
        return await _db.ExchangeRates.AsNoTracking()
            .Where(r => r.Currency.Code == currencyCode && r.IsActive)
            .OrderByDescending(r => r.EffectiveAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<ProjectRevision?> LoadAsync(Guid revisionId, bool tracking, CancellationToken ct)
    {
        var q = _db.ProjectRevisions
            .Include(r => r.Lines).ThenInclude(l => l.BomItems)
            .Include(r => r.Lines).ThenInclude(l => l.BodyEsItems)
            .AsQueryable();
        if (!tracking) q = q.AsNoTracking();
        return await q.FirstOrDefaultAsync(r => r.Id == revisionId, ct);
    }
}
