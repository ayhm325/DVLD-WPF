using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApplicationTypeConfiguration
    : IEntityTypeConfiguration<ApplicationType>
{
    public void Configure(EntityTypeBuilder<ApplicationType> builder)
    {
        builder.HasKey(a => a.ApplicationTypeId);


        builder.Property(a => a.ApplicationTypeTitle)
            .HasMaxLength(100)
            .IsRequired();


        builder.Property(a => a.ApplicationFees)
            .HasPrecision(18, 2);
    }
}