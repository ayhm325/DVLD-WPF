using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class InternationalLicenseConfiguration
    : IEntityTypeConfiguration<InternationalLicense>
{
    public void Configure(
        EntityTypeBuilder<InternationalLicense> builder)
    {
        builder.HasKey(x => x.InternationalLicenseID);

        builder.HasIndex(x => x.IssuedUsingLocalLicenseID)
            .IsUnique();

        builder.HasIndex(x => x.DriverID)
            .HasFilter("[IsActive] = 1")
            .IsUnique();

        builder.HasOne(x => x.Application)
            .WithMany()
            .HasForeignKey(x => x.ApplicationID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Driver)
            .WithMany(x => x.InternationalLicenses)
            .HasForeignKey(x => x.DriverID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.IssuedUsingLocalLicense)
            .WithMany()
            .HasForeignKey(x => x.IssuedUsingLocalLicenseID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}