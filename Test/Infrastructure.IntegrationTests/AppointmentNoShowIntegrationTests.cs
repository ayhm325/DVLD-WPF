using Application.Interfaces;
using Application.Options;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Data;
using Xunit;


namespace Infrastructure.IntegrationTests;

public sealed class AppointmentNoShowIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerTestDatabase _database = new();
    private string _createdDatabaseName = string.Empty;

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();

        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_database.ConnectionString);
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
    }

    private sealed record TestApplicantSeed(int PersonId, int UserId, int ApplicationId, int LocalAppId);

    private async Task<TestApplicantSeed> SeedApplicantAsync(string nationalNoPrefix)
    {
        await using var context = _database.CreateContext();
        var uid = Guid.NewGuid().ToString("N");

        var country = await context.Countries.FirstOrDefaultAsync() ?? new Country { CountryName = "Jordan" };
        if (country.CountryId == 0)
        {
            context.Countries.Add(country);
            await context.SaveChangesAsync();
        }

        var person = new Person
        {
            NationalNo = $"N_{uid[..12]}",
            FirstName = "Ali",
            SecondName = "Hassan",
            LastName = "Obeidat",
            DateOfBirth = new DateTime(1995, 1, 1),
            Gender = 0,
            Address = "Amman, Jordan",
            Phone = "0790000000",
            NationalityCountryID = country.CountryId
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var user = new User
        {
            PersonId = person.PersonId,
            UserName = $"u_{uid[..10]}",
            Password = "Password123!",
            IsActive = true,
            Role = UserRole.Staff
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var licenseClass = new LicenseClass
        {
            ClassName = $"Class_{uid[..6]}",
            ClassDescription = "Standard class",
            MinimumAllowedAge = 18,
            DefaultValidityLength = 10,
            ClassFees = 20m
        };
        context.LicenseClasses.Add(licenseClass);
        await context.SaveChangesAsync();

        var application = new ApplicationD
        {
            ApplicantPersonID = person.PersonId,
            CreatedByUserID = user.UserId,
            ApplicationTypeID = 1,
            ApplicationDate = DateTime.UtcNow,
            ApplicationStatus = AppStatus.New,
            LastStatusDate = DateTime.UtcNow,
            PaidFees = 15m
        };
        context.Applications.Add(application);
        await context.SaveChangesAsync();

        var localApp = new LocalDrivingLicenseApplication
        {
            ApplicationID = application.ApplicationID,
            LicenseClassID = licenseClass.LicenseClassID
        };
        context.LocalDrivingLicenseApplications.Add(localApp);
        await context.SaveChangesAsync();

        return new TestApplicantSeed(
            person.PersonId,
            user.UserId,
            application.ApplicationID,
            localApp.LocalDrivingLicenseApplicationID);
    }

    private async Task<TestAppointment> SeedAppointmentAsync(
        int localAppId,
        int userId,
        DateTime appointmentDate,
        AppointmentStatus? status = AppointmentStatus.Scheduled,
        bool isLocked = false,
        int testTypeId = (int)TestTypeEnum.Theory)
    {
        await using var context = _database.CreateContext();
        var appointment = new TestAppointment
        {
            TestTypeID = testTypeId,
            LocalDrivingLicenseApplicationID = localAppId,
            AppointmentDate = appointmentDate,
            PaidFees = 10m,
            CreatedByUserID = userId,
            IsLocked = isLocked,
            Status = status,
            RetakeTestApplicationID = null
        };
        context.TestAppointments.Add(appointment);
        await context.SaveChangesAsync();
        return appointment;
    }

    private sealed class TestJordanClock(DateTime initialTime) : IJordanClock
    {
        public DateTime CurrentTime { get; set; } = initialTime;
        public DateTime Now => CurrentTime;
    }

    private (NoShowProcessorService Service, TestJordanClock Clock) CreateProcessor(
        DateTime initialTime,
        int batchSize = 50,
        int gracePeriodMinutes = 40)
    {
        var context = _database.CreateContext();
        var uow = new UnitOfWork(context);
        var repo = new TestAppointmentRepository(context);
        var testRepo = new TestRepository(context);
        var clock = new TestJordanClock(initialTime);
        var options = Options.Create(new NoShowProcessorOptions
        {
            IsEnabled = true,
            BatchSize = batchSize,
            GracePeriodMinutes = gracePeriodMinutes,
            LockTimeoutMs = 5000,
            PollingIntervalSeconds = 60
        });

        var service = new NoShowProcessorService(
            uow,
            repo,
            testRepo,
            clock,
            options,
            NullLogger<NoShowProcessorService>.Instance);

        return (service, clock);
    }

    // 1. Expired Scheduled appointment becomes NoShow
    [Fact]
    public async Task ProcessPendingNoShows_ExpiredScheduledAppointment_BecomesNoShow()
    {
        var applicant = await SeedApplicantAsync("EXPIRED");
        var baseTime = new DateTime(2026, 11, 10, 10, 0, 0); // 10:00
        var appointment = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime);

        // Process at 10:41 (41 minutes later, > 40 minutes)
        var processTime = baseTime.AddMinutes(41);
        var (processor, _) = CreateProcessor(processTime);

        var result = await processor.ProcessPendingNoShowsAsync();

        Assert.Equal(1, result.TotalCandidatesFound);
        Assert.Equal(1, result.ProcessedCount);

        await using var verifyContext = _database.CreateContext();
        var updated = await verifyContext.TestAppointments.FindAsync(appointment.TestAppointmentID);
        Assert.NotNull(updated);
        Assert.Equal(AppointmentStatus.NoShow, updated.Status);
        Assert.True(updated.IsLocked);
        Assert.Equal(processTime, updated.NoShowDate);
    }

    // 2. Appointment before 40-minute boundary remains Scheduled
    [Fact]
    public async Task ProcessPendingNoShows_AppointmentBefore40MinuteBoundary_RemainsScheduled()
    {
        var applicant = await SeedApplicantAsync("BEFORE40");
        var baseTime = new DateTime(2026, 11, 10, 10, 0, 0); // 10:00
        var appointment = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime);

        // Process at 10:39 (39 minutes later, < 40 minutes)
        var processTime = baseTime.AddMinutes(39);
        var (processor, _) = CreateProcessor(processTime);

        var result = await processor.ProcessPendingNoShowsAsync();

        Assert.Equal(0, result.TotalCandidatesFound);
        Assert.Equal(0, result.ProcessedCount);

        await using var verifyContext = _database.CreateContext();
        var updated = await verifyContext.TestAppointments.FindAsync(appointment.TestAppointmentID);
        Assert.NotNull(updated);
        Assert.Equal(AppointmentStatus.Scheduled, updated.Status);
        Assert.False(updated.IsLocked);
        Assert.Null(updated.NoShowDate);
    }

    // 3. Appointment exactly at 40-minute boundary becomes NoShow
    [Fact]
    public async Task ProcessPendingNoShows_AppointmentExactlyAt40MinuteBoundary_BecomesNoShow()
    {
        var applicant = await SeedApplicantAsync("EXACT40");
        var baseTime = new DateTime(2026, 11, 10, 10, 0, 0); // 10:00
        var appointment = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime);

        // Process at 10:40 (exactly 40 minutes later)
        var processTime = baseTime.AddMinutes(40);
        var (processor, _) = CreateProcessor(processTime);

        var result = await processor.ProcessPendingNoShowsAsync();

        Assert.Equal(1, result.TotalCandidatesFound);
        Assert.Equal(1, result.ProcessedCount);

        await using var verifyContext = _database.CreateContext();
        var updated = await verifyContext.TestAppointments.FindAsync(appointment.TestAppointmentID);
        Assert.NotNull(updated);
        Assert.Equal(AppointmentStatus.NoShow, updated.Status);
        Assert.True(updated.IsLocked);
        Assert.Equal(processTime, updated.NoShowDate);
    }

    // 4. Appointment with an existing Test is not processed
    [Fact]
    public async Task ProcessPendingNoShows_AppointmentWithExistingTest_IsNotProcessed()
    {
        var applicant = await SeedApplicantAsync("HASTEST");
        var baseTime = new DateTime(2026, 11, 10, 10, 0, 0);
        var appointment = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime);

        // Create a Test record for this appointment
        await using (var testContext = _database.CreateContext())
        {
            testContext.Tests.Add(new Test
            {
                TestAppointmentID = appointment.TestAppointmentID,
                TestResult = true,
                Notes = "Passed exam",
                CreatedByUserID = applicant.UserId
            });
            await testContext.SaveChangesAsync();
        }

        var processTime = baseTime.AddMinutes(60);
        var (processor, _) = CreateProcessor(processTime);

        var result = await processor.ProcessPendingNoShowsAsync();

        Assert.Equal(0, result.TotalCandidatesFound);
        Assert.Equal(0, result.ProcessedCount);

        await using var verifyContext = _database.CreateContext();
        var updated = await verifyContext.TestAppointments.FindAsync(appointment.TestAppointmentID);
        Assert.NotNull(updated);
        Assert.Equal(AppointmentStatus.Scheduled, updated.Status);
    }

    // 5. Cancelled appointment is not processed
    [Fact]
    public async Task ProcessPendingNoShows_CancelledAppointment_IsNotProcessed()
    {
        var applicant = await SeedApplicantAsync("CANCELLED");
        var baseTime = new DateTime(2026, 11, 10, 10, 0, 0);
        var appointment = await SeedAppointmentAsync(
            applicant.LocalAppId, applicant.UserId, baseTime,
            status: AppointmentStatus.Cancelled, isLocked: true);

        var processTime = baseTime.AddMinutes(60);
        var (processor, _) = CreateProcessor(processTime);

        var result = await processor.ProcessPendingNoShowsAsync();

        Assert.Equal(0, result.TotalCandidatesFound);
        Assert.Equal(0, result.ProcessedCount);

        await using var verifyContext = _database.CreateContext();
        var updated = await verifyContext.TestAppointments.FindAsync(appointment.TestAppointmentID);
        Assert.NotNull(updated);
        Assert.Equal(AppointmentStatus.Cancelled, updated.Status);
        Assert.Null(updated.NoShowDate);
    }

    // 6. Completed appointment is not processed
    [Fact]
    public async Task ProcessPendingNoShows_CompletedAppointment_IsNotProcessed()
    {
        var applicant = await SeedApplicantAsync("COMPLETED");
        var baseTime = new DateTime(2026, 11, 10, 10, 0, 0);
        var appointment = await SeedAppointmentAsync(
            applicant.LocalAppId, applicant.UserId, baseTime,
            status: AppointmentStatus.Completed, isLocked: true);

        var processTime = baseTime.AddMinutes(60);
        var (processor, _) = CreateProcessor(processTime);

        var result = await processor.ProcessPendingNoShowsAsync();

        Assert.Equal(0, result.TotalCandidatesFound);
        Assert.Equal(0, result.ProcessedCount);

        await using var verifyContext = _database.CreateContext();
        var updated = await verifyContext.TestAppointments.FindAsync(appointment.TestAppointmentID);
        Assert.NotNull(updated);
        Assert.Equal(AppointmentStatus.Completed, updated.Status);
        Assert.Null(updated.NoShowDate);
    }

    // 7. NULL-status appointment is not processed
    [Fact]
    public async Task ProcessPendingNoShows_NullStatusAppointment_IsNotProcessed()
    {
        var applicant = await SeedApplicantAsync("NULLSTATUS");
        var baseTime = new DateTime(2026, 11, 10, 10, 0, 0);
        var appointment = await SeedAppointmentAsync(
            applicant.LocalAppId, applicant.UserId, baseTime,
            status: AppointmentStatus.Scheduled, isLocked: false);

        // Force Status = NULL via raw SQL
        await using (var rawContext = _database.CreateContext())
        {
            await rawContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE TestAppointments SET Status = NULL WHERE TestAppointmentID = {appointment.TestAppointmentID}");
        }

        var processTime = baseTime.AddMinutes(60);
        var (processor, _) = CreateProcessor(processTime);

        var result = await processor.ProcessPendingNoShowsAsync();

        Assert.Equal(0, result.TotalCandidatesFound);
        Assert.Equal(0, result.ProcessedCount);

        await using var verifyContext = _database.CreateContext();
        var updated = await verifyContext.TestAppointments.FindAsync(appointment.TestAppointmentID);
        Assert.NotNull(updated);
        Assert.Null(updated.Status);
        Assert.Null(updated.NoShowDate);
    }

    // 8. Repeated worker execution is idempotent
    [Fact]
    public async Task ProcessPendingNoShows_RepeatedWorkerExecution_IsIdempotent()
    {
        var applicant = await SeedApplicantAsync("IDEMPOTENT");
        var baseTime = new DateTime(2026, 11, 10, 10, 0, 0);
        var appointment = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime);

        var processTime = baseTime.AddMinutes(50);
        var (processor, clock) = CreateProcessor(processTime);

        // Run 1
        var result1 = await processor.ProcessPendingNoShowsAsync();
        Assert.Equal(1, result1.ProcessedCount);

        // Advance clock by 10 minutes and Run 2
        clock.CurrentTime = processTime.AddMinutes(10);
        var result2 = await processor.ProcessPendingNoShowsAsync();
        Assert.Equal(0, result2.TotalCandidatesFound);
        Assert.Equal(0, result2.ProcessedCount);

        // Verify NoShowDate was preserved from Run 1
        await using var verifyContext = _database.CreateContext();
        var updated = await verifyContext.TestAppointments.FindAsync(appointment.TestAppointmentID);
        Assert.NotNull(updated);
        Assert.Equal(AppointmentStatus.NoShow, updated.Status);
        Assert.Equal(processTime, updated.NoShowDate);
    }

    // 9. Concurrent workers do not process an appointment twice
    [Fact]
    public async Task ProcessPendingNoShows_ConcurrentWorkers_DoNotProcessAppointmentTwice()
    {
        var applicant = await SeedApplicantAsync("CONCURRENCY");
        var baseTime = new DateTime(2026, 11, 10, 10, 0, 0);
        var appointment = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime);

        var processTime = baseTime.AddMinutes(45);

        // Instantiate two separate processors with independent contexts and connections
        var (processor1, _) = CreateProcessor(processTime);
        var (processor2, _) = CreateProcessor(processTime);

        var task1 = Task.Run(() => processor1.ProcessPendingNoShowsAsync());
        var task2 = Task.Run(() => processor2.ProcessPendingNoShowsAsync());

        var results = await Task.WhenAll(task1, task2);

        var totalProcessed = results[0].ProcessedCount + results[1].ProcessedCount;
        Assert.Equal(1, totalProcessed);

        await using var verifyContext = _database.CreateContext();
        var updated = await verifyContext.TestAppointments.FindAsync(appointment.TestAppointmentID);
        Assert.NotNull(updated);
        Assert.Equal(AppointmentStatus.NoShow, updated.Status);
        Assert.True(updated.IsLocked);
    }

    // 10. No fake Test record is created
    [Fact]
    public async Task ProcessPendingNoShows_NoFakeTestRecordIsCreated()
    {
        var applicant = await SeedApplicantAsync("NOTESTCREATED");
        var baseTime = new DateTime(2026, 11, 10, 10, 0, 0);
        var appointment = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime);

        var processTime = baseTime.AddMinutes(50);
        var (processor, _) = CreateProcessor(processTime);

        await processor.ProcessPendingNoShowsAsync();

        await using var verifyContext = _database.CreateContext();
        var testCount = await verifyContext.Tests
            .CountAsync(t => t.TestAppointmentID == appointment.TestAppointmentID);

        Assert.Equal(0, testCount);
    }

    // 11. NoShow is counted as an attempt
    [Fact]
    public async Task ProcessPendingNoShows_NoShowIsCountedAsAttempt()
    {
        var applicant = await SeedApplicantAsync("TRIALCOUNT");
        var baseTime = new DateTime(2026, 11, 10, 10, 0, 0);
        var appointment = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime);

        var processTime = baseTime.AddMinutes(50);
        var (processor, _) = CreateProcessor(processTime);
        await processor.ProcessPendingNoShowsAsync();

        await using var verifyContext = _database.CreateContext();
        var repo = new TestAppointmentRepository(verifyContext);
        var trialCount = await repo.GetTrialCountAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory);

        Assert.Equal(1, trialCount);
    }

    // 12. NoShow releases the occupied slot
    [Fact]
    public async Task ProcessPendingNoShows_NoShowReleasesOccupiedSlot()
    {
        var applicant = await SeedApplicantAsync("RELEASESLOT");
        var baseTime = new DateTime(2026, 11, 10, 10, 0, 0);
        var appointment = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime);

        // Before NoShow processing, slot is occupied
        await using (var preContext = _database.CreateContext())
        {
            var preRepo = new TestAppointmentRepository(preContext);
            var isOccupiedBefore = await preRepo.IsSlotOccupiedAsync((int)TestTypeEnum.Theory, baseTime);
            Assert.True(isOccupiedBefore);
        }

        // Process NoShow
        var processTime = baseTime.AddMinutes(50);
        var (processor, _) = CreateProcessor(processTime);
        await processor.ProcessPendingNoShowsAsync();

        // After NoShow processing, slot is released
        await using (var postContext = _database.CreateContext())
        {
            var postRepo = new TestAppointmentRepository(postContext);
            var isOccupiedAfter = await postRepo.IsSlotOccupiedAsync((int)TestTypeEnum.Theory, baseTime);
            Assert.False(isOccupiedAfter);
        }
    }

    // 13. Applicant same-day conflict remains enforced
    [Fact]
    public async Task ProcessPendingNoShows_ApplicantSameDayConflictRemainsEnforced()
    {
        var applicant = await SeedApplicantAsync("SAMEDAYCONFLICT");
        var baseTime = new DateTime(2026, 11, 10, 8, 0, 0); // 08:00
        var appointment = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime);

        // Process NoShow at 08:50
        var processTime = baseTime.AddMinutes(50);
        var (processor, _) = CreateProcessor(processTime);
        await processor.ProcessPendingNoShowsAsync();

        // Check same-day conflict for applicant on that day (e.g. at 12:00)
        await using var verifyContext = _database.CreateContext();
        var repo = new TestAppointmentRepository(verifyContext);

        var hasConflictSameDay = await repo.HasApplicantSameDayConflictAsync(
            applicant.PersonId, new DateTime(2026, 11, 10, 12, 0, 0));
        Assert.True(hasConflictSameDay);

        // Next working day has NO conflict
        var hasConflictNextDay = await repo.HasApplicantSameDayConflictAsync(
            applicant.PersonId, new DateTime(2026, 11, 11, 10, 0, 0));
        Assert.False(hasConflictNextDay);
    }

    // 14. Restart recovery processes overdue Scheduled appointments
    [Fact]
    public async Task ProcessPendingNoShows_RestartRecovery_ProcessesOverdueScheduledAppointments()
    {
        var applicant = await SeedApplicantAsync("RESTARTRECOVERY");
        var baseTime = new DateTime(2026, 11, 10, 8, 0, 0);

        // Create 3 overdue appointments that accumulated while service was stopped
        var app1 = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime);
        var app2 = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime.AddHours(1));
        var app3 = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime.AddHours(2));

        // System restarts at 12:00 (all 3 are well overdue)
        var restartTime = baseTime.AddHours(4);
        var (processor, _) = CreateProcessor(restartTime, batchSize: 2); // Batch size 2 forces multiple batches

        var result = await processor.ProcessPendingNoShowsAsync();

        Assert.Equal(3, result.TotalCandidatesFound);
        Assert.Equal(3, result.ProcessedCount);

        await using var verifyContext = _database.CreateContext();
        var list = await verifyContext.TestAppointments
            .Where(x => x.LocalDrivingLicenseApplicationID == applicant.LocalAppId)
            .ToListAsync();

        Assert.All(list, a =>
        {
            Assert.Equal(AppointmentStatus.NoShow, a.Status);
            Assert.True(a.IsLocked);
            Assert.Equal(restartTime, a.NoShowDate);
        });
    }

    // 15. A failed batch does not prevent subsequent batches from being processed
    [Fact]
    public async Task ProcessPendingNoShows_FailedBatchOrItem_DoesNotPreventSubsequentProcessing()
    {
        var applicant = await SeedApplicantAsync("FAILRECOVERY");
        var baseTime = new DateTime(2026, 11, 10, 8, 0, 0);

        // 3 overdue appointments
        var app1 = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime);
        var app2 = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime.AddMinutes(10));
        var app3 = await SeedAppointmentAsync(applicant.LocalAppId, applicant.UserId, baseTime.AddMinutes(20));

        // Hold an exclusive applock on app2 externally to simulate a lock timeout or transient contention
        var lockConn = new Microsoft.Data.SqlClient.SqlConnection(_database.ConnectionString);
        await lockConn.OpenAsync();
        var lockTx = lockConn.BeginTransaction();

        await using (var lockCmd = lockConn.CreateCommand())
        {
            lockCmd.Transaction = lockTx;
            lockCmd.CommandText = "sp_getapplock";
            lockCmd.CommandType = CommandType.StoredProcedure;
            lockCmd.Parameters.AddWithValue("@Resource", $"DVLD_Appointment_{app2.TestAppointmentID}");
            lockCmd.Parameters.AddWithValue("@LockMode", "Exclusive");
            lockCmd.Parameters.AddWithValue("@LockOwner", "Transaction");
            lockCmd.Parameters.AddWithValue("@LockTimeout", 0);
            await lockCmd.ExecuteNonQueryAsync();
        }

        try
        {
            var processTime = baseTime.AddHours(2);

            var options = Options.Create(new NoShowProcessorOptions
            {
                IsEnabled = true,
                BatchSize = 50,
                GracePeriodMinutes = 40,
                LockTimeoutMs = 100, // fast timeout
                PollingIntervalSeconds = 60
            });

            var context = _database.CreateContext();
            var customProcessor = new NoShowProcessorService(
                new UnitOfWork(context),
                new TestAppointmentRepository(context),
                new TestRepository(context),
                new TestJordanClock(processTime),
                options,
                NullLogger<NoShowProcessorService>.Instance);

            var result = await customProcessor.ProcessPendingNoShowsAsync();

            // app1 and app3 should be processed successfully; app2 lock fails
            Assert.Equal(3, result.TotalCandidatesFound);
            Assert.Equal(2, result.ProcessedCount);
            Assert.Equal(1, result.LockFailedCount);

            await using var verifyContext = _database.CreateContext();
            var verified1 = await verifyContext.TestAppointments.FindAsync(app1.TestAppointmentID);
            var verified2 = await verifyContext.TestAppointments.FindAsync(app2.TestAppointmentID);
            var verified3 = await verifyContext.TestAppointments.FindAsync(app3.TestAppointmentID);

            Assert.Equal(AppointmentStatus.NoShow, verified1!.Status);
            Assert.Equal(AppointmentStatus.Scheduled, verified2!.Status);
            Assert.Equal(AppointmentStatus.NoShow, verified3!.Status);
        }
        finally
        {
            lockTx.Rollback();
            await lockConn.CloseAsync();
            await lockConn.DisposeAsync();
        }
    }
}
