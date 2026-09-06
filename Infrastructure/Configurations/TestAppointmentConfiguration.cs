using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class TestAppointmentConfiguration
    : IEntityTypeConfiguration<TestAppointment>
{
    public void Configure(
        EntityTypeBuilder<TestAppointment> builder)
    {
        builder.HasKey(x => x.TestAppointmentID);

        builder.Property(x => x.PaidFees)
            .HasPrecision(18, 2);

        builder.Property(x => x.AppointmentDate)
            .IsRequired();

        builder.HasOne(x => x.TestType)
            .WithMany()
            .HasForeignKey(x => x.TestTypeID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LocalDrivingLicenseApplication)
            .WithMany()
            .HasForeignKey(x =>
                x.LocalDrivingLicenseApplicationID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RetakeTestApplication)
            .WithMany()
            .HasForeignKey(x =>
                x.RetakeTestApplicationID)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x =>
            new
            {
                x.LocalDrivingLicenseApplicationID,
                x.TestTypeID
            });

        builder.HasIndex(x =>
            new
            {
                x.CreatedByUserID,
                x.AppointmentDate
            });

        builder.HasIndex(x =>
            new
            {
                x.LocalDrivingLicenseApplicationID,
                x.AppointmentDate
            });

        builder.HasIndex(x =>
            x.RetakeTestApplicationID);
    }
}