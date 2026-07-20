using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class LicenseClassConfiguration
    : IEntityTypeConfiguration<LicenseClass>
{
    public void Configure(EntityTypeBuilder<LicenseClass> builder)
    {
        builder.HasKey(l => l.LicenseClassID);


        builder.Property(l => l.ClassName)
            .HasMaxLength(100)
            .IsRequired();


        builder.Property(l => l.ClassDescription)
            .HasMaxLength(500)
            .IsRequired();


        builder.Property(l => l.ClassFees)
            .HasPrecision(18, 2);
    }
}