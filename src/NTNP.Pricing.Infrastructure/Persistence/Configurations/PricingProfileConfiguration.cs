using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Infrastructure.Persistence.Configurations;

public class PricingProfileConfiguration : IEntityTypeConfiguration<PricingProfile>
{
    public void Configure(EntityTypeBuilder<PricingProfile> builder)
    {
        builder.ToTable("PricingProfiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.DefaultRate).HasPrecision(18, 8);
        builder.Property(x => x.DefaultRialShare).HasPrecision(18, 8);
        builder.Property(x => x.DefaultForeignShare).HasPrecision(18, 8);
        builder.Property(x => x.DefaultQuotationCurrencyCode).HasMaxLength(3);
        builder.Property(x => x.ReconciliationToleranceIrr).HasPrecision(28, 0);
        builder.Property(x => x.CreatedByUserName).HasMaxLength(200);
        builder.Property(x => x.UpdatedByUserName).HasMaxLength(200);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.Ignore(x => x.EquivalentMultiplier);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
