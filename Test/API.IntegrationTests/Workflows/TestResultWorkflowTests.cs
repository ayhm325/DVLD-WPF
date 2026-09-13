using API.IntegrationTests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using DVLD.Contracts.Test;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Workflows;

public sealed class TestResultWorkflowTests
{
    [Fact]
    public async Task AddResult_WhenTestIsPassed_CreatesTestAndLocksAppointment()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedValidTestScenarioAsync(factory, TestTypeEnum.Theory);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var response = await AddResultAsync(
            client,
            seed.AppointmentId,
            true,
            "Passed theory test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var context = factory.CreateDbContext();

        var test = await context.Tests.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TestAppointmentID == seed.AppointmentId);

        var appointment = await context.TestAppointments.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TestAppointmentID == seed.AppointmentId);

        Assert.NotNull(test);
        Assert.True(test.TestResult);
        Assert.Equal("Passed theory test", test.Notes);

        Assert.NotNull(appointment);
        Assert.True(appointment.IsLocked);
    }

    [Fact]
    public async Task AddResult_WhenTestIsFailed_CreatesFailedTestAndLocksAppointment()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedValidTestScenarioAsync(factory, TestTypeEnum.Theory);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var response = await AddResultAsync(
            client,
            seed.AppointmentId,
            false,
            "Failed theory test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var context = factory.CreateDbContext();

        var test = await context.Tests.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TestAppointmentID == seed.AppointmentId);

        var appointment = await context.TestAppointments.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TestAppointmentID == seed.AppointmentId);

        Assert.NotNull(test);
        Assert.False(test.TestResult);
        Assert.Equal("Failed theory test", test.Notes);

        Assert.NotNull(appointment);
        Assert.True(appointment.IsLocked);
    }

    [Fact]
    public async Task AddResult_WhenAppointmentIsAlreadyLocked_ReturnsConflictAndDoesNotCreateTest()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();

        var seed = await SeedValidTestScenarioAsync(
            factory,
            TestTypeEnum.Theory,
            appointmentLocked: true);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var response = await AddResultAsync(
            client,
            seed.AppointmentId,
            true);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var context = factory.CreateDbContext();

        Assert.False(await context.Tests.AnyAsync(
            x => x.TestAppointmentID == seed.AppointmentId));
    }

    [Fact]
    public async Task AddResult_WhenAppointmentDoesNotExist_ReturnsNotFound()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedValidTestScenarioAsync(
            factory,
            TestTypeEnum.Theory);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var response = await AddResultAsync(
            client,
            seed.AppointmentId + 999999,
            true);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddResult_WhenAppointmentIsInTheFuture_ReturnsConflictAndDoesNotCreateTest()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();

        var seed = await SeedValidTestScenarioAsync(
            factory,
            TestTypeEnum.Theory,
            appointmentDate: DateTime.UtcNow.AddDays(1));

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var response = await AddResultAsync(
            client,
            seed.AppointmentId,
            true);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var context = factory.CreateDbContext();

        Assert.False(await context.Tests.AnyAsync(
            x => x.TestAppointmentID == seed.AppointmentId));

        var appointment = await context.TestAppointments.AsNoTracking()
            .SingleAsync(
                x => x.TestAppointmentID == seed.AppointmentId);

        Assert.False(appointment.IsLocked);
    }

    [Fact]
    public async Task AddResult_WhenWrittenTestIsTakenBeforeTheoryIsPassed_ReturnsConflictAndDoesNotCreateTest()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();

        var seed = await SeedValidTestScenarioAsync(
            factory,
            TestTypeEnum.Written);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var response = await AddResultAsync(
            client,
            seed.AppointmentId,
            true);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var context = factory.CreateDbContext();

        Assert.False(await context.Tests.AnyAsync(
            x => x.TestAppointmentID == seed.AppointmentId));
    }

    [Fact]
    public async Task AddResult_WhenApplicationIsNotNew_ReturnsConflictAndDoesNotCreateTest()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();

        var seed = await SeedValidTestScenarioAsync(
            factory,
            TestTypeEnum.Theory,
            applicationStatus: AppStatus.Completed);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var response = await AddResultAsync(
            client,
            seed.AppointmentId,
            true);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var context = factory.CreateDbContext();

        Assert.False(await context.Tests.AnyAsync(
            x => x.TestAppointmentID == seed.AppointmentId));
    }

    [Fact]
    public async Task AddResult_WhenResultAlreadyExists_ReturnsConflictAndKeepsSingleResult()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();

        var seed = await SeedValidTestScenarioAsync(
            factory,
            TestTypeEnum.Theory);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var first = await AddResultAsync(
            client,
            seed.AppointmentId,
            true);

        var second = await AddResultAsync(
            client,
            seed.AppointmentId,
            false);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        await using var context = factory.CreateDbContext();

        var tests = await context.Tests.AsNoTracking()
            .Where(x => x.TestAppointmentID == seed.AppointmentId)
            .ToListAsync();

        Assert.Single(tests);
        Assert.True(tests[0].TestResult);

        var appointment = await context.TestAppointments.AsNoTracking()
            .SingleAsync(
                x => x.TestAppointmentID == seed.AppointmentId);

        Assert.True(appointment.IsLocked);
    }

    [Fact]
    public async Task AddResult_WhenDatabaseSaveFails_RollsBackTestAndUnlocksAppointment()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();

        var seed = await SeedValidTestScenarioAsync(
            factory,
            TestTypeEnum.Theory);

        const string constraintName =
            "CK_Tests_IntegrationTest_BlockInsert";

        await using (var setup = factory.CreateDbContext())
        {
            await setup.Database.ExecuteSqlRawAsync(
                $"""
                ALTER TABLE Tests
                ADD CONSTRAINT {constraintName}
                CHECK (TestResult = 0)
                """);
        }

        try
        {
            using var client = factory.CreateClient();
            ConfigureAuthenticatedClient(client, seed.UserId);

            var response = await AddResultAsync(
                client,
                seed.AppointmentId,
                true);

            Assert.Equal(
                HttpStatusCode.InternalServerError,
                response.StatusCode);

            await using var context = factory.CreateDbContext();

            Assert.False(await context.Tests.AnyAsync(
                x => x.TestAppointmentID == seed.AppointmentId));

            var appointment = await context.TestAppointments.AsNoTracking()
                .SingleAsync(
                    x => x.TestAppointmentID == seed.AppointmentId);

            Assert.False(appointment.IsLocked);
        }
        finally
        {
            await using var cleanup = factory.CreateDbContext();

            try
            {
                await cleanup.Database.ExecuteSqlRawAsync(
                    $"ALTER TABLE Tests DROP CONSTRAINT {constraintName}");
            }
            catch
            {
                // Database cleanup may already have removed the constraint.
            }
        }
    }

    [Fact]
    public async Task AddResult_ConcurrentRequests_CreateOnlyOneTestResult()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();

        var seed = await SeedValidTestScenarioAsync(
            factory,
            TestTypeEnum.Theory,
            createSecondUser: true);

        using var clientA = factory.CreateClient();
        using var clientB = factory.CreateClient();

        ConfigureAuthenticatedClient(clientA, seed.UserId);
        ConfigureAuthenticatedClient(clientB, seed.SecondUserId);

        var gate = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> SendAsync(HttpClient client)
        {
            await gate.Task;

            return await AddResultAsync(
                client,
                seed.AppointmentId,
                true);
        }

        var taskA = SendAsync(clientA);
        var taskB = SendAsync(clientB);

        gate.SetResult();

        var responses = await Task.WhenAll(taskA, taskB);

        Console.WriteLine(
    $"Response A: {responses[0].StatusCode}");

        Console.WriteLine(
            $"Response B: {responses[1].StatusCode}");

        Assert.Equal(
            1,
            responses.Count(
                x => x.StatusCode == HttpStatusCode.OK));

        Assert.Equal(
            1,
            responses.Count(
                x => x.StatusCode == HttpStatusCode.Conflict));

        await using var context = factory.CreateDbContext();

        var tests = await context.Tests.AsNoTracking()
            .Where(x => x.TestAppointmentID == seed.AppointmentId)
            .ToListAsync();

        Assert.Single(tests);

        var appointment = await context.TestAppointments.AsNoTracking()
            .SingleAsync(
                x => x.TestAppointmentID == seed.AppointmentId);

        Assert.True(appointment.IsLocked);
    }

    private static async Task<HttpResponseMessage> AddResultAsync(
        HttpClient client,
        int appointmentId,
        bool result,
        string? notes = null)
    {
        return await client.PostAsJsonAsync(
            "/api/Tests",
            new SaveTestResultRequest(
                appointmentId,
                result,
                notes));
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

    private static async Task<SeedData> SeedValidTestScenarioAsync(
        SqlServerApiWebApplicationFactory factory,
        TestTypeEnum testType,
        bool appointmentLocked = false,
        DateTime? appointmentDate = null,
        AppStatus applicationStatus = AppStatus.New,
        bool createSecondUser = false)
    {
        await using var context = factory.CreateDbContext();

        var country = new Country
        {
            CountryName =
                $"Integration Country {Guid.NewGuid():N}"
        };

        context.Countries.Add(country);

        var userPerson = new Person
        {
            NationalNo = CreateNationalNumber(),
            FirstName = "Integration",
            SecondName = "Test",
            LastName = "User",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = Gender.Male,
            Address = "Integration Test Address",
            Phone = CreatePhone(),
            Email = $"user-{Guid.NewGuid():N}@test.local",
            Country = country
        };

        var applicantPerson = new Person
        {
            NationalNo = CreateNationalNumber(),
            FirstName = "Test",
            SecondName = "Applicant",
            LastName = "User",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = Gender.Male,
            Address = "Applicant Address",
            Phone = CreatePhone(),
            Email = $"applicant-{Guid.NewGuid():N}@test.local",
            Country = country
        };

        context.People.AddRange(
            userPerson,
            applicantPerson);

        Person? secondUserPerson = null;

        if (createSecondUser)
        {
            secondUserPerson = new Person
            {
                NationalNo = CreateNationalNumber(),
                FirstName = "Integration",
                SecondName = "Second",
                LastName = "User",
                DateOfBirth = new DateTime(1991, 1, 1),
                Gender = Gender.Male,
                Address = "Second Integration Test Address",
                Phone = CreatePhone(),
                Email = $"second-user-{Guid.NewGuid():N}@test.local",
                Country = country
            };

            context.People.Add(secondUserPerson);
        }

        await context.SaveChangesAsync();

        var user = new User
        {
            PersonId = userPerson.PersonId,
            UserName = $"integration-{Guid.NewGuid():N}",
            Password = "TestPassword",
            IsActive = true,
            Role = UserRole.Staff
        };

        context.Users.Add(user);

        User? secondUser = null;

        if (secondUserPerson is not null)
        {
            secondUser = new User
            {
                PersonId = secondUserPerson.PersonId,
                UserName = "second-user",
                Password = "TestPassword",
                IsActive = true,
                Role = UserRole.Staff
            };

            context.Users.Add(secondUser);
        }

        await context.SaveChangesAsync();

        await EnsureApplicationTypeAsync(
            context,
            1,
            "New Local Driving License",
            20m);

        await EnsureApplicationTypeAsync(
            context,
            7,
            "Retake Test",
            5m);

        await EnsureLicenseClassAsync(context);

        await EnsureTestTypeAsync(
            context,
            TestTypeEnum.Theory,
            "Theory Test",
            10m);

        await EnsureTestTypeAsync(
            context,
            TestTypeEnum.Written,
            "Written Test",
            15m);

        await EnsureTestTypeAsync(
            context,
            TestTypeEnum.Practical,
            "Practical Test",
            20m);

        var application = new ApplicationD
        {
            ApplicantPersonID = applicantPerson.PersonId,
            ApplicationDate = DateTime.UtcNow,
            ApplicationTypeID = 1,
            ApplicationStatus = applicationStatus,
            LastStatusDate = DateTime.UtcNow,
            PaidFees = 20m,
            CreatedByUserID = user.UserId
        };

        context.Applications.Add(application);
        await context.SaveChangesAsync();

        var localApplication = new LocalDrivingLicenseApplication
        {
            ApplicationID = application.ApplicationID,
            LicenseClassID = 1
        };

        context.LocalDrivingLicenseApplications.Add(
            localApplication);

        await context.SaveChangesAsync();

        var appointment = new TestAppointment
        {
            TestTypeID = (int)testType,
            LocalDrivingLicenseApplicationID =
                localApplication.LocalDrivingLicenseApplicationID,
            AppointmentDate =
                appointmentDate ??
                DateTime.UtcNow.AddMinutes(-5),
            PaidFees =
                testType == TestTypeEnum.Theory ? 10m :
                testType == TestTypeEnum.Written ? 15m :
                20m,
            CreatedByUserID = user.UserId,
            IsLocked = appointmentLocked
        };

        context.TestAppointments.Add(appointment);

        await context.SaveChangesAsync();

        return new SeedData(
            user.UserId,
            secondUser?.UserId ?? 0,
            applicantPerson.PersonId,
            localApplication.LocalDrivingLicenseApplicationID,
            appointment.TestAppointmentID);
    }

    private static async Task EnsureApplicationTypeAsync(
        DVLDDbContext context,
        int id,
        string title,
        decimal fees)
    {
        if (!await context.ApplicationTypes.AnyAsync(
                x => x.ApplicationTypeId == id))
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                SET IDENTITY_INSERT ApplicationTypes ON;

                INSERT INTO ApplicationTypes
                    (ApplicationTypeId,
                     ApplicationTypeTitle,
                     ApplicationFees)
                VALUES
                    ({id},
                     {title},
                     {fees});

                SET IDENTITY_INSERT ApplicationTypes OFF;
                """);
        }
    }

    private static async Task EnsureLicenseClassAsync(
        DVLDDbContext context)
    {
        if (await context.LicenseClasses.AnyAsync(
                x => x.LicenseClassID == 1))
        {
            return;
        }

        await context.Database.ExecuteSqlRawAsync(
            """
            SET IDENTITY_INSERT LicenseClasses ON;

            INSERT INTO LicenseClasses
                (LicenseClassID,
                 ClassName,
                 ClassDescription,
                 MinimumAllowedAge,
                 DefaultValidityLength,
                 ClassFees)
            VALUES
                (1,
                 N'Integration Class',
                 N'Integration test license class',
                 18,
                 5,
                 100);

            SET IDENTITY_INSERT LicenseClasses OFF;
            """);
    }

    private static async Task EnsureTestTypeAsync(
        DVLDDbContext context,
        TestTypeEnum type,
        string title,
        decimal fees)
    {
        var id = (int)type;

        if (await context.TestTypes.AnyAsync(
                x => x.TestTypeId == id))
        {
            return;
        }

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            SET IDENTITY_INSERT TestTypes ON;

            INSERT INTO TestTypes
                (TestTypeId,
                 TestTypeTitle,
                 TestTypeDescription,
                 TestTypeFees)
            VALUES
                ({id},
                 {title},
                 {"Integration test type"},
                 {fees});

            SET IDENTITY_INSERT TestTypes OFF;
            """);
    }

    private static string CreateNationalNumber() =>
        Guid.NewGuid().ToString("N")[..18];

    private static string CreatePhone() =>
        $"07{Random.Shared.NextInt64(100000000, 999999999)}";

    private sealed record SeedData(
        int UserId,
        int SecondUserId,
        int PersonId,
        int LocalApplicationId,
        int AppointmentId);
}
