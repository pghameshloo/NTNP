using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Infrastructure.Persistence.Configurations;

public class BodyEsTemplateConfiguration : IEntityTypeConfiguration<BodyEsTemplate>
{
    public void Configure(EntityTypeBuilder<BodyEsTemplate> builder)
    {
        builder.ToTable("BodyEsTemplates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TemplateCode).HasMaxLength(60).IsRequired();
        builder.Property(x => x.TemplateName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PanelDimensions).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedByUserName).HasMaxLength(200);
        builder.Property(x => x.UpdatedByUserName).HasMaxLength(200);

        builder.HasOne(x => x.ProductFamily).WithMany().HasForeignKey(x => x.ProductFamilyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PanelType).WithMany().HasForeignKey(x => x.PanelTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TemplateCode, x.RevisionNumber }).IsUnique();

        builder.HasMany(x => x.Items).WithOne(i => i.BodyEsTemplate).HasForeignKey(i => i.BodyEsTemplateId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class BodyEsTemplateItemConfiguration : IEntityTypeConfiguration<BodyEsTemplateItem>
{
    public void Configure(EntityTypeBuilder<BodyEsTemplateItem> builder)
    {
        builder.ToTable("BodyEsTemplateItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ComponentCode).HasMaxLength(60).IsRequired();
        builder.Property(x => x.DescriptionFa).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DescriptionEn).HasMaxLength(500);
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.Unit).HasMaxLength(20);
        builder.Property(x => x.QuantityPerPanel).HasPrecision(18, 4);
        builder.Property(x => x.WastePercentage).HasPrecision(18, 8);
        builder.Property(x => x.UnitCostIrr).HasPrecision(28, 0);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.Ignore(x => x.LineCostIrr);
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
