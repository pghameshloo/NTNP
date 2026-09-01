using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Infrastructure.Persistence.Configurations;

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("Equipment");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(60).IsRequired();
        builder.Property(x => x.TechnicalPartNumber).HasMaxLength(100);
        builder.Property(x => x.DescriptionFa).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DescriptionEn).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.Subcategory).HasMaxLength(100);
        builder.Property(x => x.Brand).HasMaxLength(100);
        builder.Property(x => x.Model).HasMaxLength(100);
        builder.Property(x => x.Manufacturer).HasMaxLength(150);
        builder.Property(x => x.Supplier).HasMaxLength(150);
        builder.Property(x => x.Unit).HasMaxLength(20);
        builder.Property(x => x.CreatedByUserName).HasMaxLength(200);
        builder.Property(x => x.UpdatedByUserName).HasMaxLength(200);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.TechnicalPartNumber);
        builder.HasIndex(x => new { x.Category, x.Subcategory });
        builder.HasIndex(x => x.IsActive);

        builder.Ignore(x => x.CurrentPrice);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class EquipmentPriceConfiguration : IEntityTypeConfiguration<EquipmentPrice>
{
    public void Configure(EntityTypeBuilder<EquipmentPrice> builder)
    {
        builder.ToTable("EquipmentPrices");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PurchaseCurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.ForeignUnitPrice).HasPrecision(28, 6);
        builder.Property(x => x.RialUnitPrice).HasPrecision(28, 0);
        builder.Property(x => x.PurchaseExchangeRateSnapshot).HasPrecision(28, 6);
        builder.Property(x => x.FinalUnitCostIrr).HasPrecision(28, 0);
        builder.Property(x => x.PriceSourceText).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedByUserName).HasMaxLength(200);
        builder.Property(x => x.UpdatedByUserName).HasMaxLength(200);

        builder.HasOne(x => x.Equipment)
            .WithMany(e => e.Prices)
            .HasForeignKey(x => x.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.EquipmentId, x.EffectiveAtUtc });
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
