using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DriverConfiguration
    : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.HasKey(d => d.DriverID);


        builder.HasOne(d => d.Person)
            .WithMany()
            .HasForeignKey(d => d.PersonID)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasOne(d => d.CreatedByUser)
            .WithMany()
            .HasForeignKey(d => d.CreatedByUserID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}