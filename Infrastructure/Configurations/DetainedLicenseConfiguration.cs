using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class DetainedLicenseConfiguration
        : IEntityTypeConfiguration<DetainedLicense>
    {
        public void Configure(EntityTypeBuilder<DetainedLicense> builder)
        {
            // Primary Key
            builder.HasKey(d => d.DetainID);



            // Properties

            builder.Property(d => d.FineFees)
                .HasPrecision(18, 2);



            // License Relationship
            builder.HasOne(d => d.License)
                .WithMany()
                .HasForeignKey(d => d.LicenseID)
                .OnDelete(DeleteBehavior.Restrict);



            // Created By User Relationship
            builder.HasOne(d => d.CreatedByUser)
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);



            // Released By User Relationship (Optional)
            builder.HasOne(d => d.ReleasedByUser)
                .WithMany()
                .HasForeignKey(d => d.ReleasedByUserID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);



            // Release Application Relationship (Optional)
            builder.HasOne(d => d.ReleaseApplication)
                .WithMany()
                .HasForeignKey(d => d.ReleaseApplicationID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}