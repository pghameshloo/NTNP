using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Domain.Common;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Infrastructure.Identity;

namespace NTNP.Pricing.Infrastructure.Persistence;

/// <summary>
/// The single EF Core DbContext for the whole system (Section 5: Infrastructure owns "EF Core, SQL
/// Server, Identity, repositories, audit persistence"). Extends <see cref="IdentityDbContext{TUser,TRole,TKey}"/>
/// for ASP.NET Core Identity tables and implements <see cref="IApplicationDbContext"/> so the
/// Application layer never references EF Core's concrete <c>DbContext</c> type.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<ProductFamily> ProductFamilies => Set<ProductFamily>();
    public DbSet<PanelType> PanelTypes => Set<PanelType>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentPrice> EquipmentPrices => Set<EquipmentPrice>();
    public DbSet<PanelTemplate> PanelTemplates => Set<PanelTemplate>();
    public DbSet<PanelTemplateBomItem> PanelTemplateBomItems => Set<PanelTemplateBomItem>();
    public DbSet<BodyEsTemplate> BodyEsTemplates => Set<BodyEsTemplate>();
    public DbSet<BodyEsTemplateItem> BodyEsTemplateItems => Set<BodyEsTemplateItem>();
    public DbSet<PricingProfile> PricingProfiles => Set<PricingProfile>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectRevision> ProjectRevisions => Set<ProjectRevision>();
    public DbSet<ProjectLine> ProjectLines => Set<ProjectLine>();
    public DbSet<ProjectLineBomItem> ProjectLineBomItems => Set<ProjectLineBomItem>();
    public DbSet<ProjectLineBodyEsItem> ProjectLineBodyEsItems => Set<ProjectLineBodyEsItem>();
    public DbSet<ProjectLineOverride> ProjectLineOverrides => Set<ProjectLineOverride>();
    public DbSet<ApprovalRecord> ApprovalRecords => Set<ApprovalRecord>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
    public DbSet<CompanySettings> CompanySettingsSet => Set<CompanySettings>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AssignPlaceholderRowVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AssignPlaceholderRowVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// SQL Server's `rowversion` column type is exclusively server-generated — on both INSERT and
    /// UPDATE, EF Core excludes it from the statement it sends and the engine always supplies the
    /// authoritative value (which is what actually changes on every update and is what the real
    /// optimistic-concurrency check compares). So on a real SQL Server database this method's
    /// assignments are never used. It exists purely so non-SQL-Server providers used in tests
    /// (EF Core InMemory, SQLite) — which have no built-in byte[] rowversion generator — see a
    /// fresh, non-null value on every insert and update, the same way SQL Server's engine would
    /// bump it, so <see cref="DbUpdateConcurrencyException"/> is correctly raised in those tests too.
    /// </summary>
    private void AssignPlaceholderRowVersions()
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray()[..8];
            }
        }
    }
}
