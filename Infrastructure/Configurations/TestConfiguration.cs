using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class TestConfiguration
    : IEntityTypeConfiguration<Test>
{
    public void Configure(
        EntityTypeBuilder<Test> builder)
    {
        builder.HasKey(t => t.TestID);

        builder.Property(t => t.TestResult)
            .IsRequired();

        builder.Property(t => t.CreatedByUserID)
            .IsRequired();

        builder.Property(t => t.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.HasOne(t => t.TestAppointment)
            .WithOne(a => a.Test)
            .HasForeignKey<Test>(
                t => t.TestAppointmentID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TestAppointmentID);
        builder.HasIndex(t => t.CreatedByUserID);
    }
}
