using API.IntegrationTests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using DVLD.Contracts.LicenseIssuance;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Net;
using System.Net.Http.Json;
using static System.Net.Mime.MediaTypeNames;

namespace API.IntegrationTests.Workflows;

public sealed class LicenseIssuanceWorkflowTests
{
    [Fact]
    public async Task IssueFirstLicense_WhenWorkflowIsValid_CreatesLicenseAndCompletesApplication()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidLicenseIssuanceScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var request =
            new IssueFirstLicenseRequest(
                LocalApplicationId:
                    seed.LocalApplicationId,
                Notes:
                    "Integration test license");

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseIssuance/first-license",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<IssueFirstLicenseResponse>();

        Assert.NotNull(result);

        Assert.True(
            result.LicenseId > 0);

        await using var verificationContext =
            factory.CreateDbContext();

        var license =
            await verificationContext
                .Licenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.LicenseID ==
                        result.LicenseId);

        Assert.NotNull(license);

        Assert.Equal(
            seed.ApplicationId,
            license.ApplicationID);

        var driverPersonId =
            await verificationContext
                .Drivers
                .AsNoTracking()
                .Where(
                    x =>
                        x.DriverID ==
                        license.DriverID)
                .Select(
                    x =>
                        x.PersonID)
                .SingleAsync();

        Assert.Equal(
            seed.PersonId,
            driverPersonId);

        Assert.Equal(
            seed.LicenseClassId,
            license.LicenseClass);

        Assert.Equal(
            IssueReason.FirstTime,
            license.IssueReason);

        Assert.True(
            license.IsActive);

        Assert.Equal(
            100m,
            license.PaidFees);

        Assert.Equal(
            "Integration test license",
            license.Notes);

        var application =
            await verificationContext
                .Applications
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.ApplicationID ==
                        seed.ApplicationId);

        Assert.Equal(
            AppStatus.Completed,
            application.ApplicationStatus);

        Assert.Equal(
            seed.UserId,
            license.CreatedByUserID);
    }


    [Fact]
    public async Task IssueFirstLicense_WhenLicenseCreationFails_RollsBackDriverAndApplicationChanges()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidLicenseIssuanceScenarioAsync(factory);

        await CreateLicenseInsertFailureConstraintAsync(factory);

        try
        {
            using var client =
                factory.CreateClient();

            ConfigureAuthenticatedClient(
                client,
                seed.UserId);

            var request =
                new IssueFirstLicenseRequest(
                    LocalApplicationId:
                        seed.LocalApplicationId,
                    Notes:
                        "FORCE_FAILURE");

            var response =
                await client.PostAsJsonAsync(
                    "/api/LicenseIssuance/first-license",
                    request);

            Assert.Equal(
                HttpStatusCode.InternalServerError,
                response.StatusCode);

            await using var verificationContext =
                factory.CreateDbContext();

            var driverExists =
                await verificationContext
                    .Drivers
                    .AsNoTracking()
                    .AnyAsync(
                        x =>
                            x.PersonID ==
                            seed.PersonId);

            Assert.False(
                driverExists);

            var licenseExists =
                await verificationContext
                    .Licenses
                    .AsNoTracking()
                    .AnyAsync(
                        x =>
                            x.ApplicationID ==
                            seed.ApplicationId);

            Assert.False(
                licenseExists);

            var application =
                await verificationContext
                    .Applications
                    .AsNoTracking()
                    .SingleAsync(
                        x =>
                            x.ApplicationID ==
                            seed.ApplicationId);

            Assert.Equal(
                AppStatus.New,
                application.ApplicationStatus);
        }
        finally
        {
            await RemoveLicenseInsertFailureConstraintAsync(factory);
        }
    }


    [Fact]
    public async Task IssueFirstLicense_WhenTwoRequestsRunConcurrently_AllowsOnlyOneLicense()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidLicenseIssuanceScenarioAsync(factory);

        using var client1 =
            factory.CreateClient();

        using var client2 =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client1,
            seed.UserId);

        ConfigureAuthenticatedClient(
            client2,
            seed.UserId);

        var request =
            new IssueFirstLicenseRequest(
                LocalApplicationId:
                    seed.LocalApplicationId,
                Notes:
                    "Concurrency integration test");

        var startGate =
            new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> SendAsync(
            HttpClient client)
        {
            await startGate.Task;

            return await client.PostAsJsonAsync(
                "/api/LicenseIssuance/first-license",
                request);
        }

        var request1 =
            SendAsync(client1);

        var request2 =
            SendAsync(client2);

        startGate.SetResult();

        var responses =
            await Task.WhenAll(
                request1,
                request2);

        var statusCodes =
            responses
                .Select(x => x.StatusCode)
                .OrderBy(x => x)
                .ToArray();

        Assert.Contains(
            HttpStatusCode.OK,
            statusCodes);

        Assert.Contains(
            HttpStatusCode.Conflict,
            statusCodes);

        Assert.Equal(
            1,
            statusCodes.Count(
                x =>
                    x == HttpStatusCode.OK));

        Assert.Equal(
            1,
            statusCodes.Count(
                x =>
                    x == HttpStatusCode.Conflict));

        await using var verificationContext =
            factory.CreateDbContext();

        var licenses =
            await verificationContext
                .Licenses
                .AsNoTracking()
                .Where(
                    x =>
                        x.ApplicationID ==
                        seed.ApplicationId)
                .ToListAsync();

        Assert.Single(
            licenses);

        var drivers =
            await verificationContext
                .Drivers
                .AsNoTracking()
                .Where(
                    x =>
                        x.PersonID ==
                        seed.PersonId)
                .ToListAsync();

        Assert.Single(
            drivers);

        var application =
            await verificationContext
                .Applications
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.ApplicationID ==
                        seed.ApplicationId);

        Assert.Equal(
            AppStatus.Completed,
            application.ApplicationStatus);
    }


    private static void ConfigureAuthenticatedClient(
        HttpClient client,
        int userId)
    {
        client.DefaultRequestHeaders.Add(
            "X-Test-User-Id",
            userId.ToString());

        client.DefaultRequestHeaders.Add(
            "X-Test-Username",
            "integration.test");

        client.DefaultRequestHeaders.Add(
            "X-Test-FullName",
            "Integration Test User");

        client.DefaultRequestHeaders.Add(
            "X-Test-Role",
            "Staff");
    }


    private static async Task CreateLicenseInsertFailureConstraintAsync(
        SqlServerApiWebApplicationFactory factory)
    {
        await using var context =
            factory.CreateDbContext();

        await context.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE [Licenses]
            ADD CONSTRAINT [CK_Test_Licenses_ForceFailure]
            CHECK ([Notes] <> 'FORCE_FAILURE');
            """);
    }


    private static async Task RemoveLicenseInsertFailureConstraintAsync(
        SqlServerApiWebApplicationFactory factory)
    {
        await using var context =
            factory.CreateDbContext();

        await context.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE [Licenses]
            DROP CONSTRAINT [CK_Test_Licenses_ForceFailure];
            """);
    }


    private static async Task<SeedData>
        SeedValidLicenseIssuanceScenarioAsync(
            SqlServerApiWebApplicationFactory factory)
    {
        await using var context =
            factory.CreateDbContext();

        /*
         * ---------------------------------------------------------
         * Lookup data
         * ---------------------------------------------------------
         *
         * These tables use SQL Server IDENTITY columns.
         *
         * The production workflow depends on the conventional
         * lookup IDs:
         *
         * ApplicationType = 1
         * LicenseClass    = 1
         * Theory          = 1
         * Written         = 2
         * Practical       = 3
         *
         * Therefore the integration seed intentionally uses
         * IDENTITY_INSERT.
         */

        await context.Database.OpenConnectionAsync();

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT ApplicationTypes ON");

            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO ApplicationTypes
                (
                    ApplicationTypeId,
                    ApplicationTypeTitle,
                    ApplicationFees
                )
                VALUES
                (
                    1,
                    N'New Local Driving License',
                    20
                )
                """);

            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT ApplicationTypes OFF");


            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT LicenseClasses ON");

            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO LicenseClasses
                (
                    LicenseClassID,
                    ClassName,
                    ClassDescription,
                    MinimumAllowedAge,
                    DefaultValidityLength,
                    ClassFees
                )
                VALUES
                (
                    1,
                    N'Integration Class',
                    N'Integration test license class',
                    18,
                    5,
                    100
                )
                """);

            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT LicenseClasses OFF");


            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT TestTypes ON");

            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO TestTypes
                (
                    TestTypeId,
                    TestTypeTitle,
                    TestTypeDescription,
                    TestTypeFees
                )
                VALUES
                (
                    1,
                    N'Theory',
                    N'Theory test',
                    10
                ),
                (
                    2,
                    N'Written',
                    N'Written test',
                    10
                ),
                (
                    3,
                    N'Practical',
                    N'Practical test',
                    20
                )
                """);

            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT TestTypes OFF");
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }

        /*
         * ---------------------------------------------------------
         * Country
         * ---------------------------------------------------------
         */

        var country =
            new Country
            {
                CountryName =
                    $"Integration Country {Guid.NewGuid():N}"
            };

        context.Countries.Add(country);

        await context.SaveChangesAsync();

        /*
         * ---------------------------------------------------------
         * People
         * ---------------------------------------------------------
         */

        var userPerson =
            new Person
            {
                NationalNo =
                    CreateNationalNumber(),

                FirstName =
                    "Integration",

                SecondName =
                    "Test",

                ThirdName =
                    null,

                LastName =
                    "User",

                DateOfBirth =
                    new DateTime(
                        1990,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Integration Test Address",

                Phone =
                    CreatePhone(),

                Email =
                    $"user-{Guid.NewGuid():N}@test.local",

                NationalityCountryID =
                    country.CountryId
            };

        var applicantPerson =
            new Person
            {
                NationalNo =
                    CreateNationalNumber(),

                FirstName =
                    "Applicant",

                SecondName =
                    "Integration",

                ThirdName =
                    null,

                LastName =
                    "Person",

                DateOfBirth =
                    new DateTime(
                        1990,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Applicant Test Address",

                Phone =
                    CreatePhone(),

                Email =
                    $"applicant-{Guid.NewGuid():N}@test.local",

                NationalityCountryID =
                    country.CountryId
            };

        context.People.AddRange(
            userPerson,
            applicantPerson);

        await context.SaveChangesAsync();

        /*
         * ---------------------------------------------------------
         * User
         * ---------------------------------------------------------
         */

        var user =
            new User
            {
                PersonId =
                    userPerson.PersonId,

                UserName =
                    $"integration-{Guid.NewGuid():N}",

                Password =
                    "TestPassword",

                IsActive =
                    true,

                Role =
                    UserRole.Staff
            };

        context.Users.Add(user);

        await context.SaveChangesAsync();

        /*
         * ---------------------------------------------------------
         * Application
         * ---------------------------------------------------------
         */

        var application =
            new ApplicationD
            {
                ApplicantPersonID =
                    applicantPerson.PersonId,

                ApplicationDate =
                    DateTime.UtcNow,

                ApplicationTypeID =
                    1,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    DateTime.UtcNow,

                PaidFees =
                    20m,

                CreatedByUserID =
                    user.UserId
            };

        context.Applications.Add(
            application);

        await context.SaveChangesAsync();

        /*
         * ---------------------------------------------------------
         * Local Driving License Application
         * ---------------------------------------------------------
         */

        var localApplication =
            new LocalDrivingLicenseApplication
            {
                ApplicationID =
                    application.ApplicationID,

                LicenseClassID =
                    1
            };

        context.LocalDrivingLicenseApplications.Add(
            localApplication);

        await context.SaveChangesAsync();

        /*
         * ---------------------------------------------------------
         * Test Appointments
         * ---------------------------------------------------------
         */

        var appointmentDate =
            DateTime.UtcNow.AddDays(-3);

        var theoryAppointment =
            new TestAppointment
            {
                TestTypeID =
                    (int)TestTypeEnum.Theory,

                LocalDrivingLicenseApplicationID =
                    localApplication.LocalDrivingLicenseApplicationID,

                AppointmentDate =
                    appointmentDate,

                PaidFees =
                    10m,

                CreatedByUserID =
                    user.UserId,

                IsLocked =
                    true
            };

        var writtenAppointment =
            new TestAppointment
            {
                TestTypeID =
                    (int)TestTypeEnum.Written,

                LocalDrivingLicenseApplicationID =
                    localApplication.LocalDrivingLicenseApplicationID,

                AppointmentDate =
                    appointmentDate,

                PaidFees =
                    10m,

                CreatedByUserID =
                    user.UserId,

                IsLocked =
                    true
            };

        var practicalAppointment =
            new TestAppointment
            {
                TestTypeID =
                    (int)TestTypeEnum.Practical,

                LocalDrivingLicenseApplicationID =
                    localApplication.LocalDrivingLicenseApplicationID,

                AppointmentDate =
                    appointmentDate,

                PaidFees =
                    20m,

                CreatedByUserID =
                    user.UserId,

                IsLocked =
                    true
            };

        context.TestAppointments.AddRange(
            theoryAppointment,
            writtenAppointment,
            practicalAppointment);

        await context.SaveChangesAsync();

        /*
         * ---------------------------------------------------------
         * Passed Tests
         * ---------------------------------------------------------
         */

        var tests =
            new[]
            {
                new Test
                {
                    TestAppointmentID =
                        theoryAppointment.TestAppointmentID,

                    TestResult =
                        true,

                    Notes =
                        "Passed theory test",

                    CreatedByUserID =
                        user.UserId
                },

                new Test
                {
                    TestAppointmentID =
                        writtenAppointment.TestAppointmentID,

                    TestResult =
                        true,

                    Notes =
                        "Passed written test",

                    CreatedByUserID =
                        user.UserId
                },

                new Test
                {
                    TestAppointmentID =
                        practicalAppointment.TestAppointmentID,

                    TestResult =
                        true,

                    Notes =
                        "Passed practical test",

                    CreatedByUserID =
                        user.UserId
                }
            };

        context.Tests.AddRange(
            tests);

        await context.SaveChangesAsync();

        /*
         * ---------------------------------------------------------
         * Return seed identifiers
         * ---------------------------------------------------------
         */

        return new SeedData(
            UserId:
                user.UserId,

            PersonId:
                applicantPerson.PersonId,

            ApplicationId:
                application.ApplicationID,

            LocalApplicationId:
                localApplication.LocalDrivingLicenseApplicationID,

            LicenseClassId:
                1);
    }


    private static string CreateNationalNumber()
    {
        return
            Guid.NewGuid()
                .ToString("N")[..18];
    }


    private static string CreatePhone()
    {
        return
            $"07{Random.Shared.NextInt64(
                100000000,
                999999999)}";
    }


    private sealed record SeedData(
        int UserId,
        int PersonId,
        int ApplicationId,
        int LocalApplicationId,
        int LicenseClassId);
}
