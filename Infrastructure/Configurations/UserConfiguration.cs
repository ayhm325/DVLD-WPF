using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class UserConfiguration
    : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.UserId);


        builder.Property(u => u.UserName)
            .HasMaxLength(50)
            .IsRequired();


        builder.Property(u => u.Password)
            .HasMaxLength(200)
            .IsRequired();


        builder.HasIndex(u => u.UserName)
            .IsUnique();


        builder.HasOne(u => u.Person)
            .WithOne()
            .HasForeignKey<User>(u => u.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}