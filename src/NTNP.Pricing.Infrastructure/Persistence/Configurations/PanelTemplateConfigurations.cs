using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Infrastructure.Persistence.Configurations;

public class PanelTemplateConfiguration : IEntityTypeConfiguration<PanelTemplate>
{
    public void Configure(EntityTypeBuilder<PanelTemplate> builder)
    {
        builder.ToTable("PanelTemplates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TemplateCode).HasMaxLength(60).IsRequired();
        builder.Property(x => x.TemplateName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.VoltageLevel).HasMaxLength(50);
        builder.Property(x => x.TechnicalDescription).HasMaxLength(2000);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.ApprovedByUserName).HasMaxLength(200);
        builder.Property(x => x.CreatedByUserName).HasMaxLength(200);
        builder.Property(x => x.UpdatedByUserName).HasMaxLength(200);

        builder.HasOne(x => x.ProductFamily).WithMany().HasForeignKey(x => x.ProductFamilyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PanelType).WithMany().HasForeignKey(x => x.PanelTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BodyEsTemplate).WithMany().HasForeignKey(x => x.BodyEsTemplateId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TemplateCode, x.RevisionNumber }).IsUnique();
        builder.HasIndex(x => new { x.ProductFamilyId, x.PanelTypeId });

        builder.HasMany(x => x.BomItems).WithOne(i => i.PanelTemplate).HasForeignKey(i => i.PanelTemplateId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PanelTemplateBomItemConfiguration : IEntityTypeConfiguration<PanelTemplateBomItem>
{
    public void Configure(EntityTypeBuilder<PanelTemplateBomItem> builder)
    {
        builder.ToTable("PanelTemplateBomItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuantityPerPanel).HasPrecision(18, 4);
        builder.Property(x => x.WastePercentage).HasPrecision(18, 8);
        builder.Property(x => x.CostMultiplier).HasPrecision(18, 8);
        builder.Property(x => x.Unit).HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
