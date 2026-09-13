using API.IntegrationTests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using DVLD.Contracts.TestAppointment;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Workflows;

public sealed class TestAppointmentWorkflowTests
{

    [Fact]
    public async Task ScheduleTheory_WhenWorkflowIsValid_CreatesAppointment()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidSchedulingScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var appointmentDate =
            DateTime.UtcNow.AddDays(1);

        using var response =
            await ScheduleAsync(
                client,
                seed.LocalApplicationId,
                TestTypeEnum.Theory,
                appointmentDate);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var context =
            factory.CreateDbContext();

        var appointment =
            await GetAppointmentAsync(
                context,
                seed.LocalApplicationId,
                TestTypeEnum.Theory);

        Assert.NotNull(appointment);

        Assert.Equal(
            seed.LocalApplicationId,
            appointment.LocalDrivingLicenseApplicationID);

        Assert.Equal(
            (int)TestTypeEnum.Theory,
            appointment.TestTypeID);

        Assert.Equal(
            appointmentDate,
            appointment.AppointmentDate);

        Assert.Equal(
            seed.UserId,
            appointment.CreatedByUserID);

        Assert.Equal(
            10m,
            appointment.PaidFees);

        Assert.False(
            appointment.IsLocked);

        Assert.Null(
            appointment.RetakeTestApplicationID);

        var trialCount =
            await GetTrialCountAsync(
                context,
                seed.LocalApplicationId,
                TestTypeEnum.Theory);

