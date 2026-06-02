using DVLD.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure
{
    public class DVLDDbContext : DbContext
    {

        public DVLDDbContext(DbContextOptions<DVLDDbContext> options) : base(options) { }

        public DbSet<Person> People { get; set; } = null!;
        public DbSet<Country> Countries { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Fluent API configurations (if needed)

            modelBuilder.Entity<Person>().HasKey(p => p.PersonId);

            // Configure one-to-many relationship between Country and Person
            modelBuilder.Entity<Person>()
                .HasOne(p => p.Country)
                .WithMany(c => c.People)
                .HasForeignKey(p => p.NationalityCountryID);

            // Configure one-to-one relationship between User and Person
            modelBuilder.Entity<User>()
                .HasOne(u => u.Person)
                .WithOne()
                .HasForeignKey<User>(u => u.PersonId);
        }

    }
}
