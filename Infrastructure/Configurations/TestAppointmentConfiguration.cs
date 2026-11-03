using Domain.Entities;
using Domain.Enums;
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

        builder.Property(x => x.Status)
            .IsRequired(false)
            .HasDefaultValue(AppointmentStatus.Scheduled);

        builder.Property(x => x.CancelledAt)
            .IsRequired(false);

        builder.Property(x => x.NoShowDate)
            .IsRequired(false);

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

        builder.HasOne(x => x.CancelledByUser)
            .WithMany()
            .HasForeignKey(x => x.CancelledByUserID)
            .IsRequired(false)
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

        builder.HasIndex(x =>
            x.CancelledByUserID);

        builder.HasIndex(x =>
            x.Status);
    }
}
