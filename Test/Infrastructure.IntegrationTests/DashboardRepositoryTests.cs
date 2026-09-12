using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests;

public sealed class DashboardRepositoryTests
    : IClassFixture<DashboardRepositoryDatabaseFixture>
{
    private readonly DashboardRepositoryDatabaseFixture _fixture;

    public DashboardRepositoryTests(
        DashboardRepositoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    [Fact]
    public void Constructor_ShouldThrowWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DashboardRepository(null!));
    }

    // =========================================================
    // GET STATISTICS
    // =========================================================

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnAllExpectedStatistics()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DashboardRepository(context);

        var result =
            await repository.GetStatisticsAsync();

        Assert.NotNull(result);

        Assert.Equal(
            3,
            result.TotalPeople);

        Assert.Equal(
            2,
            result.TotalDrivers);

        Assert.Equal(
            1,
            result.ActiveLicenses);

        Assert.Equal(
            2,
            result.PendingApplications);

        Assert.Equal(
            2,
            result.LocalDrivingLicenseApplications);

        Assert.Equal(
            1,
            result.InternationalLicenses);

        Assert.Equal(
            1,
            result.DetainedLicenses);

        Assert.Equal(
            1,
            result.UpcomingTests);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldCountOnlyActiveLicenses()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DashboardRepository(context);

        var result =
            await repository.GetStatisticsAsync();

        Assert.Equal(
            1,
            result.ActiveLicenses);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldCountOnlyPendingApplications()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DashboardRepository(context);

        var result =
            await repository.GetStatisticsAsync();

        Assert.Equal(
            2,
            result.PendingApplications);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldCountLocalDrivingLicenseApplications()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DashboardRepository(context);

        var result =
            await repository.GetStatisticsAsync();

        Assert.Equal(
            2,
            result.LocalDrivingLicenseApplications);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldCountInternationalLicenses()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DashboardRepository(context);

        var result =
            await repository.GetStatisticsAsync();

        Assert.Equal(
            1,
            result.InternationalLicenses);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldCountOnlyActiveDetainedLicenses()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DashboardRepository(context);

        var result =
            await repository.GetStatisticsAsync();

        Assert.Equal(
            1,
            result.DetainedLicenses);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldCountOnlyTodayAndFutureAppointments()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DashboardRepository(context);

        var result =
            await repository.GetStatisticsAsync();

        Assert.Equal(
            1,
            result.UpcomingTests);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnZeroForEmptyDatabase()
    {
        await using var context =
            _fixture.Database.CreateContext();

        await ClearDashboardDataAsync(context);

        var repository =
            new DashboardRepository(context);

        var result =
            await repository.GetStatisticsAsync();

        Assert.NotNull(result);

        Assert.Equal(0, result.TotalPeople);
        Assert.Equal(0, result.TotalDrivers);
        Assert.Equal(0, result.ActiveLicenses);
        Assert.Equal(0, result.PendingApplications);
        Assert.Equal(0, result.LocalDrivingLicenseApplications);
        Assert.Equal(0, result.InternationalLicenses);
        Assert.Equal(0, result.DetainedLicenses);
        Assert.Equal(0, result.UpcomingTests);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static async Task ClearDashboardDataAsync(
        Infrastructure.DVLDDbContext context)
    {
        context.TestAppointments.RemoveRange(
            context.TestAppointments);

        context.DetainedLicenses.RemoveRange(
            context.DetainedLicenses);

        context.InternationalLicenses.RemoveRange(
            context.InternationalLicenses);

        context.Licenses.RemoveRange(
            context.Licenses);

        context.LocalDrivingLicenseApplications.RemoveRange(
            context.LocalDrivingLicenseApplications);

        context.Drivers.RemoveRange(
            context.Drivers);

        context.Applications.RemoveRange(
            context.Applications);

        context.Users.RemoveRange(
            context.Users);

        context.People.RemoveRange(
            context.People);

        context.TestTypes.RemoveRange(
            context.TestTypes);

        context.ApplicationTypes.RemoveRange(
            context.ApplicationTypes);

        context.LicenseClasses.RemoveRange(
            context.LicenseClasses);

        context.Countries.RemoveRange(
            context.Countries);

        await context.SaveChangesAsync();
    }
}

