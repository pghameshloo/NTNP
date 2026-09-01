using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Contracts.PanelTemplates;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Domain.Exceptions;

namespace NTNP.Pricing.Application.PanelTemplates;

public sealed class LookupService : ILookupService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public LookupService(IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<ProductFamilyDto>> ListProductFamiliesAsync(bool includeInactive, CancellationToken ct = default)
    {
        var q = _db.ProductFamilies.AsNoTracking().AsQueryable();
        if (!includeInactive) q = q.Where(f => f.IsActive);
        var items = await q.OrderBy(f => f.Name).ToListAsync(ct);
        return items.Select(f => new ProductFamilyDto(f.Id, f.Code, f.Name, f.VoltageRangeDescription, f.SwitchgearClass, f.IsActive)).ToList();
    }

    public async Task<ProductFamilyDto> CreateProductFamilyAsync(
        string code, string name, string? voltageRange, string? switchgearClass, CancellationToken ct = default)
    {
        if (await _db.ProductFamilies.AnyAsync(f => f.Code == code, ct))
            throw new DomainValidationException($"Product family code '{code}' already exists.");

        var family = new ProductFamily
        {
            Code = code, Name = name, VoltageRangeDescription = voltageRange, SwitchgearClass = switchgearClass,
            CreatedByUserId = _currentUser.UserId, CreatedByUserName = _currentUser.UserName,
        };
        _db.ProductFamilies.Add(family);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Created, nameof(ProductFamily), family.Id.ToString(), newValue: family, cancellationToken: ct);

        return new ProductFamilyDto(family.Id, family.Code, family.Name, family.VoltageRangeDescription, family.SwitchgearClass, family.IsActive);
    }

    public async Task<IReadOnlyList<PanelTypeDto>> ListPanelTypesAsync(bool includeInactive, CancellationToken ct = default)
    {
        var q = _db.PanelTypes.AsNoTracking().AsQueryable();
        if (!includeInactive) q = q.Where(t => t.IsActive);
        var items = await q.OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(ct);
        return items.Select(t => new PanelTypeDto(t.Id, t.Code, t.Name, t.Description, t.SortOrder, t.IsActive)).ToList();
    }

    public async Task<PanelTypeDto> CreatePanelTypeAsync(string code, string name, string? description, int sortOrder, CancellationToken ct = default)
    {
        if (await _db.PanelTypes.AnyAsync(t => t.Code == code, ct))
            throw new DomainValidationException($"Panel type code '{code}' already exists.");

        var panelType = new PanelType
        {
            Code = code, Name = name, Description = description, SortOrder = sortOrder,
            CreatedByUserId = _currentUser.UserId, CreatedByUserName = _currentUser.UserName,
        };
        _db.PanelTypes.Add(panelType);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Created, nameof(PanelType), panelType.Id.ToString(), newValue: panelType, cancellationToken: ct);

        return new PanelTypeDto(panelType.Id, panelType.Code, panelType.Name, panelType.Description, panelType.SortOrder, panelType.IsActive);
    }
}
