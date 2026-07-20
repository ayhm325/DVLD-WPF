using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TestAppointmentConfiguration
    : IEntityTypeConfiguration<TestAppointment>
{
    public void Configure(EntityTypeBuilder<TestAppointment> builder)
    {
        builder.HasKey(t => t.TestAppointmentID);


        builder.Property(t => t.PaidFees)
            .HasPrecision(18, 2);


        builder.HasOne(t => t.TestType)
            .WithMany()
            .HasForeignKey(t => t.TestTypeID)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasOne(t => t.LocalDrivingLicenseApplication)
            .WithMany()
            .HasForeignKey(t => t.LocalDrivingLicenseApplicationID)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserID)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasOne(t => t.RetakeTestApplication)
            .WithMany()
            .HasForeignKey(t => t.RetakeTestApplicationID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}