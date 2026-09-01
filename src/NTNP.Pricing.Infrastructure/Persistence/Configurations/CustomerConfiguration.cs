using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.CompanyName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Industry).HasMaxLength(150);
        builder.Property(x => x.RegistrationNumber).HasMaxLength(100);
        builder.Property(x => x.TaxId).HasMaxLength(100);
        builder.Property(x => x.ContactPerson).HasMaxLength(150);
        builder.Property(x => x.ContactPosition).HasMaxLength(150);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(1000);
        builder.Property(x => x.CreatedByUserName).HasMaxLength(200);
        builder.Property(x => x.UpdatedByUserName).HasMaxLength(200);

        builder.HasIndex(x => x.CustomerCode).IsUnique();
        builder.HasIndex(x => x.CompanyName);
        builder.HasIndex(x => x.IsActive);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
