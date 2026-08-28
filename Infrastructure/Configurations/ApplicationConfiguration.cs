using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApplicationConfiguration
    : IEntityTypeConfiguration<ApplicationD>
{
    public void Configure(
        EntityTypeBuilder<ApplicationD> builder)
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        builder.HasKey(a => a.ApplicationID);


        // =========================================================
        // APPLICATION STATUS
        // =========================================================

        builder.Property(a => a.ApplicationStatus)
            .HasConversion<byte>()
            .IsRequired();


        // =========================================================
        // PAID FEES
        // =========================================================

        builder.Property(a => a.PaidFees)
            .HasPrecision(18, 2);


        // =========================================================
        // PERSON
        // =========================================================

        builder.HasOne(a => a.Person)
            .WithMany(p => p.Applications)
            .HasForeignKey(a => a.ApplicantPersonID)
            .OnDelete(DeleteBehavior.Restrict);


        // =========================================================
        // APPLICATION TYPE
        // =========================================================

        builder.HasOne(a => a.ApplicationType)
            .WithMany()
            .HasForeignKey(a => a.ApplicationTypeID)
            .OnDelete(DeleteBehavior.Restrict);


        // =========================================================
        // CREATED BY USER
        // =========================================================

        builder.HasOne(a => a.CreatedByUser)
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}