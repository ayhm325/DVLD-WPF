using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests;

public sealed class TestAppointmentRepositoryTests : IAsyncLifetime
{
    private SqlServerTestDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = new SqlServerTestDatabase();

        await _database.InitializeAsync();

        await SeedRequiredTestTypesAsync();
        await VerifyRequiredTestTypesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    // ============================================================
    // TestType setup
    // ============================================================

    private async Task SeedRequiredTestTypesAsync()
    {
        await using var context =
            _database.CreateContext();

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

        var requiredTestTypeIds =
            requiredTestTypes
                .Select(x => x.TestTypeId)
                .ToArray();

        var existingTestTypeIds =
            await context.TestTypes
                .AsNoTracking()
                .Where(x =>
                    requiredTestTypeIds.Contains(x.TestTypeId))
                .Select(x => x.TestTypeId)
                .ToListAsync();

        if (existingTestTypeIds.Count > 0)
        {
            throw new InvalidOperationException(
                "The integration-test database must not already contain the required TestType rows.");
        }

        await context.Database.OpenConnectionAsync();

        try
        {
            const string identityQuery =
                "SELECT CAST(COLUMNPROPERTY(OBJECT_ID(N'[dbo].[TestTypes]'), N'TestTypeId', 'IsIdentity') AS int) AS [Value]";

            var isIdentity =
                await context.Database
                    .SqlQueryRaw<int>(identityQuery)
                    .SingleAsync();

            if (isIdentity == 1)
            {
                await context.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT [dbo].[TestTypes] ON;");
            }

            try
            {
                context.TestTypes.AddRange(requiredTestTypes);

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

    private async Task VerifyRequiredTestTypesAsync()
    {
        await using var context =
            _database.CreateContext();

        var requiredTestTypes =
            new Dictionary<int, (string Title, decimal Fees)>
            {
                [(int)TestTypeEnum.Theory] =
                    ("Theory Test", 10m),

                [(int)TestTypeEnum.Written] =
                    ("Written Test", 15m),

                [(int)TestTypeEnum.Practical] =
                    ("Practical Test", 20m)
            };

        var existingTestTypes =
            await context.TestTypes
                .AsNoTracking()
                .Where(x =>
                    requiredTestTypes.Keys.Contains(
                        x.TestTypeId))
                .ToListAsync();

        Assert.Equal(
            requiredTestTypes.Count,
            existingTestTypes.Count);

        foreach (var expected in requiredTestTypes)
        {
            var actual =
                existingTestTypes.SingleOrDefault(
                    x => x.TestTypeId == expected.Key);

            Assert.NotNull(actual);

            Assert.Equal(
                expected.Value.Title,
                actual.TestTypeTitle);

            Assert.Equal(
                expected.Value.Fees,
                actual.TestTypeFees);
        }
    }

    // ============================================================
    // Constructor
    // ============================================================

    [Fact]
    public void Constructor_ShouldThrowWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TestAppointmentRepository(null!));
    }

    // ============================================================
    // GetByIdAsync
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAppointmentWithNavigationProperties()
    {
        var scenario =
            await SeedScenarioAsync();

        var appointmentId =
            await AddAppointmentAsync(
                scenario,
                TestTypeEnum.Theory,
                DateTime.UtcNow.AddHours(1));

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetByIdAsync(appointmentId);

        Assert.NotNull(result);

        Assert.Equal(
            appointmentId,
            result.TestAppointmentID);

        Assert.Equal(
            (int)TestTypeEnum.Theory,
            result.TestTypeID);

        Assert.NotNull(result.TestType);

        Assert.NotNull(
            result.LocalDrivingLicenseApplication);

        Assert.NotNull(result.User);

        Assert.Null(result.Test);

        Assert.Null(result.RetakeTestApplication);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result).State);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullWhenAppointmentDoesNotExist()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetByIdAsync(int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForInvalidId()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetByIdAsync(0);

        Assert.Null(result);
    }

    // ============================================================
    // GetForUpdateAsync
    // ============================================================

