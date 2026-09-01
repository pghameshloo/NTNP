using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Exceptions;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Projects;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Domain.Exceptions;

namespace NTNP.Pricing.Application.Projects;

/// <summary>Section 13 — Project header + the "Pricing Settings" wizard step (Section 21 step 5).</summary>
public sealed class ProjectService : IProjectService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public ProjectService(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<PagedResult<ProjectListItemDto>> SearchAsync(PagedQuery query, string? status, CancellationToken ct = default)
    {
        var q = _db.Projects.AsNoTracking().Include(p => p.Customer).Include(p => p.CurrentRevision).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProjectStatus>(status, out var parsedStatus))
            q = q.Where(p => p.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(p => p.ProjectCode.Contains(term) || p.ProjectName.Contains(term) || p.Customer.CompanyName.Contains(term));
        }

        q = query.SortDescending ? q.OrderByDescending(p => p.CreatedAtUtc) : q.OrderBy(p => p.CreatedAtUtc);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var dtos = items.Select(p => new ProjectListItemDto(
            p.Id, p.ProjectCode, p.ProjectName, p.Customer.CompanyName, p.Status.ToString(), p.CurrentRevisionNumber,
            p.CurrentRevision?.TotalProjectSellingPriceIrr, p.QuotationCurrencyCode, p.CreatedAtUtc, p.UpdatedAtUtc)).ToList();

        return new PagedResult<ProjectListItemDto>(dtos, total, page, pageSize);
    }

    public async Task<ProjectDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var project = await _db.Projects.AsNoTracking().Include(p => p.Customer).FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Project), id);
        return ProjectMappers.ToDto(project);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        if (await _db.Projects.AnyAsync(p => p.ProjectCode == request.ProjectCode, ct))
            throw new DomainValidationException($"Project code '{request.ProjectCode}' already exists.");
        if (!await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct))
            throw new NotFoundException(nameof(Customer), request.CustomerId);

        var pricingMethod = Enum.Parse<PricingMethod>(request.PricingMethod);
        Domain.Calculation.PricingCalculationEngine.ValidateShares(request.RialShare, request.ForeignShare);

        var project = new Project
        {
            ProjectCode = request.ProjectCode,
            ProjectName = request.ProjectName,
            CustomerId = request.CustomerId,
            RfqNumber = request.RfqNumber,
            InquiryDate = request.InquiryDate,
            ProjectDescription = request.ProjectDescription,
            CommercialNotes = request.CommercialNotes,
            TechnicalNotes = request.TechnicalNotes,
            QuotationCurrencyCode = request.QuotationCurrencyCode,
            RialShare = request.RialShare,
            ForeignShare = request.ForeignShare,
            PricingProfileId = request.PricingProfileId,
            PricingMethod = pricingMethod,
            PricingRate = request.PricingRate,
            Status = ProjectStatus.Draft,
            CurrentRevisionNumber = 1,
            CreatedByUserId = _currentUser.UserId,
            CreatedByUserName = _currentUser.UserName,
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        var sellingRate = await GetLatestSellingRateAsync(request.QuotationCurrencyCode, ct);

        var revision = new ProjectRevision
        {
            ProjectId = project.Id,
            RevisionNumber = 1,
            Status = ProjectStatus.Draft,
            QuotationCurrencyCode = request.QuotationCurrencyCode,
            RialShare = request.RialShare,
            ForeignShare = request.ForeignShare,
            PricingMethod = pricingMethod,
            PricingRate = request.PricingRate,
            ReconciliationToleranceIrr = 1m,
            SellingExchangeRateId = sellingRate?.Id,
            SellingExchangeRateValue = sellingRate?.SellingRateToIrr ?? 0m,
            SellingExchangeRateEffectiveAtUtc = sellingRate?.EffectiveAtUtc ?? _clock.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            CreatedByUserName = _currentUser.UserName,
        };
        _db.ProjectRevisions.Add(revision);
        await _db.SaveChangesAsync(ct);

        project.CurrentRevisionId = revision.Id;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Created, nameof(Project), project.Id.ToString(), newValue: project, projectId: project.Id, cancellationToken: ct);

        var loaded = await _db.Projects.AsNoTracking().Include(p => p.Customer).FirstAsync(p => p.Id == project.Id, ct);
        return ProjectMappers.ToDto(loaded);
    }

    public async Task<ProjectDto> UpdateInfoAsync(Guid id, UpdateProjectInfoRequest request, CancellationToken ct = default)
    {
        var project = await _db.Projects.Include(p => p.Customer).FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Project), id);

        _db.Entry(project).Property(p => p.RowVersion).OriginalValue = request.RowVersion;

        project.ProjectName = request.ProjectName;
        project.RfqNumber = request.RfqNumber;
        project.InquiryDate = request.InquiryDate;
        project.QuotationNumber = request.QuotationNumber;
        project.QuotationDate = request.QuotationDate;
        project.QuotationValidUntil = request.QuotationValidUntil;
        project.ProjectDescription = request.ProjectDescription;
        project.CommercialNotes = request.CommercialNotes;
        project.TechnicalNotes = request.TechnicalNotes;
        project.UpdatedByUserId = _currentUser.UserId;
        project.UpdatedByUserName = _currentUser.UserName;
        project.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Updated, nameof(Project), project.Id.ToString(), projectId: project.Id, cancellationToken: ct);

        return ProjectMappers.ToDto(project);
    }

    public async Task<ProjectRevisionDto> UpdatePricingSettingsAsync(Guid id, UpdateProjectPricingSettingsRequest request, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw new NotFoundException(nameof(Project), id);

        if (project.CurrentRevisionId is null)
            throw new DomainValidationException("Project has no active revision.");

        var revision = await _db.ProjectRevisions
            .Include(r => r.Lines).ThenInclude(l => l.BomItems)
            .Include(r => r.Lines).ThenInclude(l => l.BodyEsItems)
            .FirstAsync(r => r.Id == project.CurrentRevisionId, ct);

        if (revision.IsImmutable)
            throw new DomainValidationException("Cannot change pricing settings on an approved/locked revision (Section 13: approved revisions are immutable).");

        _db.Entry(project).Property(p => p.RowVersion).OriginalValue = request.RowVersion;

        var pricingMethod = Enum.Parse<PricingMethod>(request.PricingMethod);
        Domain.Calculation.PricingCalculationEngine.ValidateShares(request.RialShare, request.ForeignShare);

        project.QuotationCurrencyCode = request.QuotationCurrencyCode;
        project.RialShare = request.RialShare;
        project.ForeignShare = request.ForeignShare;
        project.PricingProfileId = request.PricingProfileId;
        project.PricingMethod = pricingMethod;
        project.PricingRate = request.PricingRate;
        project.UpdatedByUserId = _currentUser.UserId;
        project.UpdatedByUserName = _currentUser.UserName;
        project.UpdatedAtUtc = _clock.UtcNow;

        var sellingRate = await GetLatestSellingRateAsync(request.QuotationCurrencyCode, ct);

        revision.QuotationCurrencyCode = request.QuotationCurrencyCode;
        revision.RialShare = request.RialShare;
        revision.ForeignShare = request.ForeignShare;
        revision.PricingMethod = pricingMethod;
        revision.PricingRate = request.PricingRate;
        revision.SellingExchangeRateId = sellingRate?.Id;
        revision.SellingExchangeRateValue = sellingRate?.SellingRateToIrr ?? 0m;
        revision.SellingExchangeRateEffectiveAtUtc = sellingRate?.EffectiveAtUtc ?? _clock.UtcNow;

        ProjectRevisionRecalculator.RecalculateRevision(revision);

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Updated, nameof(Project), project.Id.ToString(),
            reason: "Pricing settings changed", projectId: project.Id, projectRevisionId: revision.Id, cancellationToken: ct);

        return ProjectMappers.ToDto(revision);
    }

    private async Task<ExchangeRate?> GetLatestSellingRateAsync(string currencyCode, CancellationToken ct)
    {
        if (string.Equals(currencyCode, "IRR", StringComparison.OrdinalIgnoreCase)) return null;
        return await _db.ExchangeRates.AsNoTracking()
            .Where(r => r.Currency.Code == currencyCode && r.IsActive)
            .OrderByDescending(r => r.EffectiveAtUtc)
            .FirstOrDefaultAsync(ct);
    }
}
