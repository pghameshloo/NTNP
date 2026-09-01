using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProjectCode).HasMaxLength(60).IsRequired();
        builder.Property(x => x.ProjectName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.RfqNumber).HasMaxLength(100);
        builder.Property(x => x.QuotationNumber).HasMaxLength(100);
        builder.Property(x => x.ProjectDescription).HasMaxLength(2000);
        builder.Property(x => x.CommercialNotes).HasMaxLength(2000);
        builder.Property(x => x.TechnicalNotes).HasMaxLength(2000);
        builder.Property(x => x.QuotationCurrencyCode).HasMaxLength(3);
        builder.Property(x => x.RialShare).HasPrecision(18, 8);
        builder.Property(x => x.ForeignShare).HasPrecision(18, 8);
        builder.Property(x => x.PricingRate).HasPrecision(18, 8);
        builder.Property(x => x.CreatedByUserName).HasMaxLength(200);
        builder.Property(x => x.UpdatedByUserName).HasMaxLength(200);
        builder.Property(x => x.ReviewedByUserName).HasMaxLength(200);
        builder.Property(x => x.ApprovedByUserName).HasMaxLength(200);

        builder.HasOne(x => x.Customer).WithMany(c => c.Projects).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PricingProfile).WithMany().HasForeignKey(x => x.PricingProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CurrentRevision).WithMany().HasForeignKey(x => x.CurrentRevisionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ProjectCode).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CustomerId);

        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ProjectRevisionConfiguration : IEntityTypeConfiguration<ProjectRevision>
{
    public void Configure(EntityTypeBuilder<ProjectRevision> builder)
    {
        builder.ToTable("ProjectRevisions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuotationCurrencyCode).HasMaxLength(3);
        builder.Property(x => x.RialShare).HasPrecision(18, 8);
        builder.Property(x => x.ForeignShare).HasPrecision(18, 8);
        builder.Property(x => x.PricingRate).HasPrecision(18, 8);
        builder.Property(x => x.ReconciliationToleranceIrr).HasPrecision(28, 0);
        builder.Property(x => x.SellingExchangeRateValue).HasPrecision(28, 6);

        builder.Property(x => x.TotalEquipmentCostIrr).HasPrecision(28, 0);
        builder.Property(x => x.TotalBodyEsCostIrr).HasPrecision(28, 0);
        builder.Property(x => x.TotalOtherDirectCostIrr).HasPrecision(28, 0);
        builder.Property(x => x.TotalProjectCostIrr).HasPrecision(28, 0);
        builder.Property(x => x.TotalProjectSellingPriceIrr).HasPrecision(28, 0);
        builder.Property(x => x.TotalRialPayable).HasPrecision(28, 0);
        builder.Property(x => x.TotalForeignPayable).HasPrecision(28, 6);
        builder.Property(x => x.ProjectProfitIrr).HasPrecision(28, 0);
        builder.Property(x => x.ProjectGrossMargin).HasPrecision(18, 8);
        builder.Property(x => x.ReconciliationDifferenceIrr).HasPrecision(28, 6);

        builder.Property(x => x.SupersededReason).HasMaxLength(500);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.ApprovedByUserName).HasMaxLength(200);
        builder.Property(x => x.CreatedByUserName).HasMaxLength(200);
        builder.Property(x => x.UpdatedByUserName).HasMaxLength(200);

        builder.HasOne(x => x.Project).WithMany(p => p.Revisions).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ProjectId, x.RevisionNumber }).IsUnique();

        builder.HasMany(x => x.Lines).WithOne(l => l.ProjectRevision).HasForeignKey(l => l.ProjectRevisionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ApprovalRecords).WithOne(a => a.ProjectRevision).HasForeignKey(a => a.ProjectRevisionId).OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.IsImmutable);
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public class ProjectLineConfiguration : IEntityTypeConfiguration<ProjectLine>
{
    public void Configure(EntityTypeBuilder<ProjectLine> builder)
    {
        builder.ToTable("ProjectLines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CellCode).HasMaxLength(60).IsRequired();
        builder.Property(x => x.PanelTemplateCodeSnapshot).HasMaxLength(60);
        builder.Property(x => x.ProductFamilyNameSnapshot).HasMaxLength(150);
        builder.Property(x => x.PanelTypeNameSnapshot).HasMaxLength(150);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.VoltageLevel).HasMaxLength(50);

        builder.Property(x => x.QuantityOfPanels).HasPrecision(18, 4);

        foreach (var moneyProp in new[]
                 {
                     nameof(ProjectLine.EquipmentCostPerPanel), nameof(ProjectLine.BodyEsCostPerPanel),
                     nameof(ProjectLine.OtherDirectCostPerPanel), nameof(ProjectLine.TotalCostPerPanel),
                     nameof(ProjectLine.TotalLineCost), nameof(ProjectLine.SellingPricePerPanel),
                     nameof(ProjectLine.TotalLineSellingPrice), nameof(ProjectLine.RialPayableAmount),
                     nameof(ProjectLine.ProfitIrr),
                 })
        {
            builder.Property(moneyProp).HasPrecision(28, 0);
        }

        builder.Property(x => x.PricingRateApplied).HasPrecision(18, 8);
        builder.Property(x => x.RialShareApplied).HasPrecision(18, 8);
        builder.Property(x => x.ForeignShareApplied).HasPrecision(18, 8);
        builder.Property(x => x.GrossMargin).HasPrecision(18, 8);
        builder.Property(x => x.SellingExchangeRateApplied).HasPrecision(28, 6);
        builder.Property(x => x.ForeignPayableAmount).HasPrecision(28, 6);
        builder.Property(x => x.ReconciliationDifferenceIrr).HasPrecision(28, 6);

        builder.HasMany(x => x.BomItems).WithOne(i => i.ProjectLine).HasForeignKey(i => i.ProjectLineId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.BodyEsItems).WithOne(i => i.ProjectLine).HasForeignKey(i => i.ProjectLineId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Overrides).WithOne(o => o.ProjectLine).HasForeignKey(o => o.ProjectLineId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ProjectRevisionId, x.LineNumber }).IsUnique();
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public class ProjectLineBomItemConfiguration : IEntityTypeConfiguration<ProjectLineBomItem>
{
    public void Configure(EntityTypeBuilder<ProjectLineBomItem> builder)
    {
        builder.ToTable("ProjectLineBomItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EquipmentCodeSnapshot).HasMaxLength(60).IsRequired();
        builder.Property(x => x.DescriptionSnapshot).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PartNumberSnapshot).HasMaxLength(100);
        builder.Property(x => x.BrandSnapshot).HasMaxLength(100);
        builder.Property(x => x.ModelSnapshot).HasMaxLength(100);
        builder.Property(x => x.Unit).HasMaxLength(20);
        builder.Property(x => x.PurchaseCurrencyCodeSnapshot).HasMaxLength(3);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.Property(x => x.QuantityPerPanel).HasPrecision(18, 4);
        builder.Property(x => x.WastePercentage).HasPrecision(18, 8);
        builder.Property(x => x.AdjustedQuantityPerPanel).HasPrecision(18, 4);
        builder.Property(x => x.PurchaseExchangeRateSnapshot).HasPrecision(28, 6);
        builder.Property(x => x.UnitCostIrrSnapshot).HasPrecision(28, 0);
        builder.Property(x => x.LineCostIrr).HasPrecision(28, 0);
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public class ProjectLineBodyEsItemConfiguration : IEntityTypeConfiguration<ProjectLineBodyEsItem>
{
    public void Configure(EntityTypeBuilder<ProjectLineBodyEsItem> builder)
    {
        builder.ToTable("ProjectLineBodyEsItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ComponentCodeSnapshot).HasMaxLength(60).IsRequired();
        builder.Property(x => x.DescriptionSnapshot).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.Property(x => x.QuantityPerPanel).HasPrecision(18, 4);
        builder.Property(x => x.WastePercentage).HasPrecision(18, 8);
        builder.Property(x => x.AdjustedQuantityPerPanel).HasPrecision(18, 4);
        builder.Property(x => x.UnitCostIrrSnapshot).HasPrecision(28, 0);
        builder.Property(x => x.LineCostIrr).HasPrecision(28, 0);
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public class ProjectLineOverrideConfiguration : IEntityTypeConfiguration<ProjectLineOverride>
{
    public void Configure(EntityTypeBuilder<ProjectLineOverride> builder)
    {
        builder.ToTable("ProjectLineOverrides");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FieldName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OldValue).HasMaxLength(500);
        builder.Property(x => x.NewValue).HasMaxLength(500);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UserName).HasMaxLength(200);
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
