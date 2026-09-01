using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Exceptions;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.PanelTemplates;
using NTNP.Pricing.Domain.Calculation;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Domain.Exceptions;

namespace NTNP.Pricing.Application.PanelTemplates;

/// <summary>Section 10 — Panel Template and BOM module.</summary>
public sealed class PanelTemplateService : IPanelTemplateService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public PanelTemplateService(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<PagedResult<PanelTemplateDto>> SearchAsync(
        PagedQuery query, Guid? productFamilyId, Guid? panelTypeId, CancellationToken ct = default)
    {
        var q = _db.PanelTemplates.AsNoTracking()
            .Include(t => t.ProductFamily).Include(t => t.PanelType).Include(t => t.BodyEsTemplate)
            .Include(t => t.BomItems).ThenInclude(i => i.Equipment)
            .AsQueryable();

        if (productFamilyId is not null) q = q.Where(t => t.ProductFamilyId == productFamilyId);
        if (panelTypeId is not null) q = q.Where(t => t.PanelTypeId == panelTypeId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(t => t.TemplateCode.Contains(term) || t.TemplateName.Contains(term));
        }

        q = query.SortDescending ? q.OrderByDescending(t => t.TemplateCode) : q.OrderBy(t => t.TemplateCode);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<PanelTemplateDto>(items.Select(ToDto).ToList(), total, page, pageSize);
    }

    public async Task<PanelTemplateDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var template = await LoadAsync(id, tracking: false, ct) ?? throw new NotFoundException(nameof(PanelTemplate), id);
        return ToDto(template);
    }

    public async Task<PanelTemplateDto> CreateAsync(CreatePanelTemplateRequest request, CancellationToken ct = default)
    {
        if (await _db.PanelTemplates.AnyAsync(t => t.TemplateCode == request.TemplateCode, ct))
            throw new DomainValidationException($"Panel template code '{request.TemplateCode}' already exists.");

        var template = new PanelTemplate
        {
            TemplateCode = request.TemplateCode,
            TemplateName = request.TemplateName,
            ProductFamilyId = request.ProductFamilyId,
            VoltageLevel = request.VoltageLevel,
            PanelTypeId = request.PanelTypeId,
            TechnicalDescription = request.TechnicalDescription,
            RevisionNumber = 1,
            Status = TemplateStatus.Draft,
            BodyEsTemplateId = request.BodyEsTemplateId,
            Notes = request.Notes,
            CreatedByUserId = _currentUser.UserId,
            CreatedByUserName = _currentUser.UserName,
        };

        foreach (var item in request.BomItems)
            template.BomItems.Add(ToEntity(item));

        _db.PanelTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Created, nameof(PanelTemplate), template.Id.ToString(), newValue: template, cancellationToken: ct);

        var loaded = await LoadAsync(template.Id, tracking: false, ct);
        return ToDto(loaded!);
    }

    public async Task<PanelTemplateDto> UpdateAsync(Guid id, UpdatePanelTemplateRequest request, CancellationToken ct = default)
    {
        var template = await LoadAsync(id, tracking: true, ct) ?? throw new NotFoundException(nameof(PanelTemplate), id);

        if (template.Status != TemplateStatus.Draft)
            throw new DomainValidationException(
                "Only Draft panel templates can be edited in place. Approved templates are immutable — call " +
                "'Create New Revision' first (Section 10: changing a master template must not alter approved projects).");

        _db.Entry(template).Property(t => t.RowVersion).OriginalValue = request.RowVersion;

        template.TemplateName = request.TemplateName;
        template.VoltageLevel = request.VoltageLevel;
        template.TechnicalDescription = request.TechnicalDescription;
        template.BodyEsTemplateId = request.BodyEsTemplateId;
        template.Notes = request.Notes;
        template.UpdatedByUserId = _currentUser.UserId;
        template.UpdatedByUserName = _currentUser.UserName;
        template.UpdatedAtUtc = _clock.UtcNow;

        SyncBomItems(template, request.BomItems);

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.BomChanged, nameof(PanelTemplate), template.Id.ToString(), cancellationToken: ct);

        var loaded = await LoadAsync(template.Id, tracking: false, ct);
        return ToDto(loaded!);
    }

    public async Task<PanelTemplateDto> CreateNewRevisionAsync(Guid id, CancellationToken ct = default)
    {
        var source = await LoadAsync(id, tracking: false, ct) ?? throw new NotFoundException(nameof(PanelTemplate), id);

        var maxRevision = await _db.PanelTemplates
            .Where(t => t.TemplateCode == source.TemplateCode)
            .MaxAsync(t => (int?)t.RevisionNumber, ct) ?? source.RevisionNumber;

        var newRevision = new PanelTemplate
        {
            TemplateCode = source.TemplateCode,
            TemplateName = source.TemplateName,
            ProductFamilyId = source.ProductFamilyId,
            VoltageLevel = source.VoltageLevel,
            PanelTypeId = source.PanelTypeId,
            TechnicalDescription = source.TechnicalDescription,
            RevisionNumber = maxRevision + 1,
            Status = TemplateStatus.Draft,
            BodyEsTemplateId = source.BodyEsTemplateId,
            Notes = source.Notes,
            CreatedByUserId = _currentUser.UserId,
            CreatedByUserName = _currentUser.UserName,
        };
        foreach (var item in source.BomItems)
        {
            newRevision.BomItems.Add(new PanelTemplateBomItem
            {
                EquipmentId = item.EquipmentId, QuantityPerPanel = item.QuantityPerPanel, Unit = item.Unit,
                WastePercentage = item.WastePercentage, CostMultiplier = item.CostMultiplier, Notes = item.Notes, SortOrder = item.SortOrder,
            });
        }

        _db.PanelTemplates.Add(newRevision);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.RevisionCreated, nameof(PanelTemplate), newRevision.Id.ToString(),
            reason: $"New revision of {source.TemplateCode} (from rev {source.RevisionNumber})", cancellationToken: ct);

        var loaded = await LoadAsync(newRevision.Id, tracking: false, ct);
        return ToDto(loaded!);
    }

    public async Task<PanelTemplateDto> ApproveAsync(Guid id, ApproveTemplateRequest request, CancellationToken ct = default)
    {
        var template = await LoadAsync(id, tracking: true, ct) ?? throw new NotFoundException(nameof(PanelTemplate), id);

        if (template.BomItems.Count == 0)
            throw new DomainValidationException("Cannot approve a panel template with no BOM items.");

        _db.Entry(template).Property(t => t.RowVersion).OriginalValue = request.RowVersion;

        // Only one Approved revision per TemplateCode at a time — demote earlier approved revisions.
        var previouslyApproved = await _db.PanelTemplates
            .Where(t => t.TemplateCode == template.TemplateCode && t.Status == TemplateStatus.Approved)
            .ToListAsync(ct);
        foreach (var old in previouslyApproved) old.Status = TemplateStatus.Deprecated;

        template.Status = TemplateStatus.Approved;
        template.ApprovedByUserId = _currentUser.UserId;
        template.ApprovedByUserName = _currentUser.UserName;
        template.ApprovedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.TemplateApproved, nameof(PanelTemplate), template.Id.ToString(), cancellationToken: ct);

        var loaded = await LoadAsync(template.Id, tracking: false, ct);
        return ToDto(loaded!);
    }

    private void SyncBomItems(PanelTemplate template, IReadOnlyList<UpsertPanelTemplateBomItemRequest> requestedItems)
    {
        var requestedIds = requestedItems.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
        foreach (var toRemove in template.BomItems.Where(i => !requestedIds.Contains(i.Id)).ToList())
            template.BomItems.Remove(toRemove);

        foreach (var item in requestedItems)
        {
            if (item.Id is { } existingId)
            {
                var existing = template.BomItems.First(i => i.Id == existingId);
                existing.EquipmentId = item.EquipmentId;
                existing.QuantityPerPanel = item.QuantityPerPanel;
                existing.Unit = item.Unit;
                existing.WastePercentage = item.WastePercentage;
                existing.CostMultiplier = item.CostMultiplier;
                existing.Notes = item.Notes;
                existing.SortOrder = item.SortOrder;
            }
            else
            {
                var newItem = ToEntity(item);
                template.BomItems.Add(newItem);
                // template is an already-tracked (Unchanged) entity here (loaded, not just built in
                // memory), so a new child reached only via its collection navigation would otherwise
                // be tracked as Modified/Unchanged instead of Added (EF can't infer "new" from a
                // client-generated Guid Id alone) — add it to its DbSet explicitly.
                _db.PanelTemplateBomItems.Add(newItem);
            }
        }
    }

    private static PanelTemplateBomItem ToEntity(UpsertPanelTemplateBomItemRequest item) => new()
    {
        EquipmentId = item.EquipmentId, QuantityPerPanel = item.QuantityPerPanel, Unit = item.Unit,
        WastePercentage = item.WastePercentage, CostMultiplier = item.CostMultiplier, Notes = item.Notes, SortOrder = item.SortOrder,
    };

    private async Task<PanelTemplate?> LoadAsync(Guid id, bool tracking, CancellationToken ct)
    {
        var q = _db.PanelTemplates
            .Include(t => t.ProductFamily).Include(t => t.PanelType).Include(t => t.BodyEsTemplate)
            .Include(t => t.BomItems).ThenInclude(i => i.Equipment)
            .AsQueryable();
        if (!tracking) q = q.AsNoTracking();
        return await q.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    private static PanelTemplateDto ToDto(PanelTemplate t)
    {
        var bomDtos = t.BomItems
            .OrderBy(i => i.SortOrder)
            .Select(i => new PanelTemplateBomItemDto(
                i.Id, i.EquipmentId, i.Equipment.Code, i.Equipment.DescriptionEn,
                i.QuantityPerPanel, i.Unit, i.WastePercentage, i.CostMultiplier, i.Notes, i.SortOrder))
            .ToList();

        var computedCost = t.BomItems.Sum(i =>
        {
            var unitCost = i.Equipment.CurrentPrice?.FinalUnitCostIrr ?? 0m;
            var (_, lineCost) = PricingCalculationEngine.CalculateLine(i.QuantityPerPanel, i.WastePercentage, unitCost, i.CostMultiplier);
            return lineCost;
        });

        return new PanelTemplateDto(
            t.Id, t.TemplateCode, t.TemplateName, t.ProductFamilyId, t.ProductFamily.Name, t.VoltageLevel,
            t.PanelTypeId, t.PanelType.Name, t.TechnicalDescription, t.RevisionNumber, t.Status.ToString(),
            t.BodyEsTemplateId, t.BodyEsTemplate?.TemplateName, t.Notes, t.ApprovedByUserName, t.ApprovedAtUtc,
            bomDtos, computedCost, t.RowVersion);
    }
}
