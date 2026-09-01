using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Infrastructure.Persistence.Configurations;

public class ProductFamilyConfiguration : IEntityTypeConfiguration<ProductFamily>
{
    public void Configure(EntityTypeBuilder<ProductFamily> builder)
    {
        builder.ToTable("ProductFamilies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.VoltageRangeDescription).HasMaxLength(100);
        builder.Property(x => x.SwitchgearClass).HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PanelTypeConfiguration : IEntityTypeConfiguration<PanelType>
{
    public void Configure(EntityTypeBuilder<PanelType> builder)
    {
        builder.ToTable("PanelTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
