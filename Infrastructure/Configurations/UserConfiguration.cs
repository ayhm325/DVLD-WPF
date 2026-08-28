using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class UserConfiguration
    : IEntityTypeConfiguration<User>
{
    public void Configure(
        EntityTypeBuilder<User> builder)
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        builder.HasKey(u => u.UserId);


        // =========================================================
        // USERNAME
        // =========================================================

        builder.Property(u => u.UserName)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(u => u.UserName)
            .IsUnique();


        // =========================================================
        // PASSWORD
        // =========================================================

        // BCrypt hash
        // Never store plain-text passwords.

        builder.Property(u => u.Password)
            .HasMaxLength(200)
            .IsRequired();


        // =========================================================
        // ACTIVE
        // =========================================================

        builder.Property(u => u.IsActive)
            .IsRequired();


        // =========================================================
        // PERSON
        // One Person -> One User
        // =========================================================

        builder.HasOne(u => u.Person)
            .WithOne()
            .HasForeignKey<User>(
                u => u.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
