using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class InternationalLicenseConfiguration
    : IEntityTypeConfiguration<InternationalLicense>
{
    public void Configure(EntityTypeBuilder<InternationalLicense> builder)
    {
        builder.HasKey(i => i.InternationalLicenseID);


        builder.HasOne(i => i.Application)
            .WithMany()
            .HasForeignKey(i => i.ApplicationID)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasOne(i => i.Driver)
            .WithMany(d => d.InternationalLicenses)
            .HasForeignKey(i => i.DriverID)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasOne(i => i.IssuedUsingLocalLicense)
            .WithMany()
            .HasForeignKey(i => i.IssuedUsingLocalLicenseID)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasOne(i => i.CreatedByUser)
            .WithMany()
            .HasForeignKey(i => i.CreatedByUserID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}