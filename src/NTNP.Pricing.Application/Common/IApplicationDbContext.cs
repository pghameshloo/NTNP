using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
    // Fully qualified: within the Application assembly, the sibling namespace
    // NTNP.Pricing.Application.Equipment makes the bare name "Equipment" ambiguous here.
    DbSet<Domain.Entities.Equipment> Equipment { get; }
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

    /// <summary>
    /// Exposes EF Core's change-tracker entry for a tracked entity so Application services can set
    /// <c>OriginalValue</c> on the RowVersion property from a client-supplied token (Section 31
    /// optimistic concurrency) without depending on EF Core's <c>DbContext</c> type directly.
    /// </summary>
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
}
