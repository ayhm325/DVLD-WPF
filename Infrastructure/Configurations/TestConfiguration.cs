using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TestConfiguration
    : IEntityTypeConfiguration<Test>
{
    public void Configure(
        EntityTypeBuilder<Test> builder)
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        builder.HasKey(t =>
            t.TestID);


        // =========================================================
        // PROPERTIES
        // =========================================================

        builder.Property(t =>
                t.TestAppointmentID)
            .IsRequired();


        builder.Property(t =>
                t.TestResult)
            .IsRequired();


        builder.Property(t =>
                t.CreatedByUserID)
            .IsRequired();


        builder.Property(t =>
                t.Notes)
            .HasMaxLength(500)
            .IsRequired(false);


        // =========================================================
        // TEST APPOINTMENT
        // One Test <-> One TestAppointment
        // =========================================================

        builder.HasOne(t =>
                t.TestAppointment)
            .WithOne(a =>
                a.Test)
            .HasForeignKey<Test>(
                t => t.TestAppointmentID)
            .OnDelete(
                DeleteBehavior.Restrict);


        // =========================================================
        // CREATED BY USER
        // =========================================================

        builder.HasOne(t =>
                t.User)
            .WithMany()
            .HasForeignKey(t =>
                t.CreatedByUserID)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}