        Assert.Equal(
            1,
            trialCount);
    }


    [Fact]
    public async Task ScheduleTheory_WhenTheoryAppointmentAlreadyExists_ReturnsConflictAndDoesNotCreateDuplicate()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidSchedulingScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var firstDate =
            DateTime.UtcNow.AddDays(1);

        var secondDate =
            DateTime.UtcNow.AddDays(2);

        using var firstResponse =
            await ScheduleAsync(
                client,
                seed.LocalApplicationId,
                TestTypeEnum.Theory,
                firstDate);

        Assert.Equal(
            HttpStatusCode.NoContent,
            firstResponse.StatusCode);

        using var secondResponse =
            await ScheduleAsync(
                client,
                seed.LocalApplicationId,
                TestTypeEnum.Theory,
                secondDate);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);

        await using var context =
            factory.CreateDbContext();

        var appointments =
            await context.TestAppointments
                .AsNoTracking()
                .Where(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                            seed.LocalApplicationId
                        &&
                        x.TestTypeID ==
                            (int)TestTypeEnum.Theory)
                .ToListAsync();

        Assert.Single(
            appointments);

        Assert.Equal(
            firstDate,
            appointments[0].AppointmentDate);
    }


    [Fact]
    public async Task ScheduleWritten_BeforeTheoryIsPassed_ReturnsConflictAndDoesNotCreateAppointment()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidSchedulingScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        using var response =
            await ScheduleAsync(
                client,
                seed.LocalApplicationId,
                TestTypeEnum.Written,
                DateTime.UtcNow.AddDays(1));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await using var context =
            factory.CreateDbContext();

        var appointments =
            await context.TestAppointments
                .AsNoTracking()
                .Where(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                            seed.LocalApplicationId)
                .ToListAsync();

        Assert.Empty(
            appointments);
    }


    [Fact]
    public async Task ScheduleWritten_AfterTheoryIsPassed_CreatesAppointment()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidSchedulingScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var theoryDate =
            DateTime.UtcNow.AddDays(1);

        using var theoryResponse =
            await ScheduleAsync(
                client,
                seed.LocalApplicationId,
                TestTypeEnum.Theory,
                theoryDate);

        Assert.Equal(
            HttpStatusCode.NoContent,
            theoryResponse.StatusCode);

        await MarkAppointmentAsPassedAsync(
            factory,
            seed.LocalApplicationId,
            TestTypeEnum.Theory,
            seed.UserId);

        var writtenDate =
            DateTime.UtcNow.AddDays(2);

        using var writtenResponse =
            await ScheduleAsync(
                client,
                seed.LocalApplicationId,
                TestTypeEnum.Written,
                writtenDate);

        Assert.Equal(
            HttpStatusCode.NoContent,
            writtenResponse.StatusCode);

        await using var context =
            factory.CreateDbContext();

        var writtenAppointment =
            await GetAppointmentAsync(
                context,
                seed.LocalApplicationId,
                TestTypeEnum.Written);

        Assert.NotNull(
            writtenAppointment);

        Assert.Equal(
            writtenDate,
            writtenAppointment.AppointmentDate);

        Assert.Equal(
            15m,
            writtenAppointment.PaidFees);

        Assert.False(
            writtenAppointment.IsLocked);
    }


    [Fact]
    public async Task SchedulePractical_BeforeWrittenIsPassed_ReturnsConflictAndDoesNotCreateAppointment()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidSchedulingScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        using var response =
            await ScheduleAsync(
                client,
                seed.LocalApplicationId,
                TestTypeEnum.Practical,
                DateTime.UtcNow.AddDays(1));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await using var context =
            factory.CreateDbContext();

        var appointments =
            await context.TestAppointments
                .AsNoTracking()
                .Where(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                            seed.LocalApplicationId)
                .ToListAsync();

        Assert.Empty(
            appointments);
    }


    [Fact]
    public async Task SchedulePractical_AfterTheoryAndWrittenArePassed_CreatesAppointment()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidSchedulingScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        await ScheduleAndPassAsync(
            factory,
            client,
            seed.LocalApplicationId,
            TestTypeEnum.Theory,
            DateTime.UtcNow.AddDays(1),
            seed.UserId);

        await ScheduleAndPassAsync(
            factory,
            client,
            seed.LocalApplicationId,
            TestTypeEnum.Written,
            DateTime.UtcNow.AddDays(2),
            seed.UserId);

        var practicalDate =
            DateTime.UtcNow.AddDays(3);

        using var response =
            await ScheduleAsync(
                client,
                seed.LocalApplicationId,
                TestTypeEnum.Practical,
                practicalDate);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var context =
            factory.CreateDbContext();

        var practicalAppointment =
            await GetAppointmentAsync(
                context,
                seed.LocalApplicationId,
                TestTypeEnum.Practical);

        Assert.NotNull(
            practicalAppointment);

        Assert.Equal(
            practicalDate,
            practicalAppointment.AppointmentDate);

        Assert.Equal(
            20m,
            practicalAppointment.PaidFees);

        Assert.False(
            practicalAppointment.IsLocked);
    }


    [Fact]
    public async Task ScheduleAnyTest_AfterAllRequiredTestsArePassed_ReturnsConflictAndDoesNotCreateAppointment()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidSchedulingScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        await ScheduleAndPassAsync(
            factory,
            client,
            seed.LocalApplicationId,
            TestTypeEnum.Theory,
            DateTime.UtcNow.AddDays(1),
            seed.UserId);

        await ScheduleAndPassAsync(
            factory,
            client,
            seed.LocalApplicationId,
            TestTypeEnum.Written,
            DateTime.UtcNow.AddDays(2),
            seed.UserId);

        await ScheduleAndPassAsync(
            factory,
            client,
            seed.LocalApplicationId,
            TestTypeEnum.Practical,
            DateTime.UtcNow.AddDays(3),
            seed.UserId);

        using var response =
            await ScheduleAsync(
                client,
                seed.LocalApplicationId,
                TestTypeEnum.Practical,
                DateTime.UtcNow.AddDays(4));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await using var context =
            factory.CreateDbContext();

        var appointments =
            await context.TestAppointments
                .AsNoTracking()
                .Where(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                            seed.LocalApplicationId)
                .ToListAsync();

        Assert.Equal(
            3,
            appointments.Count);
    }


    [Fact]
    public async Task ScheduleTheory_AfterPreviousTheoryAttemptFailed_CreatesRetakeApplicationAndLinksItToAppointment()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidSchedulingScenarioAsync(
                factory,
                includeRetakeApplicationType: true);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        await ScheduleAndFailAsync(
            factory,
            client,
            seed.LocalApplicationId,
            TestTypeEnum.Theory,
            DateTime.UtcNow.AddDays(1),
            seed.UserId);

        var retakeDate =
            DateTime.UtcNow.AddDays(2);

        using var response =
            await ScheduleAsync(
                client,
                seed.LocalApplicationId,
                TestTypeEnum.Theory,
                retakeDate);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var context =
            factory.CreateDbContext();

        var appointments =
            await context.TestAppointments
                .AsNoTracking()
                .Where(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                            seed.LocalApplicationId
                        &&
                        x.TestTypeID ==
                            (int)TestTypeEnum.Theory)
                .OrderBy(
                    x => x.AppointmentDate)
                .ToListAsync();

        Assert.Equal(
            2,
            appointments.Count);

        var retakeAppointment =
            appointments[1];

        Assert.Equal(
            retakeDate,
            retakeAppointment.AppointmentDate);

        Assert.Equal(
            10m,
            retakeAppointment.PaidFees);

        Assert.NotNull(
            retakeAppointment.RetakeTestApplicationID);

        var retakeApplication =
            await context.Applications
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.ApplicationID ==
                            retakeAppointment.RetakeTestApplicationID);

        Assert.Equal(
            7,
            retakeApplication.ApplicationTypeID);

        Assert.Equal(
            seed.ApplicantPersonId,
            retakeApplication.ApplicantPersonID);

        Assert.Equal(
            seed.UserId,
            retakeApplication.CreatedByUserID);

        Assert.Equal(
            AppStatus.New,
            retakeApplication.ApplicationStatus);

        Assert.Equal(
            5m,
            retakeApplication.PaidFees);
    }


    [Fact]
    public async Task ScheduleRetake_WhenAppointmentInsertFails_RollsBackRetakeApplicationAndAppointment()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidSchedulingScenarioAsync(
                factory,
                includeRetakeApplicationType: true);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        await ScheduleAndFailAsync(
            factory,
            client,
            seed.LocalApplicationId,
            TestTypeEnum.Theory,
            DateTime.UtcNow.AddDays(1),
            seed.UserId);

        await AddAppointmentFailureTriggerAsync(
            factory);

        try
        {
            using var response =
                await ScheduleAsync(
                    client,
                    seed.LocalApplicationId,
                    TestTypeEnum.Theory,
                    DateTime.UtcNow.AddDays(2));

            Assert.Equal(
                HttpStatusCode.InternalServerError,
                response.StatusCode);

            await using var context =
                factory.CreateDbContext();

            var appointments =
                await context.TestAppointments
                    .AsNoTracking()
                    .Where(
                        x =>
                            x.LocalDrivingLicenseApplicationID ==
                                seed.LocalApplicationId
                            &&
                            x.TestTypeID ==
                                (int)TestTypeEnum.Theory)
                    .ToListAsync();

            Assert.Single(
                appointments);

            var retakeApplications =
                await context.Applications
                    .AsNoTracking()
                    .Where(
                        x =>
                            x.ApplicantPersonID ==
                                seed.ApplicantPersonId
                            &&
                            x.ApplicationTypeID == 7)
                    .ToListAsync();

            Assert.Empty(
                retakeApplications);
        }
        finally
        {
            await RemoveAppointmentFailureTriggerAsync(
                factory);
        }
    }


    [Fact]
    public async Task ScheduleTheory_ConcurrentRequests_CreateOnlyOneAppointment()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidSchedulingScenarioAsync(
                factory,
                createSecondUser: true);

        using var client1 =
            factory.CreateClient();

        using var client2 =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client1,
            seed.UserId);

        ConfigureAuthenticatedClient(
            client2,
            seed.SecondUserId);

        var startGate =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var date1 =
            DateTime.UtcNow.AddDays(1);

        var date2 =
            DateTime.UtcNow.AddDays(2);

        var task1 =
            ScheduleAfterGateAsync(
                client1,
                seed.LocalApplicationId,
                TestTypeEnum.Theory,
                date1,
                startGate);

        var task2 =
            ScheduleAfterGateAsync(
                client2,
                seed.LocalApplicationId,
                TestTypeEnum.Theory,
                date2,
                startGate);

        startGate.SetResult(
            true);

        var responses =
            await Task.WhenAll(
                task1,
                task2);

        var statuses =
            responses
                .Select(
                    x => x.StatusCode)
                .OrderBy(
                    x => x)
                .ToList();

        Assert.Contains(
            HttpStatusCode.NoContent,
            statuses);

        Assert.Contains(
            HttpStatusCode.Conflict,
            statuses);

        await using var context =
            factory.CreateDbContext();

        var appointments =
            await context.TestAppointments
                .AsNoTracking()
                .Where(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                            seed.LocalApplicationId
                        &&
                        x.TestTypeID ==
                            (int)TestTypeEnum.Theory)
                .ToListAsync();

        Assert.Single(
            appointments);
    }


    [Fact]
    public async Task ScheduleTheory_WhenUserIsNotAuthenticated_ReturnsUnauthorizedAndDoesNotCreateAppointment()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidSchedulingScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        using var response =
            await ScheduleAsync(
                client,
                seed.LocalApplicationId,
                TestTypeEnum.Theory,
                DateTime.UtcNow.AddDays(1));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        await using var context =
            factory.CreateDbContext();

        var appointments =
            await context.TestAppointments
                .AsNoTracking()
                .Where(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                            seed.LocalApplicationId)
                .ToListAsync();

        Assert.Empty(
            appointments);
    }

    [Fact]
    public async Task AddAppointment_ConcurrentRequests_CreateOnlyOneAppointment()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidSchedulingScenarioAsync(
                factory,
                createSecondUser: true);

        using var client1 =
            factory.CreateClient();

        using var client2 =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client1,
            seed.UserId);

        ConfigureAuthenticatedClient(
            client2,
            seed.SecondUserId);

        var startGate =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var date1 =
            DateTime.UtcNow.AddDays(1);

        var date2 =
            DateTime.UtcNow.AddDays(2);

        var task1 =
            AddAppointmentAfterGateAsync(
                client1,
                seed.LocalApplicationId,
                TestTypeEnum.Theory,
                date1,
                startGate);

        var task2 =
            AddAppointmentAfterGateAsync(
                client2,
                seed.LocalApplicationId,
                TestTypeEnum.Theory,
                date2,
                startGate);

        startGate.SetResult(true);

        var responses =
            await Task.WhenAll(
                task1,
                task2);

        var statuses =
            responses
                .Select(
                    x => x.StatusCode)
                .OrderBy(
                    x => x)
                .ToList();

        Assert.Contains(
            HttpStatusCode.NoContent,
            statuses);

        Assert.Contains(
            HttpStatusCode.Conflict,
            statuses);

        await using var context =
            factory.CreateDbContext();

        var appointments =
            await context.TestAppointments
                .AsNoTracking()
                .Where(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                            seed.LocalApplicationId
                        &&
                        x.TestTypeID ==
                            (int)TestTypeEnum.Theory)
                .ToListAsync();

        Assert.Single(
            appointments);
    }

    private static async Task<HttpResponseMessage> ScheduleAsync(
        HttpClient client,
        int localApplicationId,
        TestTypeEnum testType,
        DateTime appointmentDate)
    {
        var request =
            new ScheduleTestRequest
            {
                LocalDrivingLicenseApplicationId =
                    localApplicationId,

                TestTypeId =
                    (int)testType,

                AppointmentDate =
                    appointmentDate
            };

        return await client.PostAsJsonAsync(
            "/api/TestAppointments/schedule",
            request);
    }


    private static async Task<HttpResponseMessage>
        ScheduleAfterGateAsync(
            HttpClient client,
            int localApplicationId,
            TestTypeEnum testType,
            DateTime appointmentDate,
            TaskCompletionSource<bool> startGate)
    {
        await startGate.Task;

        return await ScheduleAsync(
            client,
            localApplicationId,
            testType,
            appointmentDate);
    }

    private static async Task<HttpResponseMessage>
    AddAppointmentAfterGateAsync(
        HttpClient client,
        int localApplicationId,
        TestTypeEnum testType,
        DateTime appointmentDate,
        TaskCompletionSource<bool> startGate)
    {
        await startGate.Task;

        var request =
            new CreateTestAppointmentRequest
            {
                TestTypeId =
                    (int)testType,

                LocalDrivingLicenseApplicationId =
                    localApplicationId,

                AppointmentDate =
                    appointmentDate,

                RetakeTestApplicationId =
                    null
            };

        return await client.PostAsJsonAsync(
            "/api/TestAppointments",
            request);
    }

    private static async Task ScheduleAndPassAsync(
        SqlServerApiWebApplicationFactory factory,
        HttpClient client,
        int localApplicationId,
        TestTypeEnum testType,
        DateTime appointmentDate,
        int userId)
    {
        using var response =
            await ScheduleAsync(
                client,
                localApplicationId,
                testType,
                appointmentDate);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await MarkAppointmentResultAsync(
            factory,
            localApplicationId,
            testType,
            userId,
            true);
    }


    private static async Task ScheduleAndFailAsync(
        SqlServerApiWebApplicationFactory factory,
        HttpClient client,
        int localApplicationId,
        TestTypeEnum testType,
        DateTime appointmentDate,
        int userId)
    {
        using var response =
            await ScheduleAsync(
                client,
                localApplicationId,
                testType,
                appointmentDate);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await MarkAppointmentResultAsync(
            factory,
            localApplicationId,
            testType,
            userId,
            false);
    }


    private static async Task MarkAppointmentAsPassedAsync(
        SqlServerApiWebApplicationFactory factory,
        int localApplicationId,
        TestTypeEnum testType,
        int userId)
    {
        await MarkAppointmentResultAsync(
            factory,
            localApplicationId,
            testType,
            userId,
            true);
    }


    private static async Task MarkAppointmentResultAsync(
        SqlServerApiWebApplicationFactory factory,
        int localApplicationId,
        TestTypeEnum testType,
        int userId,
        bool result)
    {
        await using var context =
            factory.CreateDbContext();

        var appointment =
            await context.TestAppointments
                .SingleAsync(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                            localApplicationId
                        &&
                        x.TestTypeID ==
                            (int)testType);

        context.Tests.Add(
            new Test
            {
                TestAppointmentID =
                    appointment.TestAppointmentID,

                TestResult =
                    result,

                CreatedByUserID =
                    userId,

                Notes =
                    result
                        ? "Integration test passed."
                        : "Integration test failed."
            });

        appointment.IsLocked = true;

        await context.SaveChangesAsync();
    }


    private static async Task<TestAppointment?>
        GetAppointmentAsync(
            DVLDDbContext context,
            int localApplicationId,
            TestTypeEnum testType)
    {
        return await context.TestAppointments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.LocalDrivingLicenseApplicationID ==
                        localApplicationId
                    &&
                    x.TestTypeID ==
                        (int)testType);
    }


    private static Task<int> GetTrialCountAsync(
        DVLDDbContext context,
        int localApplicationId,
        TestTypeEnum testType)
    {
        return context.TestAppointments
            .AsNoTracking()
            .CountAsync(
                x =>
                    x.LocalDrivingLicenseApplicationID ==
                        localApplicationId
                    &&
                    x.TestTypeID ==
                        (int)testType);
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
            $"integration.test.{userId}");

        client.DefaultRequestHeaders.Add(
            "X-Test-FullName",
            "Integration Test User");

        client.DefaultRequestHeaders.Add(
            "X-Test-Role",
            "Staff");
    }


    private static async Task<SeedData>
        SeedValidSchedulingScenarioAsync(
            SqlServerApiWebApplicationFactory factory,
            bool includeRetakeApplicationType = false,
            bool createSecondUser = false)
    {
        await using var context =
            factory.CreateDbContext();

        await context.Database.OpenConnectionAsync();

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT ApplicationTypes ON");

            var applicationTypesSql =
                includeRetakeApplicationType
                    ?
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
                    ),
                    (
                        7,
                        N'Retake Test',
                        5
                    )
                    """
                    :
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
                    """;

            await context.Database.ExecuteSqlRawAsync(
                applicationTypesSql);

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
                    15
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


        var uniqueId =
            Guid.NewGuid().ToString("N");


        var country =
            new Country
            {
                CountryName =
                    $"Integration Country {uniqueId[..8]}"
            };


        var person =
            new Person
            {
                NationalNo =
                    $"IT{uniqueId[..18]}",

                FirstName =
                    "Integration",

                SecondName =
                    "Test",

                LastName =
                    uniqueId[..8],

                DateOfBirth =
                    new DateTime(1990, 1, 1),

                Gender =
                    Gender.Male,

                Address =
                    "Integration Test Address",

                Phone =
                    $"079{uniqueId[..7]}",

                Email =
                    $"integration.{uniqueId}@example.com",

                Country =
                    country
            };


        var user =
            new User
            {
                Person =
                    person,

                UserName =
                    $"test_{uniqueId[..18]}",

                Password =
                    "IntegrationTestPassword",

                IsActive =
                    true,

                Role =
                    UserRole.Staff
            };

        await context.Users.AddAsync(
            user);


        User? secondUser =
            null;

        if (createSecondUser)
        {
            var secondPerson =
                new Person
                {
                    NationalNo =
                        $"IT2{uniqueId[..17]}",

                    FirstName =
                        "Integration",

                    SecondName =
                        "Second",

                    LastName =
                        uniqueId[..8],

                    DateOfBirth =
                        new DateTime(1991, 1, 1),

                    Gender =
                        Gender.Male,

                    Address =
                        "Integration Test Address",

                    Phone =
                        $"078{uniqueId[..7]}",

                    Email =
                        $"integration.second.{uniqueId}@example.com",

                    Country =
                        country
                };

            secondUser =
                new User
                {
                    Person =
                        secondPerson,

                    UserName =
                        $"second_{uniqueId[..18]}",

                    Password =
                        "IntegrationTestPassword",

                    IsActive =
                        true,

                    Role =
                        UserRole.Staff
                };

            await context.Users.AddAsync(
                secondUser);
        }

        await context.SaveChangesAsync();


        var application =
            new ApplicationD
            {
                ApplicantPersonID =
                    person.PersonId,

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


        var localApplication =
            new LocalDrivingLicenseApplication
            {
                Application =
                    application,

                LicenseClassID =
                    1
            };

        context.LocalDrivingLicenseApplications.Add(
            localApplication);

        await context.SaveChangesAsync();


        return new SeedData(
            UserId:
                user.UserId,

            SecondUserId:
                secondUser?.UserId ?? 0,

            ApplicantPersonId:
                person.PersonId,

            LocalApplicationId:
                localApplication
                    .LocalDrivingLicenseApplicationID);
    }


    private static async Task AddAppointmentFailureTriggerAsync(
        SqlServerApiWebApplicationFactory factory)
    {
        await using var context =
            factory.CreateDbContext();

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER
                TR_TestAppointments_IntegrationTest_BlockInsert
            ON TestAppointments
            AFTER INSERT
            AS
            BEGIN
                SET NOCOUNT ON;

                THROW 50001,
                    'Integration test: appointment insert blocked.',
                    1;
            END
            """);
    }


    private static async Task RemoveAppointmentFailureTriggerAsync(
        SqlServerApiWebApplicationFactory factory)
    {
        await using var context =
            factory.CreateDbContext();

        await context.Database.ExecuteSqlRawAsync(
            """
            DROP TRIGGER
                IF EXISTS
                TR_TestAppointments_IntegrationTest_BlockInsert
            """);
    }


    private sealed record SeedData(
        int UserId,
        int SecondUserId,
        int ApplicantPersonId,
        int LocalApplicationId);
}
