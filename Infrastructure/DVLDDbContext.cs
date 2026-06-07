using Domain.Entities;
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
        public DbSet<ApplicationType> ApplicationTypes { get; set; } = null!;
        public DbSet<TestType> TestTypes { get; set; } = null!;
        public DbSet<LicenseClass> LicenseClasses { get; set; } = null!;
        public DbSet<ApplicationD> Applications { get; set; } = null!;
        public DbSet<LocalDrivingLicenseApplication> LocalDrivingLicenseApplications { get; set; } = null!;
        public DbSet<Test> Tests { get; set; } = null!;
        public DbSet<TestAppointment> TestAppointments { get; set; } = null!;
        public DbSet<License> Licenses { get; set; } = null!;
        public DbSet<Driver> Drivers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Seed data for Countries
            base.OnModelCreating(modelBuilder);

            // Configure one-to-many relationship between Country and Person
            modelBuilder.Entity<Person>()
                .HasOne(p => p.Country)
                .WithMany(c => c.People)
                .HasForeignKey(p => p.NationalityCountryID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-one relationship between User and Person
            modelBuilder.Entity<User>()
                .HasOne(u => u.Person)
                .WithOne()
                .HasForeignKey<User>(u => u.PersonId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between Application and Person
            modelBuilder.Entity<ApplicationD>()
                .HasOne(a => a.Person)
                .WithMany(p => p.Applications)       
                .HasForeignKey(a => a.ApplicantPersonID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between Application and ApplicationType
            modelBuilder.Entity<ApplicationD>()
                .HasOne(a => a.ApplicationType)
                .WithMany()
                .HasForeignKey(a => a.ApplicationTypeID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between Application and User (CreatedByUser)
            modelBuilder.Entity<ApplicationD>()
                .HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Configure one-to-many relationship between LocalDrivingLicenseApplication and Application
            modelBuilder.Entity<LocalDrivingLicenseApplication>()
                .HasOne(ldla => ldla.Application)
                .WithMany()
                .HasForeignKey(ldla => ldla.ApplicationID)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Configure one-to-many relationship between LocalDrivingLicenseApplication and LicenseClass
            modelBuilder.Entity<LocalDrivingLicenseApplication>()
                .HasOne(ldla => ldla.LicenseClass)
                .WithMany()
                .HasForeignKey(ldla => ldla.LicenseClassID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between Test and TestAppointment
            modelBuilder.Entity<Test>()
                .HasOne(t => t.TestAppointment)
                .WithMany()
                .HasForeignKey(t => t.TestAppointmentID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between Test and User (CreatedByUser)
            modelBuilder.Entity<Test>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between TestAppointment and LocalDrivingLicenseApplication
            modelBuilder.Entity<TestAppointment>()
                .HasOne(ta => ta.TestType)
                .WithMany()
                .HasForeignKey(ta => ta.TestTypeID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between TestAppointment and LocalDrivingLicenseApplication
            modelBuilder.Entity<TestAppointment>()
                .HasOne(ta => ta.LocalDrivingLicenseApplication)
                .WithMany()
                .HasForeignKey(ta => ta.LocalDrivingLicenseApplicationID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between TestAppointment and User (CreatedByUser)
            modelBuilder.Entity<TestAppointment>()
                .HasOne(ta => ta.User)
                .WithMany()
                .HasForeignKey(ta => ta.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between TestAppointment and Application (RetakeTestApplication)
            modelBuilder.Entity<TestAppointment>()
                .HasOne(ta => ta.RetakeTestApplication)
                .WithMany()
                .HasForeignKey(ta => ta.RetakeTestApplicationID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between License and Application
            modelBuilder.Entity<License>()
                .HasOne(l => l.Application)
                .WithMany()
                .HasForeignKey(l => l.ApplicationID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between License and Person (Driver)
            modelBuilder.Entity<License>()
                .HasOne(l => l.Driver)
                .WithMany()
                .HasForeignKey(l => l.DriverID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between License and LicenseClass
            modelBuilder.Entity<License>()
                .HasOne(l => l.LicenseClass)
                .WithMany()
                .HasForeignKey(l => l.LicenseClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between License and User (CreatedByUser)
            modelBuilder.Entity<License>()
                .HasOne(l => l.CreatedByUser)
                .WithMany()
                .HasForeignKey(l => l.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between Driver and Person
            modelBuilder.Entity<Driver>()
                .HasOne(d => d.Person)
                .WithMany()
                .HasForeignKey(d => d.PersonID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between Driver and User (CreatedByUser)
            modelBuilder.Entity<Driver>()
                .HasOne(d => d.CreatedByUser)
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);















        }

    }
}
