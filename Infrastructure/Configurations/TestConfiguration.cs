using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class TestConfiguration
        : IEntityTypeConfiguration<Test>
    {
        public void Configure(EntityTypeBuilder<Test> builder)
        {
            // Primary Key
            builder.HasKey(t => t.TestID);



            // Properties

            builder.Property(t => t.Notes)
                .HasMaxLength(500)
                .IsRequired(false);



            // TestAppointment (One-To-One)

            builder.HasOne(t => t.TestAppointment)
                .WithOne(a => a.Test)
                .HasForeignKey<Test>(t => t.TestAppointmentID)
                .OnDelete(DeleteBehavior.Restrict);



            // Created By User

            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}