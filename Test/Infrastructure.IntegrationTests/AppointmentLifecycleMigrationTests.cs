using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.IntegrationTests;

public sealed class AppointmentLifecycleMigrationTests : IAsyncLifetime
{
    private readonly SqlServerTestDatabase _database = new();
    private string _createdDatabaseName = string.Empty;

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();

        var builder = new SqlConnectionStringBuilder(_database.ConnectionString);
        _createdDatabaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(_createdDatabaseName) ||
            !_createdDatabaseName.StartsWith("DVLD_IntegrationTests_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Safety violation: Target database '{_createdDatabaseName}' does not start with 'DVLD_IntegrationTests_'.");
        }

        await SeedRequiredMetadataAsync();
    }

    public async Task DisposeAsync()
    {
        SqlConnection.ClearAllPools();
        if (!string.IsNullOrWhiteSpace(_createdDatabaseName) &&
            _createdDatabaseName.StartsWith("DVLD_IntegrationTests_", StringComparison.OrdinalIgnoreCase))
        {
            await _database.DisposeAsync();
        }
    }

    private async Task SeedRequiredMetadataAsync()
    {
        await using var context = _database.CreateContext();

        var requiredTestTypes = new[]
        {
            new TestType { TestTypeId = (int)TestTypeEnum.Theory, TestTypeTitle = "Theory Test", TestTypeDescription = "Theory Exam", TestTypeFees = 10m },
            new TestType { TestTypeId = (int)TestTypeEnum.Written, TestTypeTitle = "Written Test", TestTypeDescription = "Written Exam", TestTypeFees = 15m },
            new TestType { TestTypeId = (int)TestTypeEnum.Practical, TestTypeTitle = "Practical Test", TestTypeDescription = "Practical Exam", TestTypeFees = 20m }
        };

        var existingTypeIds = await context.TestTypes.Select(t => t.TestTypeId).ToListAsync();
        var typesToAdd = requiredTestTypes.Where(t => !existingTypeIds.Contains(t.TestTypeId)).ToList();

        if (typesToAdd.Count > 0)
        {
            await context.Database.OpenConnectionAsync();
            try
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[TestTypes] ON;");
                context.TestTypes.AddRange(typesToAdd);
                await context.SaveChangesAsync();
            }
            finally
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[TestTypes] OFF;");
                await context.Database.CloseConnectionAsync();
            }
        }

        var requiredAppTypes = new[]
        {
            new ApplicationType { ApplicationTypeId = 1, ApplicationTypeTitle = "New Local Driving License Service", ApplicationFees = 15m },
            new ApplicationType { ApplicationTypeId = 7, ApplicationTypeTitle = "Retake Test", ApplicationFees = 5m }
        };

        var existingAppTypeIds = await context.ApplicationTypes.Select(a => a.ApplicationTypeId).ToListAsync();
        var appTypesToAdd = requiredAppTypes.Where(a => !existingAppTypeIds.Contains(a.ApplicationTypeId)).ToList();

        if (appTypesToAdd.Count > 0)
        {
            await context.Database.OpenConnectionAsync();
            try
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[ApplicationTypes] ON;");
                context.ApplicationTypes.AddRange(appTypesToAdd);
                await context.SaveChangesAsync();
            }
            finally
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[ApplicationTypes] OFF;");
                await context.Database.CloseConnectionAsync();
            }
        }

        if (!await context.LicenseClasses.AnyAsync())
        {
            context.LicenseClasses.Add(new LicenseClass
            {
                ClassName = "Class 1",
                ClassDescription = "Standard class",
                MinimumAllowedAge = 18,
                DefaultValidityLength = 10,
                ClassFees = 20m
            });
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Migration_AppliesSuccessfully_ToIsolatedSqlServerDatabase()
    {
        await using var context = _database.CreateContext();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        Assert.Empty(pendingMigrations);

        var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToList();
        Assert.Contains(appliedMigrations, m => m.Contains("AddAppointmentLifecycle"));
    }

    [Fact]
    public async Task Schema_ContainsNewLifecycleColumnsAndIndexes()
    {
        await using var context = _database.CreateContext();
        await using var connection = (SqlConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();

        // 1. Verify Columns in TestAppointments
        var columns = new Dictionary<string, (string DataType, string IsNullable)>();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'TestAppointments';";

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns[reader.GetString(0)] = (reader.GetString(1), reader.GetString(2));
            }
        }

        Assert.True(columns.ContainsKey("Status"));
        Assert.Equal("int", columns["Status"].DataType);
        Assert.Equal("YES", columns["Status"].IsNullable);

        Assert.True(columns.ContainsKey("CancelledAt"));
        Assert.Equal("datetime2", columns["CancelledAt"].DataType);
        Assert.Equal("YES", columns["CancelledAt"].IsNullable);

        Assert.True(columns.ContainsKey("CancelledByUserID"));
        Assert.Equal("int", columns["CancelledByUserID"].DataType);
        Assert.Equal("YES", columns["CancelledByUserID"].IsNullable);

        Assert.True(columns.ContainsKey("NoShowDate"));
        Assert.Equal("datetime2", columns["NoShowDate"].DataType);
        Assert.Equal("YES", columns["NoShowDate"].IsNullable);

        // 2. Verify Indexes
        var indexes = new HashSet<string>();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT i.name 
                FROM sys.indexes i
                JOIN sys.tables t ON i.object_id = t.object_id
                WHERE t.name = 'TestAppointments' AND i.name IS NOT NULL;";

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                indexes.Add(reader.GetString(0));
            }
        }

        Assert.Contains("IX_TestAppointments_Status", indexes);
        Assert.Contains("IX_TestAppointments_CancelledByUserID", indexes);
    }

    [Fact]
    public async Task ExistingData_DeterministicBackfill_CorrectlyAssignsScheduledAndCompleted()
    {
        await using var context = _database.CreateContext();

        var person = await SeedPersonAsync(context, "BF");

        var user = new User
        {
            PersonId = person.PersonId,
            UserName = $"u_{Guid.NewGuid():N}"[..20],
            Password = "Password123!",
            IsActive = true,
            Role = UserRole.Staff
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var app = new ApplicationD
        {
            ApplicantPersonID = person.PersonId,
            CreatedByUserID = user.UserId,
            ApplicationTypeID = 1,
            ApplicationDate = DateTime.UtcNow,
            ApplicationStatus = AppStatus.New,
            LastStatusDate = DateTime.UtcNow,
            PaidFees = 20m
        };
        context.Applications.Add(app);
        await context.SaveChangesAsync();

        var localApp = new LocalDrivingLicenseApplication
        {
            ApplicationID = app.ApplicationID,
            LicenseClassID = 1
        };
        context.LocalDrivingLicenseApplications.Add(localApp);
        await context.SaveChangesAsync();

        // 1. Appointment simulating legacy unlocked record without test
        var apptUnlocked = new TestAppointment
        {
            TestTypeID = 1,
            LocalDrivingLicenseApplicationID = localApp.LocalDrivingLicenseApplicationID,
            AppointmentDate = DateTime.UtcNow.AddDays(3),
            PaidFees = 10m,
            CreatedByUserID = user.UserId,
            IsLocked = false,
            Status = AppointmentStatus.Scheduled
        };
        context.TestAppointments.Add(apptUnlocked);

        // 2. Appointment simulating legacy locked record with completed test
        var apptLockedWithTest = new TestAppointment
        {
            TestTypeID = 1,
            LocalDrivingLicenseApplicationID = localApp.LocalDrivingLicenseApplicationID,
            AppointmentDate = DateTime.UtcNow.AddDays(-2),
            PaidFees = 10m,
            CreatedByUserID = user.UserId,
            IsLocked = true,
            Status = AppointmentStatus.Scheduled // initially default 1 before backfill logic
        };
        context.TestAppointments.Add(apptLockedWithTest);
        await context.SaveChangesAsync();

        var test = new Test
        {
            TestAppointmentID = apptLockedWithTest.TestAppointmentID,
            TestResult = true,
            Notes = "Passed",
            CreatedByUserID = user.UserId
        };
        context.Tests.Add(test);
        await context.SaveChangesAsync();

        // Act: Execute migration backfill SQL
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE TestAppointments
            SET Status = 2
            WHERE IsLocked = 1
              AND EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID);");

        // Assert: Verify state in fresh context
        await using var verifyContext = _database.CreateContext();
        var reloadedUnlocked = await verifyContext.TestAppointments.FindAsync(apptUnlocked.TestAppointmentID);
        var reloadedLocked = await verifyContext.TestAppointments.FindAsync(apptLockedWithTest.TestAppointmentID);

        Assert.NotNull(reloadedUnlocked);
        Assert.Equal(AppointmentStatus.Scheduled, reloadedUnlocked.Status);
        Assert.False(reloadedUnlocked.IsLocked);

        Assert.NotNull(reloadedLocked);
        Assert.Equal(AppointmentStatus.Completed, reloadedLocked.Status);
        Assert.True(reloadedLocked.IsLocked);
    }

    [Fact]
    public async Task InconsistentData_DeterministicBackfill_ResolvesContradictions_WithoutGuessingOrSilentCompletion()
    {
        await using var context = _database.CreateContext();

        var person = await SeedPersonAsync(context, "INCON");

        var user = new User
        {
            PersonId = person.PersonId,
            UserName = $"u_{Guid.NewGuid():N}"[..20],
            Password = "Password123!",
            IsActive = true,
            Role = UserRole.Staff
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var app = new ApplicationD
        {
            ApplicantPersonID = person.PersonId,
            CreatedByUserID = user.UserId,
            ApplicationTypeID = 1,
            ApplicationDate = DateTime.UtcNow,
            ApplicationStatus = AppStatus.New,
            LastStatusDate = DateTime.UtcNow,
            PaidFees = 20m
        };
        context.Applications.Add(app);
        await context.SaveChangesAsync();

        var localApp = new LocalDrivingLicenseApplication
        {
            ApplicationID = app.ApplicationID,
            LicenseClassID = 1
        };
        context.LocalDrivingLicenseApplications.Add(localApp);
        await context.SaveChangesAsync();

        var pastDate = new DateTime(2026, 10, 15, 9, 20, 0);

        // Case A: Inconsistent record: IsLocked = 1, but NO Test was ever recorded
        var apptLockedNoTest = new TestAppointment
        {
            TestTypeID = 1,
            LocalDrivingLicenseApplicationID = localApp.LocalDrivingLicenseApplicationID,
            AppointmentDate = pastDate,
            PaidFees = 10m,
            CreatedByUserID = user.UserId,
            IsLocked = true,
            Status = AppointmentStatus.Scheduled // initially 1
        };
        context.TestAppointments.Add(apptLockedNoTest);

        // Case B: Inconsistent record: IsLocked = 0, but a Test record exists
        var apptUnlockedWithTest = new TestAppointment
        {
            TestTypeID = 1,
            LocalDrivingLicenseApplicationID = localApp.LocalDrivingLicenseApplicationID,
            AppointmentDate = pastDate.AddDays(1),
            PaidFees = 10m,
            CreatedByUserID = user.UserId,
            IsLocked = false,
            Status = AppointmentStatus.Scheduled // initially 1
        };
        context.TestAppointments.Add(apptUnlockedWithTest);
        await context.SaveChangesAsync();

        var genuineTest = new Test
        {
            TestAppointmentID = apptUnlockedWithTest.TestAppointmentID,
            TestResult = false,
            Notes = "Failed",
            CreatedByUserID = user.UserId
        };
        context.Tests.Add(genuineTest);
        await context.SaveChangesAsync();

        // Act: Execute migration backfill rules
        // Rule 1: Consistent Completed
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE TestAppointments
            SET Status = 2
            WHERE IsLocked = 1
              AND EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID);");

        // Rule 2: Inconsistent Locked without Test -> Must NOT be Completed, must NOT be Scheduled. Becomes NoShow (3) when expired (>40m).
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE TestAppointments
            SET Status = 3,
                NoShowDate = DATEADD(minute, 40, AppointmentDate)
            WHERE IsLocked = 1
              AND NOT EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID)
              AND DATEADD(minute, 40, AppointmentDate) <= CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'Jordan Standard Time' AS datetime2);");

        // Rule 3: Inconsistent Unlocked with Test -> Has genuine Test, must be Completed (2) and IsLocked = 1.
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE TestAppointments
            SET Status = 2,
                IsLocked = 1
            WHERE IsLocked = 0
              AND EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID);");

        // Assert: Verify state in fresh context
        await using var verifyContext = _database.CreateContext();

        var reloadedCaseA = await verifyContext.TestAppointments
            .Include(x => x.Test)
            .FirstOrDefaultAsync(x => x.TestAppointmentID == apptLockedNoTest.TestAppointmentID);

        Assert.NotNull(reloadedCaseA);
        // Case A MUST NOT be Completed (2) and MUST NOT be Scheduled (1)
        Assert.Equal(AppointmentStatus.NoShow, reloadedCaseA.Status);
        Assert.True(reloadedCaseA.IsLocked); // Consistent with NoShow
        Assert.Null(reloadedCaseA.Test);      // No fake test was created
        Assert.NotNull(reloadedCaseA.NoShowDate);
        Assert.Equal(pastDate.AddMinutes(40), reloadedCaseA.NoShowDate.Value);

        var reloadedCaseB = await verifyContext.TestAppointments
            .Include(x => x.Test)
            .FirstOrDefaultAsync(x => x.TestAppointmentID == apptUnlockedWithTest.TestAppointmentID);

        Assert.NotNull(reloadedCaseB);
        Assert.Equal(AppointmentStatus.Completed, reloadedCaseB.Status);
        Assert.True(reloadedCaseB.IsLocked); // Locking contradiction resolved
        Assert.NotNull(reloadedCaseB.Test);
        Assert.False(reloadedCaseB.Test.TestResult);
    }

    [Fact]
    public async Task TemporalSafetyBackfill_HandlesAllFourScenarios_WithoutGuessingOrSilentMisclassification()
    {
        await using var context = _database.CreateContext();

        var person = await SeedPersonAsync(context, "TEMP");

        var user = new User
        {
            PersonId = person.PersonId,
            UserName = $"u_{Guid.NewGuid():N}"[..20],
            Password = "Password123!",
            IsActive = true,
            Role = UserRole.Staff
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var app = new ApplicationD
        {
            ApplicantPersonID = person.PersonId,
            CreatedByUserID = user.UserId,
            ApplicationTypeID = 1,
            ApplicationDate = DateTime.UtcNow,
            ApplicationStatus = AppStatus.New,
            LastStatusDate = DateTime.UtcNow,
            PaidFees = 20m
        };
        context.Applications.Add(app);
        await context.SaveChangesAsync();

        var localApp = new LocalDrivingLicenseApplication
        {
            ApplicationID = app.ApplicationID,
            LicenseClassID = 1
        };
        context.LocalDrivingLicenseApplications.Add(localApp);
        await context.SaveChangesAsync();

        // Establish facility time in Jordan
        var jordanNow = Application.Validators.TestAppointmentValidator.GetCurrentJordanTime();

        // 1. Locked appointment without Test, with appointment time more than 40 minutes in the past (e.g. 2 hours ago)
        var pastExpiredDate = jordanNow.AddHours(-2);
        var apptPastExpired = new TestAppointment
        {
            TestTypeID = 1,
            LocalDrivingLicenseApplicationID = localApp.LocalDrivingLicenseApplicationID,
            AppointmentDate = pastExpiredDate,
            PaidFees = 10m,
            CreatedByUserID = user.UserId,
            IsLocked = true,
            Status = null
        };
        context.TestAppointments.Add(apptPastExpired);

        // 2. Locked appointment without Test, with appointment time in the future (e.g. +2 days)
        var futureDate = jordanNow.AddDays(2);
        var apptFutureLocked = new TestAppointment
        {
            TestTypeID = 1,
            LocalDrivingLicenseApplicationID = localApp.LocalDrivingLicenseApplicationID,
            AppointmentDate = futureDate,
            PaidFees = 10m,
            CreatedByUserID = user.UserId,
            IsLocked = true,
            Status = null
        };
        context.TestAppointments.Add(apptFutureLocked);

        // 3. Locked appointment without Test, with less than 40 minutes elapsed (e.g. 15 minutes ago, slot not concluded)
        var ongoingDate = jordanNow.AddMinutes(-15);
        var apptOngoingLocked = new TestAppointment
        {
            TestTypeID = 1,
            LocalDrivingLicenseApplicationID = localApp.LocalDrivingLicenseApplicationID,
            AppointmentDate = ongoingDate,
            PaidFees = 10m,
            CreatedByUserID = user.UserId,
            IsLocked = true,
            Status = null
        };
        context.TestAppointments.Add(apptOngoingLocked);

        // 4. Unlocked appointment with Test
        var apptUnlockedWithTest = new TestAppointment
        {
            TestTypeID = 1,
            LocalDrivingLicenseApplicationID = localApp.LocalDrivingLicenseApplicationID,
            AppointmentDate = pastExpiredDate.AddDays(-1),
            PaidFees = 10m,
            CreatedByUserID = user.UserId,
            IsLocked = false,
            Status = null
        };
        context.TestAppointments.Add(apptUnlockedWithTest);
        await context.SaveChangesAsync();

        var testForUnlocked = new Test
        {
            TestAppointmentID = apptUnlockedWithTest.TestAppointmentID,
            TestResult = true,
            Notes = "Passed",
            CreatedByUserID = user.UserId
        };
        context.Tests.Add(testForUnlocked);
        await context.SaveChangesAsync();

        // Simulate legacy unmigrated state where Status column was newly added as NULL for all records
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE TestAppointments SET Status = NULL WHERE TestAppointmentID IN ({0}, {1}, {2}, {3});",
            apptPastExpired.TestAppointmentID,
            apptFutureLocked.TestAppointmentID,
            apptOngoingLocked.TestAppointmentID,
            apptUnlockedWithTest.TestAppointmentID);

        // Act: Execute migration backfill SQL rules exactly as defined in 20261102232837_AddAppointmentLifecycle:
        // Rule 1: Consistent Scheduled
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE TestAppointments
            SET Status = 1
            WHERE IsLocked = 0
              AND NOT EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID);");

        // Rule 2: Consistent Completed
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE TestAppointments
            SET Status = 2
            WHERE IsLocked = 1
              AND EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID);");

        // Rule 3: Inconsistent Unlocked with Test -> Becomes Completed (2) and IsLocked = 1
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE TestAppointments
            SET Status = 2,
                IsLocked = 1
            WHERE IsLocked = 0
              AND EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID);");

        // Rule 4: Inconsistent Locked without Test, Expired (> 40 minutes in past in Jordan time) -> Becomes NoShow (3)
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE TestAppointments
            SET Status = 3,
                NoShowDate = DATEADD(minute, 40, AppointmentDate)
            WHERE IsLocked = 1
              AND NOT EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID)
              AND DATEADD(minute, 40, AppointmentDate) <= CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'Jordan Standard Time' AS datetime2);");

        // Assert: Reload in fresh context
        await using var verifyContext = _database.CreateContext();

        // 1. Past expired: Must be NoShow (3) with NoShowDate = pastExpiredDate + 40m, IsLocked = true, no test
        var reloadedPastExpired = await verifyContext.TestAppointments
            .Include(x => x.Test)
            .FirstOrDefaultAsync(x => x.TestAppointmentID == apptPastExpired.TestAppointmentID);
        Assert.NotNull(reloadedPastExpired);
        Assert.Equal(AppointmentStatus.NoShow, reloadedPastExpired.Status);
        Assert.True(reloadedPastExpired.IsLocked);
        Assert.Null(reloadedPastExpired.Test);
        Assert.NotNull(reloadedPastExpired.NoShowDate);
        Assert.Equal(pastExpiredDate.AddMinutes(40), reloadedPastExpired.NoShowDate.Value);

        // 2. Future locked: Must NOT be NoShow, must NOT be Completed, must NOT be Scheduled, Status remains NULL (unresolved ambiguity preserved)
        var reloadedFutureLocked = await verifyContext.TestAppointments
            .Include(x => x.Test)
            .FirstOrDefaultAsync(x => x.TestAppointmentID == apptFutureLocked.TestAppointmentID);
        Assert.NotNull(reloadedFutureLocked);
        Assert.NotEqual(AppointmentStatus.NoShow, reloadedFutureLocked.Status);
        Assert.NotEqual(AppointmentStatus.Completed, reloadedFutureLocked.Status);
        Assert.NotEqual(AppointmentStatus.Scheduled, reloadedFutureLocked.Status);
        Assert.Null(reloadedFutureLocked.Status);
        Assert.True(reloadedFutureLocked.IsLocked);
        Assert.Null(reloadedFutureLocked.NoShowDate);
        Assert.Null(reloadedFutureLocked.Test);

        // 3. Ongoing/Recent (< 40m elapsed): Must NOT be NoShow, must NOT be Completed, must NOT be Scheduled, Status remains NULL
        var reloadedOngoingLocked = await verifyContext.TestAppointments
            .Include(x => x.Test)
            .FirstOrDefaultAsync(x => x.TestAppointmentID == apptOngoingLocked.TestAppointmentID);
        Assert.NotNull(reloadedOngoingLocked);
        Assert.NotEqual(AppointmentStatus.NoShow, reloadedOngoingLocked.Status);
        Assert.NotEqual(AppointmentStatus.Completed, reloadedOngoingLocked.Status);
        Assert.NotEqual(AppointmentStatus.Scheduled, reloadedOngoingLocked.Status);
        Assert.Null(reloadedOngoingLocked.Status);
        Assert.True(reloadedOngoingLocked.IsLocked);
        Assert.Null(reloadedOngoingLocked.NoShowDate);
        Assert.Null(reloadedOngoingLocked.Test);

        // 4. Unlocked with Test: Must be Completed (2) and IsLocked = true
        var reloadedUnlockedWithTest = await verifyContext.TestAppointments
            .Include(x => x.Test)
            .FirstOrDefaultAsync(x => x.TestAppointmentID == apptUnlockedWithTest.TestAppointmentID);
        Assert.NotNull(reloadedUnlockedWithTest);
        Assert.Equal(AppointmentStatus.Completed, reloadedUnlockedWithTest.Status);
        Assert.True(reloadedUnlockedWithTest.IsLocked);
        Assert.NotNull(reloadedUnlockedWithTest.Test);
    }

    [Fact]
    public async Task CancelledByUser_ForeignKey_RestrictsUserDeletion()
    {
        await using var context = _database.CreateContext();

        var person = await SeedPersonAsync(context, "CA");

        var staffUser = new User
        {
            PersonId = person.PersonId,
            UserName = $"st_{Guid.NewGuid():N}"[..20],
            Password = "Password123!",
            IsActive = true,
            Role = UserRole.Staff
        };
        context.Users.Add(staffUser);
        await context.SaveChangesAsync();

        var app = new ApplicationD
        {
            ApplicantPersonID = person.PersonId,
            CreatedByUserID = staffUser.UserId,
            ApplicationTypeID = 1,
            ApplicationDate = DateTime.UtcNow,
            ApplicationStatus = AppStatus.New,
            LastStatusDate = DateTime.UtcNow,
            PaidFees = 20m
        };
        context.Applications.Add(app);
        await context.SaveChangesAsync();

        var localApp = new LocalDrivingLicenseApplication
        {
            ApplicationID = app.ApplicationID,
            LicenseClassID = 1
        };
        context.LocalDrivingLicenseApplications.Add(localApp);
        await context.SaveChangesAsync();

        var cancelledAppt = new TestAppointment
        {
            TestTypeID = 1,
            LocalDrivingLicenseApplicationID = localApp.LocalDrivingLicenseApplicationID,
            AppointmentDate = DateTime.UtcNow.AddDays(5),
            PaidFees = 10m,
            CreatedByUserID = staffUser.UserId,
            IsLocked = true,
            Status = AppointmentStatus.Cancelled,
            CancelledAt = DateTime.UtcNow,
            CancelledByUserID = staffUser.UserId
        };
        context.TestAppointments.Add(cancelledAppt);
        await context.SaveChangesAsync();

        // Act & Assert: Attempting to delete staffUser must fail because of Restrict delete behavior
        await using var deleteContext = _database.CreateContext();
        var userToDelete = await deleteContext.Users.FindAsync(staffUser.UserId);
        Assert.NotNull(userToDelete);
        deleteContext.Users.Remove(userToDelete);

        await Assert.ThrowsAsync<DbUpdateException>(() => deleteContext.SaveChangesAsync());
    }

    [Fact]
    public async Task NoShow_CanBePersisted_WithoutCreatingFakeTestRecord()
    {
        await using var context = _database.CreateContext();

        var person = await SeedPersonAsync(context, "NS");

        var user = new User
        {
            PersonId = person.PersonId,
            UserName = $"ns_{Guid.NewGuid():N}"[..20],
            Password = "Password123!",
            IsActive = true,
            Role = UserRole.Staff
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var app = new ApplicationD
        {
            ApplicantPersonID = person.PersonId,
            CreatedByUserID = user.UserId,
            ApplicationTypeID = 1,
            ApplicationDate = DateTime.UtcNow,
            ApplicationStatus = AppStatus.New,
            LastStatusDate = DateTime.UtcNow,
            PaidFees = 20m
        };
        context.Applications.Add(app);
        await context.SaveChangesAsync();

        var localApp = new LocalDrivingLicenseApplication
        {
            ApplicationID = app.ApplicationID,
            LicenseClassID = 1
        };
        context.LocalDrivingLicenseApplications.Add(localApp);
        await context.SaveChangesAsync();

        var noShowDate = DateTime.UtcNow.AddHours(-1);
        var noShowAppt = new TestAppointment
        {
            TestTypeID = 1,
            LocalDrivingLicenseApplicationID = localApp.LocalDrivingLicenseApplicationID,
            AppointmentDate = DateTime.UtcNow.AddHours(-2),
            PaidFees = 10m,
            CreatedByUserID = user.UserId,
            IsLocked = true,
            Status = AppointmentStatus.NoShow,
            NoShowDate = noShowDate
        };
        context.TestAppointments.Add(noShowAppt);
        await context.SaveChangesAsync();

        // Act & Assert: Reload from DB and verify
        await using var verifyContext = _database.CreateContext();
        var reloaded = await verifyContext.TestAppointments
            .Include(x => x.Test)
            .FirstOrDefaultAsync(x => x.TestAppointmentID == noShowAppt.TestAppointmentID);

        Assert.NotNull(reloaded);
        Assert.Equal(AppointmentStatus.NoShow, reloaded.Status);
        Assert.True(reloaded.IsLocked);
        Assert.Equal(noShowDate.Date, reloaded.NoShowDate!.Value.Date);
        Assert.Null(reloaded.Test); // Absolutely NO fake test record
    }

    private static async Task<Person> SeedPersonAsync(DVLDDbContext context, string prefix)
    {
        var uid = Guid.NewGuid().ToString("N");
        var country = new Country { CountryName = $"Country_{uid[..8]}" };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var person = new Person
        {
            NationalNo = $"N_{prefix}_{uid}"[..20],
            FirstName = prefix,
            SecondName = "Test",
            LastName = "User",
            DateOfBirth = new DateTime(1995, 1, 1),
            Gender = Gender.Male,
            NationalityCountryID = country.CountryId,
            Address = "Amman, Jordan",
            Phone = "0790000000"
        };
        context.People.Add(person);
        await context.SaveChangesAsync();
        return person;
    }
}
