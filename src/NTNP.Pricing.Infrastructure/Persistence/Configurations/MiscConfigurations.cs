using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Infrastructure.Identity;

namespace NTNP.Pricing.Infrastructure.Persistence.Configurations;

public class ApprovalRecordConfiguration : IEntityTypeConfiguration<ApprovalRecord>
{
    public void Configure(EntityTypeBuilder<ApprovalRecord> builder)
    {
        builder.ToTable("ApprovalRecords");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Comments).HasMaxLength(2000);
        builder.Property(x => x.DecidedByUserName).HasMaxLength(200);
        builder.Property(x => x.TotalProjectCostIrrAtDecision).HasPrecision(28, 0);
        builder.Property(x => x.TotalProjectSellingPriceIrrAtDecision).HasPrecision(28, 0);
        builder.Property(x => x.ProjectGrossMarginAtDecision).HasPrecision(18, 8);
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLogEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserName).HasMaxLength(200);
        builder.Property(x => x.EntityType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
        // No explicit HasColumnType: EF Core already maps unbounded `string` to `nvarchar(max)` on
        // SQL Server by convention, and a literal "nvarchar(max)" column type breaks other
        // providers (e.g. SQLite in tests) which don't understand that syntax.
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.IpAddress).HasMaxLength(64);

        builder.HasIndex(x => x.AtUtc);
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.ProjectId);

        // Append-only log: rows are never updated after insert, so RowVersion is intentionally
        // not mapped as a concurrency token here (there is nothing to conflict on).
        builder.Ignore(x => x.RowVersion);
    }
}

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(150);
        builder.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Sha256Hash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedByUserName).HasMaxLength(200);
        builder.Property(x => x.UpdatedByUserName).HasMaxLength(200);

        builder.HasIndex(x => x.ProjectId);
        builder.HasIndex(x => x.Category);
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public class CompanySettingsConfiguration : IEntityTypeConfiguration<CompanySettings>
{
    public void Configure(EntityTypeBuilder<CompanySettings> builder)
    {
        builder.ToTable("CompanySettings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LegalNameEn).HasMaxLength(200);
        builder.Property(x => x.LegalNameFa).HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(1000);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.Website).HasMaxLength(200);
        builder.Property(x => x.LogoStoragePath).HasMaxLength(1000);
        builder.Property(x => x.StampImageStoragePath).HasMaxLength(1000);
        builder.Property(x => x.DefaultQuotationTitleFa).HasMaxLength(300);
        builder.Property(x => x.DefaultQuotationTitleEn).HasMaxLength(300);
        builder.Property(x => x.ConfidentialityLabelFa).HasMaxLength(100);
        builder.Property(x => x.ConfidentialityLabelEn).HasMaxLength(100);
        foreach (var termProp in new[]
                 {
                     nameof(CompanySettings.DefaultDeliveryTerms), nameof(CompanySettings.DefaultPaymentTerms),
                     nameof(CompanySettings.DefaultWarrantyTerms), nameof(CompanySettings.DefaultInspectionTerms),
                     nameof(CompanySettings.DefaultPackingTerms), nameof(CompanySettings.DefaultTransportationTerms),
                     nameof(CompanySettings.DefaultTaxesAndDutiesNote), nameof(CompanySettings.DefaultScopeExclusions),
                 })
        {
            builder.Property(termProp).HasMaxLength(2000);
        }
        builder.Property(x => x.PreparedByName).HasMaxLength(150);
        builder.Property(x => x.PreparedByPosition).HasMaxLength(150);
        builder.Property(x => x.CommercialManagerName).HasMaxLength(150);
        builder.Property(x => x.CommercialManagerPosition).HasMaxLength(150);
        builder.Property(x => x.ManagingDirectorName).HasMaxLength(150);
        builder.Property(x => x.ManagingDirectorPosition).HasMaxLength(150);
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedByIp).HasMaxLength(64);
        builder.Property(x => x.RevokedByIp).HasMaxLength(64);
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.Ignore(x => x.IsActive);
    }
}
