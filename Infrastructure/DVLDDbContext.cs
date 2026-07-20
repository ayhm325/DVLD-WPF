using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public class DVLDDbContext : DbContext
    {
        public DVLDDbContext(DbContextOptions<DVLDDbContext> options)
            : base(options)
        {
        }


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


            // تحميل جميع ملفات IEntityTypeConfiguration تلقائياً
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(DVLDDbContext).Assembly
            );
        }
    }
}