    [Fact]
    public async Task GetForUpdateAsync_ShouldReturnTrackedAppointment()
    {
        var scenario =
            await SeedScenarioAsync();

        var appointmentId =
            await AddAppointmentAsync(
                scenario,
                TestTypeEnum.Theory,
                DateTime.UtcNow.AddHours(2));

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetForUpdateAsync(
                appointmentId);

        Assert.NotNull(result);

        Assert.Equal(
            appointmentId,
            result.TestAppointmentID);

        Assert.Equal(
            EntityState.Unchanged,
            context.Entry(result).State);
    }

    [Fact]
    public async Task GetForUpdateAsync_ShouldReturnNullForInvalidId()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetForUpdateAsync(0);

        Assert.Null(result);
    }

    // ============================================================
    // GetAllAsync
    // ============================================================

    [Fact]
    public async Task GetAllAsync_ShouldReturnAppointmentsOrderedByAppointmentDateDescending()
    {
        var scenario =
            await SeedScenarioAsync();

        var olderDate =
            DateTime.UtcNow.AddDays(1);

        var newerDate =
            DateTime.UtcNow.AddDays(2);

        var olderAppointmentId =
            await AddAppointmentAsync(
                scenario,
                TestTypeEnum.Theory,
                olderDate);

        var newerAppointmentId =
            await AddAppointmentAsync(
                scenario,
                TestTypeEnum.Written,
                newerDate);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetAllAsync();

        var olderIndex =
            result.FindIndex(
                x =>
                    x.TestAppointmentID ==
                    olderAppointmentId);

        var newerIndex =
            result.FindIndex(
                x =>
                    x.TestAppointmentID ==
                    newerAppointmentId);

        Assert.True(olderIndex >= 0);
        Assert.True(newerIndex >= 0);

        Assert.True(
            newerIndex < olderIndex);
    }

    // ============================================================
    // GetByLocalDrivingLicenseApplicationIdAsync
    // ============================================================

    [Fact]
    public async Task GetByLocalDrivingLicenseApplicationIdAsync_ShouldReturnOnlyMatchingAppointmentsOrderedAscending()
    {
        var matchingScenario =
            await SeedScenarioAsync();

        var unrelatedScenario =
            await SeedScenarioAsync();

        var firstDate =
            DateTime.UtcNow.AddDays(3);

        var secondDate =
            DateTime.UtcNow.AddDays(4);

        var firstAppointmentId =
            await AddAppointmentAsync(
                matchingScenario,
                TestTypeEnum.Written,
                firstDate);

        var secondAppointmentId =
            await AddAppointmentAsync(
                matchingScenario,
                TestTypeEnum.Theory,
                secondDate);

        await AddAppointmentAsync(
            unrelatedScenario,
            TestTypeEnum.Practical,
            DateTime.UtcNow.AddDays(5));

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository
                .GetByLocalDrivingLicenseApplicationIdAsync(
                    matchingScenario.LocalApplicationId);

        Assert.Equal(
            2,
            result.Count);

        Assert.Equal(
            firstAppointmentId,
            result[0].TestAppointmentID);

        Assert.Equal(
            secondAppointmentId,
            result[1].TestAppointmentID);
    }

    [Fact]
    public async Task GetByLocalDrivingLicenseApplicationIdAsync_ShouldReturnEmptyForInvalidId()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository
                .GetByLocalDrivingLicenseApplicationIdAsync(0);

        Assert.Empty(result);
    }

    // ============================================================
    // GetByTestTypeIdAsync
    // ============================================================

    [Fact]
    public async Task GetByTestTypeIdAsync_ShouldReturnOnlyMatchingTestTypeOrderedDescending()
    {
        var scenario =
            await SeedScenarioAsync();

        var olderTheoryDate =
            DateTime.UtcNow.AddDays(6);

        var newerTheoryDate =
            DateTime.UtcNow.AddDays(7);

        var writtenDate =
            DateTime.UtcNow.AddDays(8);

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Theory,
            olderTheoryDate);

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Theory,
            newerTheoryDate);

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Written,
            writtenDate);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetByTestTypeIdAsync(
                TestTypeEnum.Theory);

        Assert.Equal(
            2,
            result.Count);

        Assert.All(
            result,
            appointment =>
                Assert.Equal(
                    (int)TestTypeEnum.Theory,
                    appointment.TestTypeID));

        Assert.True(
            result[0].AppointmentDate >
            result[1].AppointmentDate);
    }

    [Fact]
    public async Task GetByTestTypeIdAsync_ShouldReturnEmptyForUndefinedEnumValue()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetByTestTypeIdAsync(
                (TestTypeEnum)999);

        Assert.Empty(result);
    }

    // ============================================================
    // GetByCreatedUserIdAsync
    // ============================================================

    [Fact]
    public async Task GetByCreatedUserIdAsync_ShouldReturnOnlyAppointmentsCreatedByUser()
    {
        var matchingScenario =
            await SeedScenarioAsync();

        var unrelatedScenario =
            await SeedScenarioAsync();

        var firstDate =
            DateTime.UtcNow.AddDays(9);

        var secondDate =
            DateTime.UtcNow.AddDays(10);

        await AddAppointmentAsync(
            matchingScenario,
            TestTypeEnum.Theory,
            firstDate);

        await AddAppointmentAsync(
            matchingScenario,
            TestTypeEnum.Written,
            secondDate);

        await AddAppointmentAsync(
            unrelatedScenario,
            TestTypeEnum.Practical,
            DateTime.UtcNow.AddDays(11));

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetByCreatedUserIdAsync(
                matchingScenario.UserId);

        Assert.Equal(
            2,
            result.Count);

        Assert.All(
            result,
            appointment =>
                Assert.Equal(
                    matchingScenario.UserId,
                    appointment.CreatedByUserID));

        Assert.True(
            result[0].AppointmentDate >
            result[1].AppointmentDate);
    }

    [Fact]
    public async Task GetByCreatedUserIdAsync_ShouldReturnEmptyForInvalidUserId()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetByCreatedUserIdAsync(0);

        Assert.Empty(result);
    }

    // ============================================================
    // GetScheduleInfoAsync
    // ============================================================

    [Fact]
    public async Task GetScheduleInfoAsync_ShouldLoadCompleteScheduleInformation()
    {
        var scenario =
            await SeedScenarioAsync();

        var appointmentId =
            await AddAppointmentAsync(
                scenario,
                TestTypeEnum.Practical,
                DateTime.UtcNow.AddDays(12));

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetScheduleInfoAsync(
                appointmentId);

        Assert.NotNull(result);

        Assert.Equal(
            appointmentId,
            result.TestAppointmentID);

        Assert.NotNull(result.TestType);

        Assert.Equal(
            (int)TestTypeEnum.Practical,
            result.TestTypeID);

        Assert.NotNull(
            result.LocalDrivingLicenseApplication);

        Assert.NotNull(
            result.LocalDrivingLicenseApplication
                .Application);

        Assert.NotNull(
            result.LocalDrivingLicenseApplication
                .Application
                .Person);

        Assert.NotNull(
            result.LocalDrivingLicenseApplication
                .Application
                .ApplicationType);

        Assert.NotNull(
            result.LocalDrivingLicenseApplication
                .LicenseClass);

        Assert.Equal(
            scenario.PersonId,
            result.LocalDrivingLicenseApplication
                .Application
                .Person
                .PersonId);

        Assert.Equal(
            scenario.ApplicationTypeId,
            result.LocalDrivingLicenseApplication
                .Application
                .ApplicationType!
                .ApplicationTypeId);

        Assert.Equal(
            scenario.LicenseClassId,
            result.LocalDrivingLicenseApplication
                .LicenseClass
                .LicenseClassID);
    }

    [Fact]
    public async Task GetScheduleInfoAsync_ShouldReturnNullForInvalidId()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetScheduleInfoAsync(0);

        Assert.Null(result);
    }

    // ============================================================
    // HasUserConflictAsync
    // ============================================================

    [Fact]
    public async Task HasUserConflictAsync_ShouldReturnTrueForSameUserSameDateUnlockedAppointment()
    {
        var scenario =
            await SeedScenarioAsync();

        var appointmentDate =
            DateTime.UtcNow.AddDays(13);

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Theory,
            appointmentDate,
            isLocked: false);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.HasUserConflictAsync(
                scenario.UserId,
                appointmentDate);

        Assert.True(result);
    }

    [Fact]
    public async Task HasUserConflictAsync_ShouldReturnFalseForLockedAppointment()
    {
        var scenario =
            await SeedScenarioAsync();

        var appointmentDate =
            DateTime.UtcNow.AddDays(14);

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Theory,
            appointmentDate,
            isLocked: true);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.HasUserConflictAsync(
                scenario.UserId,
                appointmentDate);

        Assert.False(result);
    }

    [Fact]
    public async Task HasUserConflictAsync_ShouldIgnoreExcludedAppointment()
    {
        var scenario =
            await SeedScenarioAsync();

        var appointmentDate =
            DateTime.UtcNow.AddDays(15);

        var appointmentId =
            await AddAppointmentAsync(
                scenario,
                TestTypeEnum.Theory,
                appointmentDate);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.HasUserConflictAsync(
                scenario.UserId,
                appointmentDate,
                appointmentId);

        Assert.False(result);
    }

    [Fact]
    public async Task HasUserConflictAsync_ShouldReturnFalseForDifferentDate()
    {
        var scenario =
            await SeedScenarioAsync();

        var appointmentDate =
            DateTime.UtcNow.AddDays(16);

        var differentDate =
            appointmentDate.AddHours(1);

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Theory,
            appointmentDate);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.HasUserConflictAsync(
                scenario.UserId,
                differentDate);

        Assert.False(result);
    }

    [Fact]
    public async Task HasUserConflictAsync_ShouldReturnFalseForInvalidUserId()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.HasUserConflictAsync(
                0,
                DateTime.UtcNow);

        Assert.False(result);
    }

    // ============================================================
    // HasLocalApplicationConflictAsync
    // ============================================================

    [Fact]
    public async Task HasLocalApplicationConflictAsync_ShouldReturnTrueForSameApplicationSameDateUnlockedAppointment()
    {
        var scenario =
            await SeedScenarioAsync();

        var appointmentDate =
            DateTime.UtcNow.AddDays(17);

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Theory,
            appointmentDate,
            isLocked: false);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.HasLocalApplicationConflictAsync(
                scenario.LocalApplicationId,
                appointmentDate);

        Assert.True(result);
    }

    [Fact]
    public async Task HasLocalApplicationConflictAsync_ShouldReturnFalseForLockedAppointment()
    {
        var scenario =
            await SeedScenarioAsync();

        var appointmentDate =
            DateTime.UtcNow.AddDays(18);

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Theory,
            appointmentDate,
            isLocked: true);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.HasLocalApplicationConflictAsync(
                scenario.LocalApplicationId,
                appointmentDate);

        Assert.False(result);
    }

    [Fact]
    public async Task HasLocalApplicationConflictAsync_ShouldIgnoreExcludedAppointment()
    {
        var scenario =
            await SeedScenarioAsync();

        var appointmentDate =
            DateTime.UtcNow.AddDays(19);

        var appointmentId =
            await AddAppointmentAsync(
                scenario,
                TestTypeEnum.Theory,
                appointmentDate);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.HasLocalApplicationConflictAsync(
                scenario.LocalApplicationId,
                appointmentDate,
                appointmentId);

        Assert.False(result);
    }

    [Fact]
    public async Task HasLocalApplicationConflictAsync_ShouldReturnFalseForDifferentDate()
    {
        var scenario =
            await SeedScenarioAsync();

        var appointmentDate =
            DateTime.UtcNow.AddDays(20);

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Theory,
            appointmentDate);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.HasLocalApplicationConflictAsync(
                scenario.LocalApplicationId,
                appointmentDate.AddMinutes(1));

        Assert.False(result);
    }

    [Fact]
    public async Task HasLocalApplicationConflictAsync_ShouldReturnFalseForInvalidLocalApplicationId()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.HasLocalApplicationConflictAsync(
                0,
                DateTime.UtcNow);

        Assert.False(result);
    }

    // ============================================================
    // IsAppointmentAlreadyScheduledAsync
    // ============================================================

    [Fact]
    public async Task IsAppointmentAlreadyScheduledAsync_ShouldReturnTrueForUnlockedMatchingAppointment()
    {
        var scenario =
            await SeedScenarioAsync();

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Theory,
            DateTime.UtcNow.AddDays(21),
            isLocked: false);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.IsAppointmentAlreadyScheduledAsync(
                scenario.LocalApplicationId,
                (int)TestTypeEnum.Theory);

        Assert.True(result);
    }

    [Fact]
    public async Task IsAppointmentAlreadyScheduledAsync_ShouldReturnFalseForLockedMatchingAppointment()
    {
        var scenario =
            await SeedScenarioAsync();

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Theory,
            DateTime.UtcNow.AddDays(22),
            isLocked: true);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.IsAppointmentAlreadyScheduledAsync(
                scenario.LocalApplicationId,
                (int)TestTypeEnum.Theory);

        Assert.False(result);
    }

    [Fact]
    public async Task IsAppointmentAlreadyScheduledAsync_ShouldReturnFalseForDifferentTestType()
    {
        var scenario =
            await SeedScenarioAsync();

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Written,
            DateTime.UtcNow.AddDays(23),
            isLocked: false);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.IsAppointmentAlreadyScheduledAsync(
                scenario.LocalApplicationId,
                (int)TestTypeEnum.Theory);

        Assert.False(result);
    }

    [Fact]
    public async Task IsAppointmentAlreadyScheduledAsync_ShouldReturnFalseForInvalidArguments()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var invalidLocalApplication =
            await repository.IsAppointmentAlreadyScheduledAsync(
                0,
                (int)TestTypeEnum.Theory);

        var invalidTestType =
            await repository.IsAppointmentAlreadyScheduledAsync(
                1,
                0);

        Assert.False(invalidLocalApplication);
        Assert.False(invalidTestType);
    }

    // ============================================================
    // GetApplicationStatusAsync
    // ============================================================

    [Fact]
    public async Task GetApplicationStatusAsync_ShouldReturnApplicationStatus()
    {
        var scenario =
            await SeedScenarioAsync(
                AppStatus.Completed);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetApplicationStatusAsync(
                scenario.LocalApplicationId);

        Assert.Equal(
            AppStatus.Completed,
            result);
    }

    [Fact]
    public async Task GetApplicationStatusAsync_ShouldReturnNullWhenLocalApplicationDoesNotExist()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetApplicationStatusAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetApplicationStatusAsync_ShouldReturnNullForInvalidId()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetApplicationStatusAsync(0);

        Assert.Null(result);
    }

    // ============================================================
    // GetPassedTestTypeIdsAsync
    // ============================================================

    [Fact]
    public async Task GetPassedTestTypeIdsAsync_ShouldReturnOnlyDistinctPassedTestTypes()
    {
        var scenario =
            await SeedScenarioAsync();

        var theoryAppointmentId =
            await AddAppointmentAsync(
                scenario,
                TestTypeEnum.Theory,
                DateTime.UtcNow.AddDays(24));

        var writtenAppointmentId =
            await AddAppointmentAsync(
                scenario,
                TestTypeEnum.Written,
                DateTime.UtcNow.AddDays(25));

        var practicalAppointmentId =
            await AddAppointmentAsync(
                scenario,
                TestTypeEnum.Practical,
                DateTime.UtcNow.AddDays(26));

        var secondTheoryAppointmentId =
            await AddAppointmentAsync(
                scenario,
                TestTypeEnum.Theory,
                DateTime.UtcNow.AddDays(27));

        await AddTestResultAsync(
            theoryAppointmentId,
            scenario.UserId,
            true);

        await AddTestResultAsync(
            writtenAppointmentId,
            scenario.UserId,
            false);

        await AddTestResultAsync(
            practicalAppointmentId,
            scenario.UserId,
            true);

        await AddTestResultAsync(
            secondTheoryAppointmentId,
            scenario.UserId,
            true);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetPassedTestTypeIdsAsync(
                scenario.LocalApplicationId);

        Assert.Equal(
            2,
            result.Count);

        Assert.Contains(
            (int)TestTypeEnum.Theory,
            result);

        Assert.Contains(
            (int)TestTypeEnum.Practical,
            result);

        Assert.DoesNotContain(
            (int)TestTypeEnum.Written,
            result);
    }

    [Fact]
    public async Task GetPassedTestTypeIdsAsync_ShouldReturnEmptyWhenNoTestsHavePassed()
    {
        var scenario =
            await SeedScenarioAsync();

        var appointmentId =
            await AddAppointmentAsync(
                scenario,
                TestTypeEnum.Theory,
                DateTime.UtcNow.AddDays(28));

        await AddTestResultAsync(
            appointmentId,
            scenario.UserId,
            false);

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetPassedTestTypeIdsAsync(
                scenario.LocalApplicationId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPassedTestTypeIdsAsync_ShouldReturnEmptyForInvalidId()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetPassedTestTypeIdsAsync(0);

        Assert.Empty(result);
    }

    // ============================================================
    // GetTrialCountAsync
    // ============================================================

    [Fact]
    public async Task GetTrialCountAsync_ShouldReturnNumberOfAppointmentsForTestType()
    {
        var scenario =
            await SeedScenarioAsync();

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Theory,
            DateTime.UtcNow.AddDays(29));

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Theory,
            DateTime.UtcNow.AddDays(30));

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Theory,
            DateTime.UtcNow.AddDays(31));

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Written,
            DateTime.UtcNow.AddDays(32));

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetTrialCountAsync(
                scenario.LocalApplicationId,
                (int)TestTypeEnum.Theory);

        Assert.Equal(
            3,
            result);
    }

    [Fact]
    public async Task GetTrialCountAsync_ShouldReturnZeroWhenNoMatchingAppointmentsExist()
    {
        var scenario =
            await SeedScenarioAsync();

        await AddAppointmentAsync(
            scenario,
            TestTypeEnum.Written,
            DateTime.UtcNow.AddDays(33));

        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var result =
            await repository.GetTrialCountAsync(
                scenario.LocalApplicationId,
                (int)TestTypeEnum.Theory);

        Assert.Equal(
            0,
            result);
    }

    [Fact]
    public async Task GetTrialCountAsync_ShouldReturnZeroForInvalidArguments()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        var invalidLocalApplication =
            await repository.GetTrialCountAsync(
                0,
                (int)TestTypeEnum.Theory);

        var invalidTestType =
            await repository.GetTrialCountAsync(
                1,
                0);

        Assert.Equal(
            0,
            invalidLocalApplication);

        Assert.Equal(
            0,
            invalidTestType);
    }

    // ============================================================
    // AddAsync
    // ============================================================

    [Fact]
    public async Task AddAsync_ShouldPersistAppointment()
    {
        var scenario =
            await SeedScenarioAsync();

        var appointmentDate =
            DateTime.UtcNow.AddDays(34);

        var appointment =
            new TestAppointment
            {
                TestTypeID =
                    (int)TestTypeEnum.Theory,

                LocalDrivingLicenseApplicationID =
                    scenario.LocalApplicationId,

                AppointmentDate =
                    appointmentDate,

                PaidFees =
                    25m,

                CreatedByUserID =
                    scenario.UserId,

                IsLocked =
                    false,

                RetakeTestApplicationID =
                    null
            };

        await using (var context =
            _database.CreateContext())
        {
            var repository =
                new TestAppointmentRepository(context);

            await repository.AddAsync(
                appointment);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var persisted =
            await verificationContext
                .TestAppointments
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.TestAppointmentID ==
                        appointment.TestAppointmentID);

        Assert.NotNull(persisted);

        Assert.Equal(
            scenario.LocalApplicationId,
            persisted.LocalDrivingLicenseApplicationID);

        Assert.Equal(
            (int)TestTypeEnum.Theory,
            persisted.TestTypeID);

        Assert.Equal(
            appointmentDate,
            persisted.AppointmentDate);

        Assert.Equal(
            scenario.UserId,
            persisted.CreatedByUserID);

        Assert.Equal(
            25m,
            persisted.PaidFees);

        Assert.False(
            persisted.IsLocked);

        Assert.Null(
            persisted.RetakeTestApplicationID);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowWhenAppointmentIsNull()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                repository.AddAsync(null!));
    }

    // ============================================================
    // Delete
    // ============================================================

    [Fact]
    public async Task Delete_ShouldRemoveAppointmentAfterSaveChanges()
    {
        var scenario =
            await SeedScenarioAsync();

        var appointmentId =
            await AddAppointmentAsync(
                scenario,
                TestTypeEnum.Theory,
                DateTime.UtcNow.AddDays(35));

        await using (var context =
            _database.CreateContext())
        {
            var repository =
                new TestAppointmentRepository(context);

            var appointment =
                await repository.GetForUpdateAsync(
                    appointmentId);

            Assert.NotNull(appointment);

            repository.Delete(
                appointment);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var deleted =
            await verificationContext
                .TestAppointments
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.TestAppointmentID ==
                        appointmentId);

        Assert.Null(deleted);
    }

    [Fact]
    public void Delete_ShouldThrowWhenAppointmentIsNull()
    {
        using var context =
            _database.CreateContext();

        var repository =
            new TestAppointmentRepository(context);

        Assert.Throws<ArgumentNullException>(
            () =>
                repository.Delete(null!));
    }

    // ============================================================
    // Test Data Helpers
    // ============================================================

    private async Task<ScenarioSeed> SeedScenarioAsync(
        AppStatus applicationStatus =
            AppStatus.New)
    {
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

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    $"Integration Application Type {uniqueId[..8]}",

                ApplicationFees =
                    50m
            };

        var licenseClass =
            new LicenseClass
            {
                ClassName =
                    $"Integration Class {uniqueId[..8]}",

                ClassDescription =
                    "Integration test license class",

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
                    applicationStatus,

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
            _database.CreateContext();

        context.LocalDrivingLicenseApplications.Add(
            localApplication);

        await context.SaveChangesAsync();

        return new ScenarioSeed(
            PersonId:
                person.PersonId,

            UserId:
                user.UserId,

            ApplicationId:
                application.ApplicationID,

            LocalApplicationId:
                localApplication.LocalDrivingLicenseApplicationID,

            ApplicationTypeId:
                applicationType.ApplicationTypeId,

            LicenseClassId:
                licenseClass.LicenseClassID);
    }

    private async Task<int> AddAppointmentAsync(
        ScenarioSeed scenario,
        TestTypeEnum testType,
        DateTime appointmentDate,
        bool isLocked = false,
        decimal paidFees = 20m)
    {
        await using var context =
            _database.CreateContext();

        var appointment =
            new TestAppointment
            {
                TestTypeID =
                    (int)testType,

                LocalDrivingLicenseApplicationID =
                    scenario.LocalApplicationId,

                AppointmentDate =
                    appointmentDate,

                PaidFees =
                    paidFees,

                CreatedByUserID =
                    scenario.UserId,

                IsLocked =
                    isLocked,

                RetakeTestApplicationID =
                    null
            };

        context.TestAppointments.Add(
            appointment);

        await context.SaveChangesAsync();

        return appointment.TestAppointmentID;
    }

    private async Task AddTestResultAsync(
        int appointmentId,
        int userId,
        bool passed)
    {
        await using var context =
            _database.CreateContext();

        var test =
            new Test
            {
                TestAppointmentID =
                    appointmentId,

                TestResult =
                    passed,

                Notes =
                    passed
                        ? "Integration test passed"
                        : "Integration test failed",

                CreatedByUserID =
                    userId
            };

        context.Tests.Add(
            test);

        await context.SaveChangesAsync();
    }

    private sealed record ScenarioSeed(
        int PersonId,
        int UserId,
        int ApplicationId,
        int LocalApplicationId,
        int ApplicationTypeId,
        int LicenseClassId);
}