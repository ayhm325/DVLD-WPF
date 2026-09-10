using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests;

public sealed class TestRepositoryTests
    : IClassFixture<TestRepositoryDatabaseFixture>
{
    private readonly TestRepositoryDatabaseFixture _fixture;

    public TestRepositoryTests(
        TestRepositoryDatabaseFixture fixture) =>
        _fixture = fixture;

    // ============================================================
    // Constructor
    // ============================================================

    [Fact]
    public void Constructor_ShouldThrowWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TestRepository(null!));
    }

    // ============================================================
    // GetByIdAsync
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTestWithNavigationProperties()
    {
        var scenario =
            await SeedScenarioAsync();

        var testId =
            await AddTestAsync(
                scenario,
                passed: true,
                notes: "Passed integration test");

        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestRepository(context);

        var result =
            await repository.GetByIdAsync(testId);

        Assert.NotNull(result);

        Assert.Equal(
            testId,
            result.TestID);

        Assert.Equal(
            scenario.AppointmentId,
            result.TestAppointmentID);

        Assert.True(
            result.TestResult);

        Assert.Equal(
            "Passed integration test",
            result.Notes);

        Assert.Equal(
            scenario.UserId,
            result.CreatedByUserID);

        Assert.NotNull(
            result.TestAppointment);

        Assert.Equal(
            scenario.AppointmentId,
            result.TestAppointment.TestAppointmentID);

        Assert.NotNull(
            result.TestAppointment.TestType);

        Assert.Equal(
            (int)TestTypeEnum.Theory,
            result.TestAppointment.TestTypeID);

        Assert.NotNull(
            result.User);

        Assert.Equal(
            scenario.UserId,
            result.User.UserId);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result).State);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullWhenTestDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestRepository(context);

        var result =
            await repository.GetByIdAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForInvalidId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestRepository(context);

        var result =
            await repository.GetByIdAsync(0);

        Assert.Null(result);
    }

    // ============================================================
    // GetAllAsync
    // ============================================================

    [Fact]
    public async Task GetAllAsync_ShouldReturnTestsOrderedByTestIdDescending()
    {
        var firstScenario =
            await SeedScenarioAsync();

        var secondScenario =
            await SeedScenarioAsync();

        var firstTestId =
            await AddTestAsync(
                firstScenario,
                passed: true);

        var secondTestId =
            await AddTestAsync(
                secondScenario,
                passed: false);

        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestRepository(context);

        var result =
            await repository.GetAllAsync();

        var firstIndex =
            result.FindIndex(
                x =>
                    x.TestID == firstTestId);

        var secondIndex =
            result.FindIndex(
                x =>
                    x.TestID == secondTestId);

        Assert.True(
            firstIndex >= 0);

        Assert.True(
            secondIndex >= 0);

        if (secondTestId > firstTestId)
        {
            Assert.True(
                secondIndex < firstIndex);
        }
        else
        {
            Assert.True(
                firstIndex < secondIndex);
        }
    }

    // ============================================================
    // GetByTestAppointmentIdAsync
    // ============================================================

    [Fact]
    public async Task GetByTestAppointmentIdAsync_ShouldReturnMatchingTest()
    {
        var scenario =
            await SeedScenarioAsync();

        var testId =
            await AddTestAsync(
                scenario,
                passed: true);

        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestRepository(context);

        var result =
            await repository.GetByTestAppointmentIdAsync(
                scenario.AppointmentId);

        Assert.Single(
            result);

        Assert.Equal(
            testId,
            result[0].TestID);

        Assert.Equal(
            scenario.AppointmentId,
            result[0].TestAppointmentID);

        Assert.NotNull(
            result[0].TestAppointment);

        Assert.NotNull(
            result[0].TestAppointment.TestType);

        Assert.NotNull(
            result[0].User);
    }

    [Fact]
    public async Task GetByTestAppointmentIdAsync_ShouldReturnEmptyWhenNoTestExists()
    {
        var scenario =
            await SeedScenarioAsync();

        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestRepository(context);

        var result =
            await repository.GetByTestAppointmentIdAsync(
                scenario.AppointmentId);

        Assert.Empty(
            result);
    }

    [Fact]
    public async Task GetByTestAppointmentIdAsync_ShouldReturnEmptyForInvalidId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestRepository(context);

        var result =
            await repository.GetByTestAppointmentIdAsync(
                0);

        Assert.Empty(
            result);
    }

    // ============================================================
    // GetByUserIdAsync
    // ============================================================

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnOnlyTestsCreatedByUserOrderedDescending()
    {
        var firstScenario =
            await SeedScenarioAsync();

        var secondScenario =
            await SeedScenarioAsync();

        var firstTestId =
            await AddTestAsync(
                firstScenario,
                passed: true);

        var secondAppointmentId =
            await AddAppointmentAsync(
                firstScenario);

        var secondTestId =
            await AddTestAsync(
                firstScenario,
                secondAppointmentId,
                passed: false);

        var otherUserTestId =
            await AddTestAsync(
                secondScenario,
                passed: true);

        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestRepository(context);

        var result =
            await repository.GetByUserIdAsync(
                firstScenario.UserId);

        Assert.Equal(
            2,
            result.Count);

        Assert.All(
            result,
            test =>
                Assert.Equal(
                    firstScenario.UserId,
                    test.CreatedByUserID));

        Assert.DoesNotContain(
            result,
            test =>
                test.TestID ==
                otherUserTestId);

        var firstIndex =
            result.FindIndex(
                x =>
                    x.TestID ==
                    firstTestId);

        var secondIndex =
            result.FindIndex(
                x =>
                    x.TestID ==
                    secondTestId);

        Assert.True(
            firstIndex >= 0);

        Assert.True(
            secondIndex >= 0);

        Assert.True(
            secondIndex < firstIndex);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnEmptyForUserWithoutTests()
    {
        var scenario =
            await SeedScenarioAsync();

        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestRepository(context);

        var result =
            await repository.GetByUserIdAsync(
                scenario.UserId);

        Assert.Empty(
            result);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnEmptyForInvalidId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestRepository(context);

        var result =
            await repository.GetByUserIdAsync(
                0);

        Assert.Empty(
            result);
    }

    // ============================================================
    // IsTestAlreadyTakenAsync
    // ============================================================

    [Fact]
    public async Task IsTestAlreadyTakenAsync_ShouldReturnTrueWhenTestExists()
    {
        var scenario =
            await SeedScenarioAsync();

        await AddTestAsync(
            scenario,
            passed: true);

        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestRepository(context);

        var result =
            await repository.IsTestAlreadyTakenAsync(
                scenario.AppointmentId);

        Assert.True(
            result);
    }

    [Fact]
    public async Task IsTestAlreadyTakenAsync_ShouldReturnFalseWhenTestDoesNotExist()
    {
        var scenario =
            await SeedScenarioAsync();

        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestRepository(context);

        var result =
            await repository.IsTestAlreadyTakenAsync(
                scenario.AppointmentId);

        Assert.False(
            result);
    }

    [Fact]
    public async Task IsTestAlreadyTakenAsync_ShouldReturnFalseForInvalidId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestRepository(context);

        var result =
            await repository.IsTestAlreadyTakenAsync(
                0);

        Assert.False(
            result);
    }

    // ============================================================
    // AddAsync
    // ============================================================

    [Fact]
    public async Task AddAsync_ShouldPersistTest()
    {
        var scenario =
            await SeedScenarioAsync();

        var test =
            new Test
            {
                TestAppointmentID =
                    scenario.AppointmentId,

                TestResult =
                    true,

                Notes =
                    "Persisted integration test",

                CreatedByUserID =
                    scenario.UserId
            };

        await using (var context =
            _fixture.Database.CreateContext())
        {
            var repository =
                new TestRepository(context);

            await repository.AddAsync(
                test);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.Database.CreateContext();

        var persisted =
            await verificationContext.Tests
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.TestID ==
                        test.TestID);

        Assert.NotNull(
            persisted);

        Assert.Equal(
            scenario.AppointmentId,
            persisted.TestAppointmentID);

        Assert.True(
            persisted.TestResult);

        Assert.Equal(
            "Persisted integration test",
            persisted.Notes);

        Assert.Equal(
            scenario.UserId,
            persisted.CreatedByUserID);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistNullNotes()
    {
        var scenario =
            await SeedScenarioAsync();

        var test =
            new Test
            {
                TestAppointmentID =
                    scenario.AppointmentId,

                TestResult =
                    false,

                Notes =
                    null,

                CreatedByUserID =
                    scenario.UserId
            };

        await using (var context =
            _fixture.Database.CreateContext())
        {
            var repository =
                new TestRepository(context);

            await repository.AddAsync(
                test);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.Database.CreateContext();

        var persisted =
            await verificationContext.Tests
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.TestID ==
                        test.TestID);

        Assert.NotNull(
            persisted);

        Assert.False(
            persisted.TestResult);

        Assert.Null(
            persisted.Notes);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowWhenTestIsNull()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                repository.AddAsync(
                    null!));
    }

    // ============================================================
    // Test Data
    // ============================================================

    private async Task<ScenarioSeed> SeedScenarioAsync()
    {
        var uniqueId =
            Guid.NewGuid().ToString("N");

        var country =
            new Country
            {
                CountryName =
                    $"Test Repository Country {uniqueId[..8]}"
            };

        var person =
            new Person
            {
                NationalNo =
                    $"TR{uniqueId[..18]}",

                FirstName =
                    "Repository",

                SecondName =
                    "Integration",

                ThirdName =
                    null,

                LastName =
                    uniqueId[..8],

                DateOfBirth =
                    new DateTime(
                        1990,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Test Repository Address",

                Phone =
                    $"079{uniqueId[..7]}",

                Email =
                    $"test.repository.{uniqueId}@example.com",

                Country =
                    country
            };

        var user =
            new User
            {
                Person =
                    person,

                UserName =
                    $"test_repository_{uniqueId[..18]}",

                Password =
                    "IntegrationTestPassword",

                IsActive =
                    true,

                Role =
                    UserRole.Staff
            };

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    $"Test Repository Application Type {uniqueId[..8]}",

                ApplicationFees =
                    50m
            };

        var licenseClass =
            new LicenseClass
            {
                ClassName =
                    $"Test Repository Class {uniqueId[..8]}",

                ClassDescription =
                    "Test repository integration license class",

                MinimumAllowedAge =
                    18,

                DefaultValidityLength =
                    10,

                ClassFees =
                    100m
            };

        var application =
            new ApplicationD
            {
                Person =
                    person,

                CreatedByUser =
                    user,

                ApplicationType =
                    applicationType,

                ApplicationDate =
                    DateTime.UtcNow,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    DateTime.UtcNow,

                PaidFees =
                    50m
            };

        var localApplication =
            new LocalDrivingLicenseApplication
            {
                Application =
                    application,

                LicenseClass =
                    licenseClass
            };

        await using var context =
            _fixture.Database.CreateContext();

        context.LocalDrivingLicenseApplications.Add(
            localApplication);

        await context.SaveChangesAsync();

        var appointment =
            new TestAppointment
            {
                TestTypeID =
                    (int)TestTypeEnum.Theory,

                LocalDrivingLicenseApplicationID =
                    localApplication
                        .LocalDrivingLicenseApplicationID,

                AppointmentDate =
                    DateTime.UtcNow.AddDays(1),

                PaidFees =
                    20m,

                CreatedByUserID =
                    user.UserId,

                IsLocked =
                    true,

                RetakeTestApplicationID =
                    null
            };

        context.TestAppointments.Add(
            appointment);

        await context.SaveChangesAsync();

        return new ScenarioSeed(
            PersonId:
                person.PersonId,

            UserId:
                user.UserId,

            ApplicationId:
                application.ApplicationID,

            LocalApplicationId:
                localApplication
                    .LocalDrivingLicenseApplicationID,

            AppointmentId:
                appointment.TestAppointmentID);
    }

    private async Task<int> AddAppointmentAsync(
        ScenarioSeed scenario)
    {
        var appointment =
            new TestAppointment
            {
                TestTypeID =
                    (int)TestTypeEnum.Written,

                LocalDrivingLicenseApplicationID =
                    scenario.LocalApplicationId,

                AppointmentDate =
                    DateTime.UtcNow.AddDays(2),

                PaidFees =
                    20m,

                CreatedByUserID =
                    scenario.UserId,

                IsLocked =
                    true,

                RetakeTestApplicationID =
                    null
            };

        await using var context =
            _fixture.Database.CreateContext();

        context.TestAppointments.Add(
            appointment);

        await context.SaveChangesAsync();

        return appointment.TestAppointmentID;
    }

    private async Task<int> AddTestAsync(
        ScenarioSeed scenario,
        bool passed,
        string? notes = null)
    {
        return await AddTestAsync(
            scenario,
            scenario.AppointmentId,
            passed,
            notes);
    }

    private async Task<int> AddTestAsync(
        ScenarioSeed scenario,
        int appointmentId,
        bool passed,
        string? notes = null)
    {
        var test =
            new Test
            {
                TestAppointmentID =
                    appointmentId,

                TestResult =
                    passed,

                Notes =
                    notes,

                CreatedByUserID =
                    scenario.UserId
            };

        await using var context =
            _fixture.Database.CreateContext();

        context.Tests.Add(
            test);

        await context.SaveChangesAsync();

        return test.TestID;
    }

    private sealed record ScenarioSeed(
        int PersonId,
        int UserId,
        int ApplicationId,
        int LocalApplicationId,
        int AppointmentId);
}

