using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApplicationConfiguration
    : IEntityTypeConfiguration<ApplicationD>
{
    public void Configure(EntityTypeBuilder<ApplicationD> builder)
    {
        builder.HasKey(a => a.ApplicationID);


        builder.Property(a => a.PaidFees)
            .HasPrecision(18, 2);


        builder.HasOne(a => a.Person)
            .WithMany(p => p.Applications)
            .HasForeignKey(a => a.ApplicantPersonID)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasOne(a => a.ApplicationType)
            .WithMany()
            .HasForeignKey(a => a.ApplicationTypeID)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasOne(a => a.CreatedByUser)
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}