using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Application.Common;

/// <summary>
/// The Application layer's only view of persistence: a set of queryable/trackable
/// <see cref="DbSet{TEntity}"/> plus <see cref="SaveChangesAsync"/>. Implemented by
/// <c>NTNP.Pricing.Infrastructure.Persistence.ApplicationDbContext</c>. Application code depends on
/// this interface (not EF Core's <c>DbContext</c> base type, and never on Infrastructure), which
/// keeps EF Core an implementation detail and lets tests substitute an EF Core InMemory/SQLite
/// context that also implements this interface.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<Currency> Currencies { get; }
    DbSet<ExchangeRate> ExchangeRates { get; }
    DbSet<ProductFamily> ProductFamilies { get; }
    DbSet<PanelType> PanelTypes { get; }
    DbSet<Equipment> Equipment { get; }
    DbSet<EquipmentPrice> EquipmentPrices { get; }
    DbSet<PanelTemplate> PanelTemplates { get; }
    DbSet<PanelTemplateBomItem> PanelTemplateBomItems { get; }
    DbSet<BodyEsTemplate> BodyEsTemplates { get; }
    DbSet<BodyEsTemplateItem> BodyEsTemplateItems { get; }
    DbSet<PricingProfile> PricingProfiles { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectRevision> ProjectRevisions { get; }
    DbSet<ProjectLine> ProjectLines { get; }
    DbSet<ProjectLineBomItem> ProjectLineBomItems { get; }
    DbSet<ProjectLineBodyEsItem> ProjectLineBodyEsItems { get; }
    DbSet<ProjectLineOverride> ProjectLineOverrides { get; }
    DbSet<ApprovalRecord> ApprovalRecords { get; }
    DbSet<AuditLogEntry> AuditLogEntries { get; }
    DbSet<StoredFile> StoredFiles { get; }
    DbSet<CompanySettings> CompanySettingsSet { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