// ==================================================================
// Fixture
// ==================================================================

public sealed class TestRepositoryDatabaseFixture
    : IAsyncLifetime
{
    public SqlServerTestDatabase Database { get; private set; } = null!;

    private SqlServerTestDatabase? _database;

    public async Task InitializeAsync()
    {
        _database =
            new SqlServerTestDatabase();

        await _database.InitializeAsync();

        Database =
            _database;

        await SeedTestTypesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    private async Task SeedTestTypesAsync()
    {
        await using var context =
            _database!.CreateContext();

        var requiredTestTypes =
            new[]
            {
                new TestType
                {
                    TestTypeId =
                        (int)TestTypeEnum.Theory,

                    TestTypeTitle =
                        "Theory Test",

                    TestTypeDescription =
                        "Integration test theory examination",

                    TestTypeFees =
                        10m
                },

                new TestType
                {
                    TestTypeId =
                        (int)TestTypeEnum.Written,

                    TestTypeTitle =
                        "Written Test",

                    TestTypeDescription =
                        "Integration test written examination",

                    TestTypeFees =
                        15m
                },

                new TestType
                {
                    TestTypeId =
                        (int)TestTypeEnum.Practical,

                    TestTypeTitle =
                        "Practical Test",

                    TestTypeDescription =
                        "Integration test practical examination",

                    TestTypeFees =
                        20m
                }
            };

        var requiredIds =
            requiredTestTypes
                .Select(x =>
                    x.TestTypeId)
                .ToArray();

        var existingIds =
            await context.TestTypes
                .AsNoTracking()
                .Where(x =>
                    requiredIds.Contains(
                        x.TestTypeId))
                .Select(x =>
                    x.TestTypeId)
                .ToListAsync();

        if (existingIds.Count > 0)
        {
            throw new InvalidOperationException(
                "The TestRepository integration-test database already contains one or more required TestType rows.");
        }

        await context.Database.OpenConnectionAsync();

        try
        {
            const string identityQuery =
                """
                SELECT CAST(
                    COLUMNPROPERTY(
                        OBJECT_ID(N'[dbo].[TestTypes]'),
                        N'TestTypeId',
                        'IsIdentity') AS int)
                AS [Value]
                """;

            var isIdentity =
                await context.Database
                    .SqlQueryRaw<int>(
                        identityQuery)
                    .SingleAsync();

            if (isIdentity == 1)
            {
                await context.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT [dbo].[TestTypes] ON;");
            }

            try
            {
                context.TestTypes.AddRange(
                    requiredTestTypes);

                await context.SaveChangesAsync();
            }
            finally
            {
                if (isIdentity == 1)
                {
                    await context.Database.ExecuteSqlRawAsync(
                        "SET IDENTITY_INSERT [dbo].[TestTypes] OFF;");
                }
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}