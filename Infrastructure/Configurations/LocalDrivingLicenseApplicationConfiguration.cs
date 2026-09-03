using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class LocalDrivingLicenseApplicationConfiguration
    : IEntityTypeConfiguration<LocalDrivingLicenseApplication>
{
    public void Configure(
        EntityTypeBuilder<LocalDrivingLicenseApplication> builder)
    {
        builder.HasKey(
            l => l.LocalDrivingLicenseApplicationID);

        builder.HasIndex(
            l => l.ApplicationID)
            .IsUnique()
            .HasDatabaseName(
                "UX_LocalDrivingLicenseApplications_ApplicationID");

        builder.HasOne(l => l.Application)
            .WithMany()
            .HasForeignKey(l => l.ApplicationID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.LicenseClass)
            .WithMany()
            .HasForeignKey(l => l.LicenseClassID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}