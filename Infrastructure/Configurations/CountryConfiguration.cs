using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CountryConfiguration
    : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.HasKey(c => c.CountryId);


        builder.Property(c => c.CountryName)
            .HasMaxLength(100)
            .IsRequired();


        builder.HasIndex(c => c.CountryName)
            .IsUnique();
    }
}