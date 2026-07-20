using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class LicenseConfiguration
        : IEntityTypeConfiguration<License>
    {
        public void Configure(EntityTypeBuilder<License> builder)
        {
            // Primary Key
            builder.HasKey(l => l.LicenseID);



            // Properties

            builder.Property(l => l.Notes)
                .HasMaxLength(500)
                .IsRequired(false);


            builder.Property(l => l.PaidFees)
                .HasPrecision(18, 2);



            // Application Relationship

            builder.HasOne(l => l.Application)
                .WithMany(a => a.Licenses)
                .HasForeignKey(l => l.ApplicationID)
                .OnDelete(DeleteBehavior.Restrict);



            // Driver Relationship

            builder.HasOne(l => l.Driver)
                .WithMany(d => d.Licenses)
                .HasForeignKey(l => l.DriverID)
                .OnDelete(DeleteBehavior.Restrict);



            // License Class Relationship

            builder.HasOne(l => l.LicenseClassInfo)
                .WithMany()
                .HasForeignKey(l => l.LicenseClass)
                .HasPrincipalKey(c => c.LicenseClassID)
                .OnDelete(DeleteBehavior.Restrict);



            // Created By User Relationship

            builder.HasOne(l => l.CreatedByUser)
                .WithMany()
                .HasForeignKey(l => l.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}