using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Exceptions;
using NTNP.Pricing.Contracts.BodyEs;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Domain.Exceptions;

namespace NTNP.Pricing.Application.BodyEs;

/// <summary>Section 11 — BODY+ES costing module, kept separate from equipment BOM but rolled into panel cost.</summary>
public sealed class BodyEsTemplateService : IBodyEsTemplateService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public BodyEsTemplateService(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<PagedResult<BodyEsTemplateDto>> SearchAsync(PagedQuery query, Guid? productFamilyId, Guid? panelTypeId, CancellationToken ct = default)
    {
        var q = _db.BodyEsTemplates.AsNoTracking().Include(t => t.ProductFamily).Include(t => t.PanelType).Include(t => t.Items).AsQueryable();
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

        return new PagedResult<BodyEsTemplateDto>(items.Select(ToDto).ToList(), total, page, pageSize);
    }

    public async Task<BodyEsTemplateDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var template = await LoadAsync(id, tracking: false, ct) ?? throw new NotFoundException(nameof(BodyEsTemplate), id);
        return ToDto(template);
    }

    public async Task<BodyEsTemplateDto> CreateAsync(CreateBodyEsTemplateRequest request, CancellationToken ct = default)
    {
        if (await _db.BodyEsTemplates.AnyAsync(t => t.TemplateCode == request.TemplateCode, ct))
            throw new DomainValidationException($"BODY+ES template code '{request.TemplateCode}' already exists.");

        var template = new BodyEsTemplate
        {
            TemplateCode = request.TemplateCode,
            TemplateName = request.TemplateName,
            ProductFamilyId = request.ProductFamilyId,
            PanelTypeId = request.PanelTypeId,
            PanelDimensions = request.PanelDimensions,
            RevisionNumber = 1,
            Status = TemplateStatus.Draft,
            Notes = request.Notes,
            CreatedByUserId = _currentUser.UserId,
            CreatedByUserName = _currentUser.UserName,
        };
        foreach (var item in request.Items) template.Items.Add(ToEntity(item));

        _db.BodyEsTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Created, nameof(BodyEsTemplate), template.Id.ToString(), newValue: template, cancellationToken: ct);

        var loaded = await LoadAsync(template.Id, tracking: false, ct);
        return ToDto(loaded!);
    }

    public async Task<BodyEsTemplateDto> UpdateAsync(Guid id, UpdateBodyEsTemplateRequest request, CancellationToken ct = default)
    {
        var template = await LoadAsync(id, tracking: true, ct) ?? throw new NotFoundException(nameof(BodyEsTemplate), id);

        if (template.Status != TemplateStatus.Draft)
            throw new DomainValidationException("Only Draft BODY+ES templates can be edited in place (Section 11 versioning).");

        _db.Entry(template).Property(t => t.RowVersion).OriginalValue = request.RowVersion;

        template.TemplateName = request.TemplateName;
        template.PanelDimensions = request.PanelDimensions;
        template.Notes = request.Notes;
        template.UpdatedByUserId = _currentUser.UserId;
        template.UpdatedByUserName = _currentUser.UserName;
        template.UpdatedAtUtc = _clock.UtcNow;

        var requestedIds = request.Items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
        foreach (var toRemove in template.Items.Where(i => !requestedIds.Contains(i.Id)).ToList())
            template.Items.Remove(toRemove);

        foreach (var item in request.Items)
        {
            if (item.Id is { } existingId)
            {
                var existing = template.Items.First(i => i.Id == existingId);
                existing.ComponentCode = item.ComponentCode;
                existing.DescriptionFa = item.DescriptionFa;
                existing.DescriptionEn = item.DescriptionEn;
                existing.Category = item.Category;
                existing.Unit = item.Unit;
                existing.QuantityPerPanel = item.QuantityPerPanel;
                existing.WastePercentage = item.WastePercentage;
                existing.UnitCostIrr = item.UnitCostIrr;
                existing.Notes = item.Notes;
                existing.SortOrder = item.SortOrder;
            }
            else
            {
                var newItem = ToEntity(item);
                template.Items.Add(newItem);
                // See ProjectLineService/PanelTemplateService for why this explicit DbSet.Add() is
                // required: template is already-tracked here, so a new child reached only via its
                // collection navigation would otherwise be mistaken for an existing, unmodified row.
                _db.BodyEsTemplateItems.Add(newItem);
            }
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.BodyEsChanged, nameof(BodyEsTemplate), template.Id.ToString(), cancellationToken: ct);

        var loaded = await LoadAsync(template.Id, tracking: false, ct);
        return ToDto(loaded!);
    }

    public async Task<BodyEsTemplateDto> ApproveAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
    {
        var template = await LoadAsync(id, tracking: true, ct) ?? throw new NotFoundException(nameof(BodyEsTemplate), id);

        if (template.Items.Count == 0)
            throw new DomainValidationException("Cannot approve a BODY+ES template with no component lines.");

        _db.Entry(template).Property(t => t.RowVersion).OriginalValue = rowVersion;

        var previouslyApproved = await _db.BodyEsTemplates
            .Where(t => t.TemplateCode == template.TemplateCode && t.Status == TemplateStatus.Approved)
            .ToListAsync(ct);
        foreach (var old in previouslyApproved) old.Status = TemplateStatus.Deprecated;

        template.Status = TemplateStatus.Approved;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.TemplateApproved, nameof(BodyEsTemplate), template.Id.ToString(), cancellationToken: ct);

        var loaded = await LoadAsync(template.Id, tracking: false, ct);
        return ToDto(loaded!);
    }

    private static BodyEsTemplateItem ToEntity(UpsertBodyEsTemplateItemRequest item) => new()
    {
        ComponentCode = item.ComponentCode, DescriptionFa = item.DescriptionFa, DescriptionEn = item.DescriptionEn,
        Category = item.Category, Unit = item.Unit, QuantityPerPanel = item.QuantityPerPanel,
        WastePercentage = item.WastePercentage, UnitCostIrr = item.UnitCostIrr, Notes = item.Notes, SortOrder = item.SortOrder,
    };

    private async Task<BodyEsTemplate?> LoadAsync(Guid id, bool tracking, CancellationToken ct)
    {
        var q = _db.BodyEsTemplates.Include(t => t.ProductFamily).Include(t => t.PanelType).Include(t => t.Items).AsQueryable();
        if (!tracking) q = q.AsNoTracking();
        return await q.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    private static BodyEsTemplateDto ToDto(BodyEsTemplate t)
    {
        var itemDtos = t.Items.OrderBy(i => i.SortOrder).Select(i => new BodyEsTemplateItemDto(
            i.Id, i.ComponentCode, i.DescriptionFa, i.DescriptionEn, i.Category, i.Unit,
            i.QuantityPerPanel, i.WastePercentage, i.UnitCostIrr, i.LineCostIrr, i.Notes, i.SortOrder)).ToList();

        return new BodyEsTemplateDto(
            t.Id, t.TemplateCode, t.TemplateName, t.ProductFamilyId, t.ProductFamily.Name, t.PanelTypeId, t.PanelType.Name,
            t.PanelDimensions, t.RevisionNumber, t.Status.ToString(), t.Notes, itemDtos, t.Items.Sum(i => i.LineCostIrr), t.RowVersion);
    }
}
