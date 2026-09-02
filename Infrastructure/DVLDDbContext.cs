using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class DVLDDbContext : DbContext
{
    public DVLDDbContext(DbContextOptions<DVLDDbContext> options): base(options)
    {
    }


    // =========================================================
    // PEOPLE
    // =========================================================

    public DbSet<Person> People { get; set; } = null!;


    // =========================================================
    // COUNTRIES
    // =========================================================

    public DbSet<Country> Countries { get; set; } = null!;


    // =========================================================
    // USERS
    // =========================================================

    public DbSet<User> Users { get; set; } = null!;


    // =========================================================
    // APPLICATION TYPES
    // =========================================================

    public DbSet<ApplicationType> ApplicationTypes
    { get; set; } = null!;


    // =========================================================
    // TEST TYPES
    // =========================================================

    public DbSet<TestType> TestTypes
    { get; set; } = null!;


    // =========================================================
    // LICENSE CLASSES
    // =========================================================

    public DbSet<LicenseClass>  LicenseClasses
    { get; set; } = null!;


    // =========================================================
    // APPLICATIONS
    // =========================================================

    public DbSet<ApplicationD>
        Applications
    { get; set; } = null!;


    // =========================================================
    // LOCAL DRIVING LICENSE APPLICATIONS
    // =========================================================

    public DbSet<LocalDrivingLicenseApplication>
        LocalDrivingLicenseApplications
    { get; set; } = null!;


    // =========================================================
    // TESTS
    // =========================================================

    public DbSet<Test>
        Tests
    { get; set; } = null!;


    // =========================================================
    // TEST APPOINTMENTS
    // =========================================================

    public DbSet<TestAppointment>
        TestAppointments
    { get; set; } = null!;


    // =========================================================
    // LICENSES
    // =========================================================

    public DbSet<License>
        Licenses
    { get; set; } = null!;


    // =========================================================
    // DRIVERS
    // =========================================================

    public DbSet<Driver>
        Drivers
    { get; set; } = null!;


    // =========================================================
    // DETAINED LICENSES
    // =========================================================

    public DbSet<DetainedLicense>
        DetainedLicenses
    { get; set; } = null!;


    // =========================================================
    // INTERNATIONAL LICENSES
    // =========================================================

    public DbSet<InternationalLicense>
        InternationalLicenses
    { get; set; } = null!;


    // =========================================================
    // MODEL CONFIGURATION
    // =========================================================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(DVLDDbContext).Assembly);
    }
}