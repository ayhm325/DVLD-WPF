using Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<DetainedLicense> DetainedLicenses { get; set; } = null!;
        public DbSet<InternationalLicense> InternationalLicenses { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Person & Country
            modelBuilder.Entity<Person>()
                .HasOne(p => p.Country)
                .WithMany(c => c.People)
                .HasForeignKey(p => p.NationalityCountryID)
                .OnDelete(DeleteBehavior.Restrict);

            // User & Person
            modelBuilder.Entity<User>()
                .HasOne(u => u.Person)
                .WithOne()
                .HasForeignKey<User>(u => u.PersonId)
                .OnDelete(DeleteBehavior.Restrict);

            // ApplicationD Relationships
            modelBuilder.Entity<ApplicationD>()
                .HasOne(a => a.Person)
                .WithMany(p => p.Applications)
                .HasForeignKey(a => a.ApplicantPersonID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApplicationD>()
                .HasOne(a => a.ApplicationType)
                .WithMany()
                .HasForeignKey(a => a.ApplicationTypeID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApplicationD>()
                .HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            // LocalDrivingLicenseApplication Relationships
            modelBuilder.Entity<LocalDrivingLicenseApplication>()
                .HasOne(ldla => ldla.Application)
                .WithMany()
                .HasForeignKey(ldla => ldla.ApplicationID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LocalDrivingLicenseApplication>()
                .HasOne(ldla => ldla.LicenseClass)
                .WithMany()
                .HasForeignKey(ldla => ldla.LicenseClassID)
                .OnDelete(DeleteBehavior.Restrict);

            // Test Relationships
            modelBuilder.Entity<Test>()
                .HasOne(t => t.TestAppointment)
                .WithOne(a => a.Test)
                .HasForeignKey<Test>(t => t.TestAppointmentID);

            modelBuilder.Entity<Test>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            // TestAppointment Relationships
            modelBuilder.Entity<TestAppointment>()
                .HasOne(ta => ta.TestType)
                .WithMany()
                .HasForeignKey(ta => ta.TestTypeID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TestAppointment>()
                .HasOne(ta => ta.LocalDrivingLicenseApplication)
                .WithMany()
                .HasForeignKey(ta => ta.LocalDrivingLicenseApplicationID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TestAppointment>()
                .HasOne(ta => ta.User)
                .WithMany()
                .HasForeignKey(ta => ta.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TestAppointment>()
                .HasOne(ta => ta.RetakeTestApplication)
                .WithMany()
                .HasForeignKey(ta => ta.RetakeTestApplicationID)
                .OnDelete(DeleteBehavior.Restrict);

            // License Relationships
            modelBuilder.Entity<License>()
                .HasOne(l => l.Application)
                .WithMany(a => a.Licenses)
                .HasForeignKey(l => l.ApplicationID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<License>()
                .HasOne(l => l.Driver)
                .WithMany(d => d.Licenses)
                .HasForeignKey(l => l.DriverID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<License>()
                .HasOne(l => l.LicenseClassInfo)
                .WithMany()
                .HasForeignKey(l => l.LicenseClass)
                .HasPrincipalKey(lc => lc.LicenseClassID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<License>()
                .HasOne(l => l.CreatedByUser)
                .WithMany()
                .HasForeignKey(l => l.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Driver Relationships
            modelBuilder.Entity<Driver>()
                .HasOne(d => d.Person)
                .WithMany()
                .HasForeignKey(d => d.PersonID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Driver>()
                .HasOne(d => d.CreatedByUser)
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            // DetainedLicense Relationships
            modelBuilder.Entity<DetainedLicense>()
                .HasOne(dl => dl.License)
                .WithMany()
                .HasForeignKey(dl => dl.LicenseID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetainedLicense>()
                .HasOne(dl => dl.CreatedByUser)
                .WithMany()
                .HasForeignKey(dl => dl.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetainedLicense>()
                .HasOne(dl => dl.ReleasedByUser)
                .WithMany()
                .HasForeignKey(dl => dl.ReleasedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetainedLicense>()
                .HasOne(dl => dl.ReleaseApplication)
                .WithMany()
                .HasForeignKey(dl => dl.ReleaseApplicationID)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<InternationalLicense>()
                .HasOne(i => i.Application)
                .WithMany()
                .HasForeignKey(i => i.ApplicationID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InternationalLicense>()
                .HasOne(i => i.Driver)
                .WithMany(d => d.InternationalLicenses)
                .HasForeignKey(i => i.DriverID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InternationalLicense>()
                .HasOne(i => i.IssuedUsingLocalLicense)
                .WithMany()
                .HasForeignKey(i => i.IssuedUsingLocalLicenseID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InternationalLicense>()
                .HasOne(i => i.CreatedByUser)
                .WithMany()
                .HasForeignKey(i => i.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}