using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PersonConfiguration
    : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(p => p.PersonId);


        builder.Property(p => p.NationalNo)
            .HasMaxLength(20)
            .IsRequired();


        builder.Property(p => p.FirstName)
            .HasMaxLength(50)
            .IsRequired();


        builder.Property(p => p.SecondName)
            .HasMaxLength(50)
            .IsRequired();


        builder.Property(p => p.ThirdName)
            .HasMaxLength(50);


        builder.Property(p => p.LastName)
            .HasMaxLength(50)
            .IsRequired();


        builder.Property(p => p.Address)
            .HasMaxLength(200)
            .IsRequired();


        builder.Property(p => p.Phone)
            .HasMaxLength(20)
            .IsRequired();


        builder.HasIndex(p => p.NationalNo)
            .IsUnique();


        builder.HasOne(p => p.Country)
            .WithMany(c => c.People)
            .HasForeignKey(p => p.NationalityCountryID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}