// =============================================================
// DATABASE FIXTURE
// =============================================================

public sealed class DashboardRepositoryDatabaseFixture
    : IAsyncLifetime
{
    private SqlServerTestDatabase? _database;

    public SqlServerTestDatabase Database { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _database =
            new SqlServerTestDatabase();

        await _database.InitializeAsync();

        Database =
            _database;

        await SeedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    private async Task SeedAsync()
    {
        await using var context =
            Database.CreateContext();

        // -----------------------------------------------------
        // COUNTRY
        // -----------------------------------------------------

        var country =
            new Country
            {
                CountryName =
                    "Jordan"
            };

        context.Countries.Add(country);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // PEOPLE
        // -----------------------------------------------------

        var firstPerson =
            new Person
            {
                NationalNo =
                    "DASH-001",

                FirstName =
                    "Dashboard",

                SecondName =
                    "First",

                LastName =
                    "Person",

                DateOfBirth =
                    new DateTime(1990, 1, 1),

                Gender =
                    Gender.Male,

                Address =
                    "Dashboard Test Address 1",

                Phone =
                    "0790000001",

                Email =
                    "dashboard1@test.local",

                NationalityCountryID =
                    country.CountryId
            };

        var secondPerson =
            new Person
            {
                NationalNo =
                    "DASH-002",

                FirstName =
                    "Dashboard",

                SecondName =
                    "Second",

                LastName =
                    "Person",

                DateOfBirth =
                    new DateTime(1991, 1, 1),

                Gender =
                    Gender.Male,

                Address =
                    "Dashboard Test Address 2",

                Phone =
                    "0790000002",

                Email =
                    "dashboard2@test.local",

                NationalityCountryID =
                    country.CountryId
            };

        var thirdPerson =
            new Person
            {
                NationalNo =
                    "DASH-003",

                FirstName =
                    "Dashboard",

                SecondName =
                    "Third",

                LastName =
                    "Person",

                DateOfBirth =
                    new DateTime(1992, 1, 1),

                Gender =
                    Gender.Female,

                Address =
                    "Dashboard Test Address 3",

                Phone =
                    "0790000003",

                Email =
                    "dashboard3@test.local",

                NationalityCountryID =
                    country.CountryId
            };

        context.People.AddRange(
            firstPerson,
            secondPerson,
            thirdPerson);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // USER
        // -----------------------------------------------------

        var user =
            new User
            {
                PersonId =
                    firstPerson.PersonId,

                UserName =
                    "dashboard.integration",

                Password =
                    "integration-test-password",

                IsActive =
                    true,

                Role =
                    UserRole.Admin
            };

        context.Users.Add(user);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // APPLICATION TYPES
        // -----------------------------------------------------

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    "Dashboard Integration Application",

                ApplicationFees =
                    20m
            };

        context.ApplicationTypes.Add(
            applicationType);

        // -----------------------------------------------------
        // LICENSE CLASS
        // -----------------------------------------------------

        var licenseClass =
            new LicenseClass
            {
                ClassName =
                    "Dashboard Integration Class",

                ClassDescription =
                    "Dashboard repository integration test class",

                MinimumAllowedAge =
                    18,

                DefaultValidityLength =
                    10,

                ClassFees =
                    100m
            };

        context.LicenseClasses.Add(
            licenseClass);

        // -----------------------------------------------------
        // TEST TYPE
        // -----------------------------------------------------

        var testType =
            new TestType
            {
                TestTypeTitle =
                    "Dashboard Integration Test",

                TestTypeDescription =
                    "Dashboard repository integration test",

                TestTypeFees =
                    10m
            };

        context.TestTypes.Add(
            testType);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // APPLICATIONS
        // -----------------------------------------------------

        var pendingApplicationOne =
            new ApplicationD
            {
                ApplicantPersonID =
                    firstPerson.PersonId,

                ApplicationDate =
                    DateTime.Today.AddDays(-2),

                ApplicationTypeID =
                    applicationType.ApplicationTypeId,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    DateTime.Today.AddDays(-2),

                PaidFees =
                    20m,

                CreatedByUserID =
                    user.UserId
            };

        var pendingApplicationTwo =
            new ApplicationD
            {
                ApplicantPersonID =
                    secondPerson.PersonId,

                ApplicationDate =
                    DateTime.Today.AddDays(-1),

                ApplicationTypeID =
                    applicationType.ApplicationTypeId,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    DateTime.Today.AddDays(-1),

                PaidFees =
                    20m,

                CreatedByUserID =
                    user.UserId
            };

        var completedApplication =
            new ApplicationD
            {
                ApplicantPersonID =
                    thirdPerson.PersonId,

                ApplicationDate =
                    DateTime.Today.AddDays(-3),

                ApplicationTypeID =
                    applicationType.ApplicationTypeId,

                ApplicationStatus =
                    AppStatus.Completed,

                LastStatusDate =
                    DateTime.Today.AddDays(-3),

                PaidFees =
                    20m,

                CreatedByUserID =
                    user.UserId
            };

        context.Applications.AddRange(
            pendingApplicationOne,
            pendingApplicationTwo,
            completedApplication);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // LOCAL DRIVING LICENSE APPLICATIONS
        // -----------------------------------------------------

        var localApplicationOne =
            new LocalDrivingLicenseApplication
            {
                ApplicationID =
                    pendingApplicationOne.ApplicationID,

                LicenseClassID =
                    licenseClass.LicenseClassID
            };

        var localApplicationTwo =
            new LocalDrivingLicenseApplication
            {
                ApplicationID =
                    completedApplication.ApplicationID,

                LicenseClassID =
                    licenseClass.LicenseClassID
            };

        context.LocalDrivingLicenseApplications.AddRange(
            localApplicationOne,
            localApplicationTwo);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // DRIVERS
        // -----------------------------------------------------

        var firstDriver =
            new Driver
            {
                PersonID =
                    firstPerson.PersonId,

                CreatedByUserID =
                    user.UserId,

                CreatedDate =
                    DateTime.UtcNow
            };

        var secondDriver =
            new Driver
            {
                PersonID =
                    secondPerson.PersonId,

                CreatedByUserID =
                    user.UserId,

                CreatedDate =
                    DateTime.UtcNow
            };

        context.Drivers.AddRange(
            firstDriver,
            secondDriver);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // LICENSES
        // -----------------------------------------------------

        var activeLicense =
            new License
            {
                ApplicationID =
                    pendingApplicationOne.ApplicationID,

                DriverID =
                    firstDriver.DriverID,

                LicenseClass =
                    licenseClass.LicenseClassID,

                IssueDate =
                    DateTime.Today.AddDays(-10),

                ExpirationDate =
                    DateTime.Today.AddYears(1),

                Notes =
                    "Active dashboard integration license",

                PaidFees =
                    100m,

                IsActive =
                    true,

                IssueReason =
                    IssueReason.FirstTime,

                CreatedByUserID =
                    user.UserId
            };

        var inactiveLicense =
            new License
            {
                ApplicationID =
                    completedApplication.ApplicationID,

                DriverID =
                    secondDriver.DriverID,

                LicenseClass =
                    licenseClass.LicenseClassID,

                IssueDate =
                    DateTime.Today.AddYears(-2),

                ExpirationDate =
                    DateTime.Today.AddYears(-1),

                Notes =
                    "Inactive dashboard integration license",

                PaidFees =
                    100m,

                IsActive =
                    false,

                IssueReason =
                    IssueReason.FirstTime,

                CreatedByUserID =
                    user.UserId
            };

        context.Licenses.AddRange(
            activeLicense,
            inactiveLicense);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // INTERNATIONAL LICENSE
        // -----------------------------------------------------

        var internationalLicense =
            new InternationalLicense
            {
                ApplicationID =
                    completedApplication.ApplicationID,

                DriverID =
                    firstDriver.DriverID,

                IssuedUsingLocalLicenseID =
                    activeLicense.LicenseID,

                IssueDate =
                    DateTime.Today.AddDays(-5),

                ExpirationDate =
                    DateTime.Today.AddYears(1),

                IsActive =
                    true,

                CreatedByUserID =
                    user.UserId
            };

        context.InternationalLicenses.Add(
            internationalLicense);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // DETAINED LICENSES
        // -----------------------------------------------------

        var activeDetainedLicense =
            new DetainedLicense
            {
                LicenseID =
                    activeLicense.LicenseID,

                DetainDate =
                    DateTime.Today.AddDays(-2),

                FineFees =
                    50m,

                CreatedByUserID =
                    user.UserId,

                IsReleased =
                    false
            };

        var releasedDetainedLicense =
            new DetainedLicense
            {
                LicenseID =
                    inactiveLicense.LicenseID,

                DetainDate =
                    DateTime.Today.AddDays(-10),

                FineFees =
                    50m,

                CreatedByUserID =
                    user.UserId,

                IsReleased =
                    true,

                ReleaseDate =
                    DateTime.Today.AddDays(-5),

                ReleasedByUserID =
                    user.UserId,

                ReleaseApplicationID =
                    completedApplication.ApplicationID
            };

        context.DetainedLicenses.AddRange(
            activeDetainedLicense,
            releasedDetainedLicense);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // TEST APPOINTMENTS
        // -----------------------------------------------------

        var upcomingAppointment =
            new TestAppointment
            {
                TestTypeID =
                    testType.TestTypeId,

                LocalDrivingLicenseApplicationID =
                    localApplicationOne.LocalDrivingLicenseApplicationID,

                AppointmentDate =
                    DateTime.Today.AddDays(1)
                    .AddHours(10),

                PaidFees =
                    10m,

                CreatedByUserID =
                    user.UserId,

                IsLocked =
                    false
            };

        var pastAppointment =
            new TestAppointment
            {
                TestTypeID =
                    testType.TestTypeId,

                LocalDrivingLicenseApplicationID =
                    localApplicationTwo.LocalDrivingLicenseApplicationID,

                AppointmentDate =
                    DateTime.Today.AddDays(-1)
                    .AddHours(10),

                PaidFees =
                    10m,

                CreatedByUserID =
                    user.UserId,

                IsLocked =
                    false
            };

        context.TestAppointments.AddRange(
            upcomingAppointment,
            pastAppointment);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // SEED ASSERTIONS
        // -----------------------------------------------------

        Assert.Equal(
            3,
            await context.People.CountAsync());

        Assert.Equal(
            2,
            await context.Drivers.CountAsync());

        Assert.Equal(
            3,
            await context.Applications.CountAsync());

        Assert.Equal(
            2,
            await context.LocalDrivingLicenseApplications.CountAsync());

        Assert.Equal(
            2,
            await context.Licenses.CountAsync());

        Assert.Equal(
            1,
            await context.Licenses
                .CountAsync(x => x.IsActive));

        Assert.Equal(
            1,
            await context.InternationalLicenses.CountAsync());

        Assert.Equal(
            1,
            await context.DetainedLicenses
                .CountAsync(x => !x.IsReleased));

        Assert.Equal(
            2,
            await context.TestAppointments.CountAsync());

        Assert.Equal(
            1,
            await context.TestAppointments
                .CountAsync(
                    x =>
                        x.AppointmentDate >=
                        DateTime.Today));
    }
}