using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.UserId);

        builder.Property(u => u.UserName)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(u => u.UserName)
            .IsUnique();

        builder.Property(u => u.Password)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<int>()
            .HasDefaultValue(UserRole.Staff)
            .IsRequired();

        builder.HasOne(u => u.Person)
            .WithOne()
            .HasForeignKey<User>(u => u.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}