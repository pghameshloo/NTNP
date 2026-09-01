using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Exceptions;
using NTNP.Pricing.Contracts.Projects;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Domain.Exceptions;

namespace NTNP.Pricing.Application.Projects;

/// <summary>Section 15/14 — Automatic BOM Generator and project lineup line management.</summary>
public sealed class ProjectLineService : IProjectLineService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;
    private readonly BomSnapshotBuilder _snapshotBuilder;

    public ProjectLineService(
        IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, IAuditService audit, BomSnapshotBuilder snapshotBuilder)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
        _snapshotBuilder = snapshotBuilder;
    }

    public async Task<ProjectRevisionDto> AddLineAsync(Guid revisionId, AddProjectLineRequest request, CancellationToken ct = default)
    {
        var revision = await LoadRevisionAsync(revisionId, ct);
        EnsureMutable(revision);

        if (request.QuantityOfPanels <= 0)
            throw new DomainValidationException("Panel quantity must be positive.");
        if (await _db.ProjectLines.AnyAsync(l => l.ProjectRevisionId == revisionId && l.CellCode == request.CellCode, ct))
            throw new DomainValidationException($"Cell code '{request.CellCode}' is already used in this revision.");

        var lineNumber = revision.Lines.Count == 0 ? 1 : revision.Lines.Max(l => l.LineNumber) + 1;
        var line = await _snapshotBuilder.BuildAsync(
            request.PanelTemplateId, request.CellCode, request.QuantityOfPanels, request.OtherDirectCostPerPanel, lineNumber, ct);

        revision.Lines.Add(line);
        // EF Core cannot infer "new" from a client-generated Guid Id reached only via a collection
        // navigation on an already-tracked (Unchanged) parent — it must be added to its DbSet
        // explicitly so it (and its freshly-built BomItems/BodyEsItems) are tracked as Added rather
        // than mistaken for an existing, unmodified row.
        _db.ProjectLines.Add(line);
        ProjectRevisionRecalculator.RecalculateRevision(revision);

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.ProjectLineChanged, nameof(ProjectLine), line.Id.ToString(),
            reason: $"Line added from template {line.PanelTemplateCodeSnapshot}", projectId: revision.ProjectId, projectRevisionId: revision.Id, cancellationToken: ct);

        return ProjectMappers.ToDto(revision);
    }

    public async Task<ProjectRevisionDto> UpdateLineQuantityAsync(
        Guid revisionId, Guid lineId, UpdateProjectLineQuantityRequest request, CancellationToken ct = default)
    {
        var revision = await LoadRevisionAsync(revisionId, ct);
        EnsureMutable(revision);

        var line = revision.Lines.FirstOrDefault(l => l.Id == lineId) ?? throw new NotFoundException(nameof(ProjectLine), lineId);
        if (request.QuantityOfPanels <= 0)
            throw new DomainValidationException("Panel quantity must be positive.");

        _db.Entry(line).Property(l => l.RowVersion).OriginalValue = request.RowVersion;

        line.QuantityOfPanels = request.QuantityOfPanels;
        line.OtherDirectCostPerPanel = request.OtherDirectCostPerPanel;

        ProjectRevisionRecalculator.RecalculateRevision(revision);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.ProjectLineChanged, nameof(ProjectLine), line.Id.ToString(),
            projectId: revision.ProjectId, projectRevisionId: revision.Id, cancellationToken: ct);

        return ProjectMappers.ToDto(revision);
    }

    public async Task<ProjectRevisionDto> RemoveLineAsync(Guid revisionId, Guid lineId, byte[] rowVersion, CancellationToken ct = default)
    {
        var revision = await LoadRevisionAsync(revisionId, ct);
        EnsureMutable(revision);

        var line = revision.Lines.FirstOrDefault(l => l.Id == lineId) ?? throw new NotFoundException(nameof(ProjectLine), lineId);
        _db.Entry(line).Property(l => l.RowVersion).OriginalValue = rowVersion;

        revision.Lines.Remove(line);
        _db.ProjectLines.Remove(line);

        ProjectRevisionRecalculator.RecalculateRevision(revision);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.ProjectLineChanged, nameof(ProjectLine), lineId.ToString(),
            reason: "Line removed", projectId: revision.ProjectId, projectRevisionId: revision.Id, cancellationToken: ct);

        return ProjectMappers.ToDto(revision);
    }

    public async Task<ProjectRevisionDto> OverrideLineFieldAsync(
        Guid revisionId, Guid lineId, ProjectLineOverrideRequest request, CancellationToken ct = default)
    {
        var revision = await LoadRevisionAsync(revisionId, ct);
        EnsureMutable(revision);

        var line = revision.Lines.FirstOrDefault(l => l.Id == lineId) ?? throw new NotFoundException(nameof(ProjectLine), lineId);
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new DomainValidationException("An override reason is required (Section 14).");

        _db.Entry(line).Property(l => l.RowVersion).OriginalValue = request.RowVersion;

        var oldValue = ApplyOverride(line, request.FieldName, request.NewValue);

        var overrideRecord = new ProjectLineOverride
        {
            ProjectLineId = line.Id,
            FieldName = request.FieldName,
            OldValue = oldValue,
            NewValue = request.NewValue,
            Reason = request.Reason,
            UserId = _currentUser.UserId,
            UserName = _currentUser.UserName,
            AtUtc = _clock.UtcNow,
        };
        line.Overrides.Add(overrideRecord);
        _db.ProjectLineOverrides.Add(overrideRecord); // see AddLineAsync's comment re: new entities under an already-tracked parent
        line.HasOverride = true;

        ProjectRevisionRecalculator.RecalculateRevision(revision);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.PricingOverride, nameof(ProjectLine), line.Id.ToString(),
            oldValue: new { request.FieldName, OldValue = oldValue }, newValue: new { request.FieldName, request.NewValue },
            reason: request.Reason, projectId: revision.ProjectId, projectRevisionId: revision.Id, cancellationToken: ct);

        return ProjectMappers.ToDto(revision);
    }

    public async Task<IReadOnlyList<ProjectLineOverrideHistoryDto>> GetOverrideHistoryAsync(Guid lineId, CancellationToken ct = default)
    {
        var overrides = await _db.ProjectLineOverrides.AsNoTracking()
            .Where(o => o.ProjectLineId == lineId)
            .OrderByDescending(o => o.AtUtc)
            .ToListAsync(ct);

        return overrides.Select(o => new ProjectLineOverrideHistoryDto(o.Id, o.FieldName, o.OldValue, o.NewValue, o.Reason, o.UserName, o.AtUtc)).ToList();
    }

    /// <summary>Section 14 — the closed set of fields a Commercial/Approver user may authoritatively override on a line.</summary>
    private static string ApplyOverride(ProjectLine line, string fieldName, string newValue)
    {
        switch (fieldName)
        {
            case nameof(ProjectLine.OtherDirectCostPerPanel):
                var old = line.OtherDirectCostPerPanel.ToString(System.Globalization.CultureInfo.InvariantCulture);
                line.OtherDirectCostPerPanel = decimal.Parse(newValue, System.Globalization.CultureInfo.InvariantCulture);
                return old;
            case nameof(ProjectLine.PricingRateApplied):
                var oldRate = line.PricingRateApplied.ToString(System.Globalization.CultureInfo.InvariantCulture);
                line.PricingRateApplied = decimal.Parse(newValue, System.Globalization.CultureInfo.InvariantCulture);
                return oldRate;
            default:
                throw new DomainValidationException($"Field '{fieldName}' is not an authorized override target.");
        }
    }

    private void EnsureMutable(ProjectRevision revision)
    {
        if (revision.IsImmutable)
            throw new DomainValidationException("This project revision is approved/locked and is immutable (Section 13).");
    }

    private async Task<ProjectRevision> LoadRevisionAsync(Guid revisionId, CancellationToken ct)
    {
        // No explicit .Include for the ProjectLine -> ProjectRevision back-reference: EF Core
        // automatically fixes up that navigation from the Lines collection above (and an explicit
        // Include of it is rejected as "walking back the include tree").
        return await _db.ProjectRevisions
            .Include(r => r.Lines).ThenInclude(l => l.BomItems)
            .Include(r => r.Lines).ThenInclude(l => l.BodyEsItems)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct)
            ?? throw new NotFoundException(nameof(ProjectRevision), revisionId);
    }
}
