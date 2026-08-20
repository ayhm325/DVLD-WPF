using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class UserConfiguration
        : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // =========================
            // PRIMARY KEY
            // =========================

            builder.HasKey(u => u.UserId);

            // =========================
            // USERNAME
            // =========================

            builder.Property(u => u.UserName)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(u => u.UserName)
                .IsUnique();

            // =========================
            // PASSWORD
            // =========================

            // BCrypt hash is stored here.
            // The plain-text password is never stored.
            builder.Property(u => u.Password)
                .HasMaxLength(200)
                .IsRequired();

            // =========================
            // PERSON RELATIONSHIP
            // =========================

            builder.HasOne(u => u.Person)
                .WithOne()
                .HasForeignKey<User>(u => u.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}