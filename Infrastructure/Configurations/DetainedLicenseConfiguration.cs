using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class DetainedLicenseConfiguration
    : IEntityTypeConfiguration<DetainedLicense>
{
    public void Configure(
        EntityTypeBuilder<DetainedLicense> builder)
    {
        builder.HasKey(d => d.DetainID);

        builder.Property(d => d.FineFees)
            .HasPrecision(18, 2);

        builder.HasOne(d => d.License)
            .WithMany()
            .HasForeignKey(d => d.LicenseID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.CreatedByUser)
            .WithMany()
            .HasForeignKey(d => d.CreatedByUserID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ReleasedByUser)
            .WithMany()
            .HasForeignKey(d => d.ReleasedByUserID)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ReleaseApplication)
            .WithMany()
            .HasForeignKey(d => d.ReleaseApplicationID)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new
        {
            d.LicenseID,
            d.IsReleased
        })
            .HasFilter("[IsReleased] = 0")
            .IsUnique();

        builder.HasIndex(d => d.ReleaseApplicationID);
    }
}