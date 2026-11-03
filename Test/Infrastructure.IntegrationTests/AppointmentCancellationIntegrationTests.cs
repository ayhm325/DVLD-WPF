using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.TestAppointmentDTO;
using Application.Interfaces;
using Application.Services;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Xunit;

namespace Infrastructure.IntegrationTests;

public sealed class AppointmentCancellationIntegrationTests : IAsyncLifetime
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
            NationalNo = $"N_{nationalNoPrefix}_{uid[..6]}",
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

    private sealed class TestCurrentUserService(int userId) : ICurrentUserService
    {
        public int UserId => userId;
        public string Username => "test_staff";
        public string FullName => "Test Staff";
        public UserRole Role => UserRole.Staff;
        public string AccessToken => "integration_test_token";
        public bool IsLoggedIn => true;
        public void SetSession(int id, string u, string fn, UserRole r, string at) { }
        public void Clear() { }
    }

    private TestAppointmentService CreateService(DVLDDbContext context, int staffUserId)
    {
        var unitOfWork = new UnitOfWork(context);
        var appointmentRepo = new TestAppointmentRepository(context);
        var testTypeRepo = new TestTypeRepository(context);
        var appTypeRepo = new ApplicationTypeRepository(context);
        var appRepo = new ApplicationRepository(context);
        var localAppRepo = new LocalDrivingLicenseApplicationRepository(context);
        var licenseRepo = new LicenseRepository(context);
        var licenseClassRepo = new LicenseClassRepository(context);

        var currentUserService = new TestCurrentUserService(staffUserId);
        var applicationService = new ApplicationService(appRepo, appTypeRepo, unitOfWork, currentUserService);
        var applicationTypeService = new ApplicationTypeService(appTypeRepo, unitOfWork);
        var licenseClassService = new LicenseClassService(licenseClassRepo, unitOfWork);
        var localAppService = new LocalDrivingLicenseApplicationService(
            localAppRepo, licenseRepo, unitOfWork, appRepo, appTypeRepo, licenseClassService, currentUserService, applicationService);
        var workflowService = new TestWorkflowService(appointmentRepo);

        return new TestAppointmentService(
            unitOfWork,
            appointmentRepo,
            testTypeRepo,
            localAppService,
            applicationTypeService,
            currentUserService,
            workflowService,
            applicationService,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TestAppointmentService>.Instance);
    }

    private static DateTime GetValidWorkingDaySlot(int daysAhead = 4, int hour = 8, int minute = 0)
    {
        var jordanNow = TestAppointmentValidator.GetCurrentJordanTime();
        var candidate = jordanNow.Date.AddDays(daysAhead);
        while (candidate.DayOfWeek == DayOfWeek.Friday || candidate.DayOfWeek == DayOfWeek.Saturday)
        {
            candidate = candidate.AddDays(1);
        }
        return candidate.AddHours(hour).AddMinutes(minute);
    }

    // =========================================================================
    // 1. SOFT CANCELLATION WORKFLOW TESTS
    // =========================================================================

    [Fact]
    public async Task CancelAsync_SuccessfulSoftCancellation_PreservesRowAndMetadata_AndDoesNotCreateTestOrRetake()
    {
        var applicant = await SeedApplicantAsync("SC_OK");
        var slot = GetValidWorkingDaySlot(daysAhead: 5, hour: 8, minute: 40);

        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        var scheduleResult = await service.ScheduleAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory, slot);
        Assert.True(scheduleResult.IsSuccess);
        var appointmentId = scheduleResult.Value.AppointmentID;

        // Act: Soft Cancel
        var cancelResult = await service.CancelAsync(appointmentId);
        Assert.True(cancelResult.IsSuccess);

        // Assert: Reload in fresh context
        await using var verifyContext = _database.CreateContext();
        var reloaded = await verifyContext.TestAppointments
            .Include(x => x.Test)
            .Include(x => x.RetakeTestApplication)
            .FirstOrDefaultAsync(x => x.TestAppointmentID == appointmentId);

        Assert.NotNull(reloaded);
        Assert.Equal(AppointmentStatus.Cancelled, reloaded.Status);
        Assert.True(reloaded.IsLocked); // Locked to prevent edits
        Assert.NotNull(reloaded.CancelledAt);
        Assert.Equal(applicant.UserId, reloaded.CancelledByUserID);
        Assert.Equal(slot, reloaded.AppointmentDate);
        Assert.Equal(10m, reloaded.PaidFees);
        Assert.Equal((int)TestTypeEnum.Theory, reloaded.TestTypeID);
        Assert.Equal(applicant.LocalAppId, reloaded.LocalDrivingLicenseApplicationID);

        // Crucial invariants:
        Assert.Null(reloaded.Test); // No test row created
        Assert.Null(reloaded.RetakeTestApplicationID); // No retake application created
    }

    [Fact]
    public async Task CancelAsync_RepeatedCancellation_ReturnsConflict_AndDoesNotOverwriteMetadata()
    {
        var applicant = await SeedApplicantAsync("SC_REP");
        var slot = GetValidWorkingDaySlot(daysAhead: 5, hour: 9, minute: 20);

        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        var scheduleResult = await service.ScheduleAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory, slot);
        var appointmentId = scheduleResult.Value.AppointmentID;

        // First cancellation
        var firstCancel = await service.CancelAsync(appointmentId);
        Assert.True(firstCancel.IsSuccess);

        await using var verifyContext = _database.CreateContext();
        var original = await verifyContext.TestAppointments.FindAsync(appointmentId);
        Assert.NotNull(original);
        var originalCancelledAt = original.CancelledAt;
        var originalCancelledBy = original.CancelledByUserID;

        // Second cancellation attempt
        var secondCancel = await service.CancelAsync(appointmentId);
        Assert.True(secondCancel.IsFailure);
        Assert.Equal(ErrorType.Conflict, secondCancel.ErrorType);
        Assert.Equal("Appointment is already cancelled.", secondCancel.Error);

        // Metadata must be intact
        await using var verifyContext2 = _database.CreateContext();
        var afterSecond = await verifyContext2.TestAppointments.FindAsync(appointmentId);
        Assert.NotNull(afterSecond);
        Assert.Equal(originalCancelledAt, afterSecond.CancelledAt);
        Assert.Equal(originalCancelledBy, afterSecond.CancelledByUserID);
    }

    [Fact]
    public async Task CancelAsync_CompletedAppointment_ReturnsConflict()
    {
        var applicant = await SeedApplicantAsync("SC_CMP");
        var slot = GetValidWorkingDaySlot(daysAhead: 5, hour: 10, minute: 0);

        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        var scheduleResult = await service.ScheduleAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory, slot);
        var appointmentId = scheduleResult.Value.AppointmentID;

        // Mark as completed with test
        var appt = await context.TestAppointments.FindAsync(appointmentId);
        Assert.NotNull(appt);
        appt.Status = AppointmentStatus.Completed;
        appt.IsLocked = true;
        context.Tests.Add(new Test
        {
            TestAppointmentID = appointmentId,
            TestResult = true,
            Notes = "Passed",
            CreatedByUserID = applicant.UserId
        });
        await context.SaveChangesAsync();

        // Attempt cancel with fresh context so EF Core reads the raw SQL update
        await using var cancelContext = _database.CreateContext();
        var cancelService = CreateService(cancelContext, applicant.UserId);
        var cancelResult = await cancelService.CancelAsync(appointmentId);
        Assert.True(cancelResult.IsFailure);
        Assert.Equal(ErrorType.Conflict, cancelResult.ErrorType);
        Assert.Equal("Cannot cancel a completed appointment.", cancelResult.Error);
    }

    [Fact]
    public async Task CancelAsync_NoShowAppointment_ReturnsConflict()
    {
        var applicant = await SeedApplicantAsync("SC_NS");
        var slot = GetValidWorkingDaySlot(daysAhead: 5, hour: 10, minute: 40);

        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        var scheduleResult = await service.ScheduleAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory, slot);
        var appointmentId = scheduleResult.Value.AppointmentID;

        var appt = await context.TestAppointments.FindAsync(appointmentId);
        Assert.NotNull(appt);
        appt.Status = AppointmentStatus.NoShow;
        appt.IsLocked = true;
        appt.NoShowDate = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Attempt cancel with fresh context so EF Core reads the raw SQL update
        await using var cancelContext = _database.CreateContext();
        var cancelService = CreateService(cancelContext, applicant.UserId);
        var cancelResult = await cancelService.CancelAsync(appointmentId);
        Assert.True(cancelResult.IsFailure);
        Assert.Equal(ErrorType.Conflict, cancelResult.ErrorType);
        Assert.Equal("Cannot cancel a no-show appointment.", cancelResult.Error);
    }

    [Fact]
    public async Task CancelAsync_NullStatusAppointment_ReturnsConflict_AndPreservesNull()
    {
        var applicant = await SeedApplicantAsync("SC_NULL");
        var slot = GetValidWorkingDaySlot(daysAhead: 5, hour: 11, minute: 20);

        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        var scheduleResult = await service.ScheduleAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory, slot);
        var appointmentId = scheduleResult.Value.AppointmentID;

        // Set status to NULL to simulate unresolved historical record
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE TestAppointments SET Status = NULL WHERE TestAppointmentID = {0};", appointmentId);

        // Attempt cancel with fresh context so EF Core reads the raw SQL update
        await using var cancelContext = _database.CreateContext();
        var cancelService = CreateService(cancelContext, applicant.UserId);
        var cancelResult = await cancelService.CancelAsync(appointmentId);
        Assert.True(cancelResult.IsFailure);
        Assert.Equal(ErrorType.Conflict, cancelResult.ErrorType);
        Assert.Equal("Appointment has an unresolved lifecycle status and cannot be cancelled.", cancelResult.Error);

        // Verify status remains NULL in DB
        await using var verifyContext = _database.CreateContext();
        var reloaded = await verifyContext.TestAppointments.FindAsync(appointmentId);
        Assert.NotNull(reloaded);
        Assert.Null(reloaded.Status);
    }

    [Fact]
    public async Task CancelAsync_CutoffViolated_ReturnsValidationFailure()
    {
        var applicant = await SeedApplicantAsync("SC_CUT");
        var jordanNow = TestAppointmentValidator.GetCurrentJordanTime();
        // 1 calendar day in future (violates 2 calendar days rule)
        var nearSlot = jordanNow.Date.AddDays(1).AddHours(8);

        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        // Seed directly to bypass schedule validator
        var appt = new TestAppointment
        {
            TestTypeID = (int)TestTypeEnum.Theory,
            LocalDrivingLicenseApplicationID = applicant.LocalAppId,
            AppointmentDate = nearSlot,
            PaidFees = 10m,
            CreatedByUserID = applicant.UserId,
            IsLocked = false,
            Status = AppointmentStatus.Scheduled
        };
        context.TestAppointments.Add(appt);
        await context.SaveChangesAsync();

        // Attempt cancel
        var cancelResult = await service.CancelAsync(appt.TestAppointmentID);
        Assert.True(cancelResult.IsFailure);
        Assert.Equal(ErrorType.Validation, cancelResult.ErrorType);
        Assert.Equal("Appointments cannot be modified or cancelled less than two calendar days before the scheduled date.", cancelResult.Error);
    }

    [Fact]
    public async Task CancelAsync_ConcurrentRequests_ExactlyOneSucceeds_MetadataNotCorrupted()
    {
        var applicant = await SeedApplicantAsync("SC_CONC");
        var slot = GetValidWorkingDaySlot(daysAhead: 6, hour: 12, minute: 0);

        await using var initContext = _database.CreateContext();
        var initService = CreateService(initContext, applicant.UserId);

        var scheduleResult = await initService.ScheduleAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory, slot);
        var appointmentId = scheduleResult.Value.AppointmentID;

        // Launch 4 concurrent cancel requests
        var tasks = Enumerable.Range(0, 4).Select(async _ =>
        {
            await using var taskContext = _database.CreateContext();
            var taskService = CreateService(taskContext, applicant.UserId);
            return await taskService.CancelAsync(appointmentId);
        }).ToList();

        var results = await Task.WhenAll(tasks);

        var successes = results.Count(r => r.IsSuccess);
        var conflicts = results.Count(r => r.IsFailure && r.ErrorType == ErrorType.Conflict);

        Assert.Equal(1, successes);
        Assert.Equal(3, conflicts);

        await using var verifyContext = _database.CreateContext();
        var finalAppt = await verifyContext.TestAppointments.FindAsync(appointmentId);
        Assert.NotNull(finalAppt);
        Assert.Equal(AppointmentStatus.Cancelled, finalAppt.Status);
        Assert.True(finalAppt.IsLocked);
        Assert.NotNull(finalAppt.CancelledAt);
    }

    // =========================================================================
    // 2. CANCELLED QUERY EXCLUSIONS & SCHEDULING PROTECTION TESTS
    // =========================================================================

    [Fact]
    public async Task CancelledAppointment_DoesNotOccupySlot_AllowsAnotherApplicantToBook()
    {
        var applicantA = await SeedApplicantAsync("EX_A");
        var applicantB = await SeedApplicantAsync("EX_B");
        var slot = GetValidWorkingDaySlot(daysAhead: 5, hour: 12, minute: 40);

        await using var context = _database.CreateContext();
        var serviceA = CreateService(context, applicantA.UserId);
        var serviceB = CreateService(context, applicantB.UserId);

        // Applicant A books slot
        var bookA = await serviceA.ScheduleAsync(applicantA.LocalAppId, (int)TestTypeEnum.Theory, slot);
        Assert.True(bookA.IsSuccess);

        var repo = new TestAppointmentRepository(context);
        Assert.True(await repo.IsSlotOccupiedAsync((int)TestTypeEnum.Theory, slot));

        // Applicant A cancels appointment
        var cancelA = await serviceA.CancelAsync(bookA.Value.AppointmentID);
        Assert.True(cancelA.IsSuccess);

        // Slot must now be FREE: IsSlotOccupiedAsync returns false
        Assert.False(await repo.IsSlotOccupiedAsync((int)TestTypeEnum.Theory, slot));

        // Applicant B can book the exact same slot!
        var bookB = await serviceB.ScheduleAsync(applicantB.LocalAppId, (int)TestTypeEnum.Theory, slot);
        Assert.True(bookB.IsSuccess);
    }

    [Fact]
    public async Task CancelledAppointment_DoesNotCauseApplicantSameDayConflict()
    {
        var applicant = await SeedApplicantAsync("EX_SD");
        var slot1 = GetValidWorkingDaySlot(daysAhead: 5, hour: 8, minute: 0);
        var slot2 = slot1.Date.AddHours(9).AddMinutes(20); // Same day, different period

        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        // Book slot 1
        var book1 = await service.ScheduleAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory, slot1);
        Assert.True(book1.IsSuccess);

        var repo = new TestAppointmentRepository(context);
        // Before cancellation: Has same-day conflict for slot 2
        Assert.True(await repo.HasApplicantSameDayConflictAsync(applicant.PersonId, slot2));

        // Cancel slot 1
        var cancel1 = await service.CancelAsync(book1.Value.AppointmentID);
        Assert.True(cancel1.IsSuccess);

        // After cancellation: HasApplicantSameDayConflictAsync must return FALSE!
        Assert.False(await repo.HasApplicantSameDayConflictAsync(applicant.PersonId, slot2));

        // Same applicant can book slot 2 on that same day!
        var book2 = await service.ScheduleAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory, slot2);
        Assert.True(book2.IsSuccess);
    }

    [Fact]
    public async Task CancelledAppointment_DoesNotIncreaseTrialCount_AndDoesNotChargeRetakeFeeOnReplacement()
    {
        var applicant = await SeedApplicantAsync("EX_TR");
        var slot1 = GetValidWorkingDaySlot(daysAhead: 5, hour: 13, minute: 20);
        var slot2 = GetValidWorkingDaySlot(daysAhead: 6, hour: 14, minute: 0);

        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        // First booking: Trial should be 1, no retake fee
        var book1 = await service.ScheduleAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory, slot1);
        Assert.True(book1.IsSuccess);
        Assert.Equal(1, book1.Value.Trial);
        Assert.Equal(0m, book1.Value.RetakerFees);
        Assert.Null(book1.Value.RetakeTestApplicationID);

        // Cancel first booking
        var cancel1 = await service.CancelAsync(book1.Value.AppointmentID);
        Assert.True(cancel1.IsSuccess);

        var repo = new TestAppointmentRepository(context);
        // Trial count must be 0 after cancellation!
        var trialCount = await repo.GetTrialCountAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory);
        Assert.Equal(0, trialCount);

        // Replacement booking: Must STILL be Trial = 1 and charge ZERO retake fee!
        var book2 = await service.ScheduleAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory, slot2);
        Assert.True(book2.IsSuccess);
        Assert.Equal(1, book2.Value.Trial);
        Assert.Equal(0m, book2.Value.RetakerFees);
        Assert.Null(book2.Value.RetakeTestApplicationID);
    }

    [Fact]
    public async Task CancelledAppointment_DoesNotAppearAsActiveAppointment()
    {
        var applicant = await SeedApplicantAsync("EX_ACT");
        var slot = GetValidWorkingDaySlot(daysAhead: 5, hour: 14, minute: 40);

        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        var book = await service.ScheduleAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory, slot);
        Assert.True(book.IsSuccess);

        var repo = new TestAppointmentRepository(context);
        Assert.True(await repo.IsAppointmentAlreadyScheduledAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory));

        // Cancel
        var cancel = await service.CancelAsync(book.Value.AppointmentID);
        Assert.True(cancel.IsSuccess);

        // IsAppointmentAlreadyScheduledAsync must return false
        Assert.False(await repo.IsAppointmentAlreadyScheduledAsync(applicant.LocalAppId, (int)TestTypeEnum.Theory));
    }
}
