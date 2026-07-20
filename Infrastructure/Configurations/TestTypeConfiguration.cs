using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TestTypeConfiguration
    : IEntityTypeConfiguration<TestType>
{
    public void Configure(EntityTypeBuilder<TestType> builder)
    {
        builder.HasKey(t => t.TestTypeId);


        builder.Property(t => t.TestTypeTitle)
            .HasMaxLength(100)
            .IsRequired();


        builder.Property(t => t.TestTypeDescription)
            .HasMaxLength(250)
            .IsRequired();


        builder.Property(t => t.TestTypeFees)
            .HasPrecision(18, 2);
    }
}