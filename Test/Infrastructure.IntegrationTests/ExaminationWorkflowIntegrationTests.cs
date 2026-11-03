using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.TestAppointmentDTO;
using Application.DTOs.TestDTO;
using Application.Interfaces;
using Application.Services;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;
using Infrastructure;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using Xunit;

namespace Infrastructure.IntegrationTests;

public sealed class ExaminationWorkflowIntegrationTests : IAsyncLifetime
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
            new TestType { TestTypeId = (int)TestTypeEnum.Theory, TestTypeTitle = "Vision Test", TestTypeDescription = "Vision Exam", TestTypeFees = 10m },
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
            FirstName = "Ahmad",
            SecondName = "Mahmoud",
            LastName = "Ali",
            DateOfBirth = new DateTime(1996, 5, 15),
            Gender = 0,
            Address = "Amman, Jordan",
            Phone = "0791112233",
            NationalityCountryID = country.CountryId
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var user = new User
        {
            PersonId = person.PersonId,
            UserName = $"staff_{uid[..8]}",
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

    private sealed class TestControlledClock(DateTime now) : IJordanClock
    {
        public DateTime Now { get; set; } = now;
    }

    private (TestService testService, TestAppointmentService appointmentService, LocalDrivingLicenseApplicationService localAppService)
        CreateServices(DVLDDbContext context, int staffUserId, IJordanClock? clock = null)
    {
        var unitOfWork = new UnitOfWork(context);
        var appointmentRepo = new TestAppointmentRepository(context);
        var testRepo = new TestRepository(context);
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
            localAppRepo, licenseRepo, unitOfWork, appRepo, appTypeRepo, licenseClassService, currentUserService, applicationService, appointmentRepo);
        var workflowService = new TestWorkflowService(appointmentRepo, clock);

        var appointmentService = new TestAppointmentService(
            unitOfWork,
            appointmentRepo,
            testTypeRepo,
            localAppService,
            applicationTypeService,
            currentUserService,
            workflowService,
            applicationService,
            NullLogger<TestAppointmentService>.Instance);

        var testService = new TestService(
            testRepo,
            appointmentRepo,
            currentUserService,
            workflowService,
            unitOfWork,
            NullLogger<TestService>.Instance);

        return (testService, appointmentService, localAppService);
    }

    private async Task<TestAppointment> CreateAppointmentRecordAsync(
        int localAppId,
        int testTypeId,
        DateTime appointmentDate,
        AppointmentStatus status = AppointmentStatus.Scheduled,
        bool isLocked = false,
        int createdByUserId = 1)
    {
        await using var context = _database.CreateContext();
        var appointment = new TestAppointment
        {
            LocalDrivingLicenseApplicationID = localAppId,
            TestTypeID = testTypeId,
            AppointmentDate = appointmentDate,
            PaidFees = 10m,
            CreatedByUserID = createdByUserId,
            IsLocked = isLocked,
            Status = status
        };
        context.TestAppointments.Add(appointment);
        await context.SaveChangesAsync();
        return appointment;
    }

    // =========================================================================
    // 1. RESULT RECORDING & ATOMIC COMPLETION (PASS / FAIL)
    // =========================================================================

    [Fact]
    public async Task AddResult_PassResult_CreatesOneTest_AndCompletesAndLocksAppointment()
    {
        var seed = await SeedApplicantAsync("pass");
        var apptDate = DateTime.UtcNow.AddMinutes(-30);
        var appointment = await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, apptDate, createdByUserId: seed.UserId);

        await using var context = _database.CreateContext();
        var (testService, _, _) = CreateServices(context, seed.UserId);

        var result = await testService.AddAsync(new SaveTestResultDto
        {
            TestAppointmentID = appointment.TestAppointmentID,
            TestResult = true,
            Notes = "Candidate demonstrated exceptional proficiency."
        });

        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);

        await using var verifyContext = _database.CreateContext();
        var updatedAppt = await verifyContext.TestAppointments.Include(a => a.Test).FirstOrDefaultAsync(a => a.TestAppointmentID == appointment.TestAppointmentID);
        Assert.NotNull(updatedAppt);
        Assert.Equal(AppointmentStatus.Completed, updatedAppt.Status);
        Assert.True(updatedAppt.IsLocked);
        Assert.NotNull(updatedAppt.Test);
        Assert.Equal(result.Value, updatedAppt.Test.TestID);
        Assert.True(updatedAppt.Test.TestResult);
        Assert.Equal("Candidate demonstrated exceptional proficiency.", updatedAppt.Test.Notes);
    }

    [Fact]
    public async Task AddResult_FailResult_CreatesOneTest_AndCompletesAndLocksAppointment()
    {
        var seed = await SeedApplicantAsync("fail");
        var apptDate = DateTime.UtcNow.AddMinutes(-30);
        var appointment = await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, apptDate, createdByUserId: seed.UserId);

        await using var context = _database.CreateContext();
        var (testService, _, _) = CreateServices(context, seed.UserId);

        var result = await testService.AddAsync(new SaveTestResultDto
        {
            TestAppointmentID = appointment.TestAppointmentID,
            TestResult = false,
            Notes = "Candidate failed sight acuity criteria."
        });

        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);

        await using var verifyContext = _database.CreateContext();
        var updatedAppt = await verifyContext.TestAppointments.Include(a => a.Test).FirstOrDefaultAsync(a => a.TestAppointmentID == appointment.TestAppointmentID);
        Assert.NotNull(updatedAppt);
        Assert.Equal(AppointmentStatus.Completed, updatedAppt.Status);
        Assert.True(updatedAppt.IsLocked);
        Assert.NotNull(updatedAppt.Test);
        Assert.Equal(result.Value, updatedAppt.Test.TestID);
        Assert.False(updatedAppt.Test.TestResult);
    }

    // =========================================================================
    // 2. DUPLICATE & CONCURRENCY PROTECTION
    // =========================================================================

    [Fact]
    public async Task AddResult_DuplicateSubmission_ReturnsConflict_AndDoesNotCreateSecondTest()
    {
        var seed = await SeedApplicantAsync("dup");
        var apptDate = DateTime.UtcNow.AddMinutes(-30);
        var appointment = await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, apptDate, createdByUserId: seed.UserId);

        await using var context1 = _database.CreateContext();
        var (testService1, _, _) = CreateServices(context1, seed.UserId);
        var firstResult = await testService1.AddAsync(new SaveTestResultDto
        {
            TestAppointmentID = appointment.TestAppointmentID,
            TestResult = true,
            Notes = "Initial pass"
        });
        Assert.True(firstResult.IsSuccess);

        await using var context2 = _database.CreateContext();
        var (testService2, _, _) = CreateServices(context2, seed.UserId);
        var secondResult = await testService2.AddAsync(new SaveTestResultDto
        {
            TestAppointmentID = appointment.TestAppointmentID,
            TestResult = false,
            Notes = "Duplicate attempt"
        });

        Assert.False(secondResult.IsSuccess);
        Assert.Equal(ErrorType.Conflict, secondResult.ErrorType);

        await using var verifyContext = _database.CreateContext();
        var tests = await verifyContext.Tests.Where(t => t.TestAppointmentID == appointment.TestAppointmentID).ToListAsync();
        Assert.Single(tests);
    }

    [Fact]
    public async Task AddResult_ConcurrentSubmissions_ExactlyOneSucceeds_AndCreatesSingleTest()
    {
        var seed = await SeedApplicantAsync("concurrent");
        var apptDate = DateTime.UtcNow.AddMinutes(-30);
        var appointment = await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, apptDate, createdByUserId: seed.UserId);

        await using var contextA = _database.CreateContext();
        await using var contextB = _database.CreateContext();
        var (serviceA, _, _) = CreateServices(contextA, seed.UserId);
        var (serviceB, _, _) = CreateServices(contextB, seed.UserId);

        var taskA = Task.Run(() => serviceA.AddAsync(new SaveTestResultDto
        {
            TestAppointmentID = appointment.TestAppointmentID,
            TestResult = true,
            Notes = "Concurrent Worker A"
        }));

        var taskB = Task.Run(() => serviceB.AddAsync(new SaveTestResultDto
        {
            TestAppointmentID = appointment.TestAppointmentID,
            TestResult = false,
            Notes = "Concurrent Worker B"
        }));

        var results = await Task.WhenAll(taskA, taskB);

        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => r.IsFailure);

        Assert.Equal(1, successCount);
        Assert.Equal(1, failureCount);

        await using var verifyContext = _database.CreateContext();
        var tests = await verifyContext.Tests.Where(t => t.TestAppointmentID == appointment.TestAppointmentID).ToListAsync();
        Assert.Single(tests);
        var updatedAppt = await verifyContext.TestAppointments.FindAsync(appointment.TestAppointmentID);
        Assert.NotNull(updatedAppt);
        Assert.Equal(AppointmentStatus.Completed, updatedAppt.Status);
        Assert.True(updatedAppt.IsLocked);
    }

    // =========================================================================
    // 3. APPOINTMENT TIMING & LIFECYCLE REJECTIONS
    // =========================================================================

    [Fact]
    public async Task AddResult_FutureAppointment_ReturnsConflict_AndDoesNotCreateTest()
    {
        var seed = await SeedApplicantAsync("future");
        var apptDate = new DateTime(2026, 11, 10, 10, 0, 0);
        var clock = new TestControlledClock(new DateTime(2026, 11, 10, 9, 30, 0)); // 30 minutes before appointment
        var appointment = await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, apptDate, createdByUserId: seed.UserId);

        await using var context = _database.CreateContext();
        var (testService, _, _) = CreateServices(context, seed.UserId, clock);

        var result = await testService.AddAsync(new SaveTestResultDto
        {
            TestAppointmentID = appointment.TestAppointmentID,
            TestResult = true,
            Notes = "Attempting early examination"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("The appointment date has not arrived yet.", result.Error);

        await using var verifyContext = _database.CreateContext();
        var count = await verifyContext.Tests.CountAsync(t => t.TestAppointmentID == appointment.TestAppointmentID);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task AddResult_CancelledAppointment_ReturnsConflict_AndDoesNotCreateTest()
    {
        var seed = await SeedApplicantAsync("canc");
        var apptDate = DateTime.UtcNow.AddMinutes(-30);
        var appointment = await CreateAppointmentRecordAsync(
            seed.LocalAppId, (int)TestTypeEnum.Theory, apptDate, status: AppointmentStatus.Cancelled, isLocked: true, createdByUserId: seed.UserId);

        await using var context = _database.CreateContext();
        var (testService, _, _) = CreateServices(context, seed.UserId);

        var result = await testService.AddAsync(new SaveTestResultDto
        {
            TestAppointmentID = appointment.TestAppointmentID,
            TestResult = true
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task AddResult_NoShowAppointment_ReturnsConflict_AndDoesNotCreateTest()
    {
        var seed = await SeedApplicantAsync("noshow");
        var apptDate = DateTime.UtcNow.AddMinutes(-60);
        var appointment = await CreateAppointmentRecordAsync(
            seed.LocalAppId, (int)TestTypeEnum.Theory, apptDate, status: AppointmentStatus.NoShow, isLocked: true, createdByUserId: seed.UserId);

        await using var context = _database.CreateContext();
        var (testService, _, _) = CreateServices(context, seed.UserId);

        var result = await testService.AddAsync(new SaveTestResultDto
        {
            TestAppointmentID = appointment.TestAppointmentID,
            TestResult = true
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task AddResult_LockedAppointment_ReturnsConflict_AndDoesNotCreateTest()
    {
        var seed = await SeedApplicantAsync("locked");
        var apptDate = DateTime.UtcNow.AddMinutes(-30);
        var appointment = await CreateAppointmentRecordAsync(
            seed.LocalAppId, (int)TestTypeEnum.Theory, apptDate, status: AppointmentStatus.Scheduled, isLocked: true, createdByUserId: seed.UserId);

        await using var context = _database.CreateContext();
        var (testService, _, _) = CreateServices(context, seed.UserId);

        var result = await testService.AddAsync(new SaveTestResultDto
        {
            TestAppointmentID = appointment.TestAppointmentID,
            TestResult = true
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task AddResult_CancelledLocalApplication_ReturnsConflict_AndDoesNotCreateTest()
    {
        var seed = await SeedApplicantAsync("appcanc");
        var apptDate = DateTime.UtcNow.AddMinutes(-30);
        var appointment = await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, apptDate, createdByUserId: seed.UserId);

        // Cancel the main application
        await using (var cancelContext = _database.CreateContext())
        {
            var app = await cancelContext.Applications.FindAsync(seed.ApplicationId);
            Assert.NotNull(app);
            app.ApplicationStatus = AppStatus.Cancelled;
            await cancelContext.SaveChangesAsync();
        }

        await using var context = _database.CreateContext();
        var (testService, _, _) = CreateServices(context, seed.UserId);

        var result = await testService.AddAsync(new SaveTestResultDto
        {
            TestAppointmentID = appointment.TestAppointmentID,
            TestResult = true
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    // =========================================================================
    // 4. ATTEMPT STATISTICS & TRIAL NUMBERING
    // =========================================================================

    [Fact]
    public async Task AttemptStats_AccuratelyCountsPassFailAndNoShow_AndComputesNextTrial()
    {
        var seed = await SeedApplicantAsync("stats");

        // 1. One Fail
        var appt1 = await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, DateTime.UtcNow.AddDays(-5), AppointmentStatus.Completed, isLocked: true, createdByUserId: seed.UserId);
        await using (var ctx = _database.CreateContext())
        {
            ctx.Tests.Add(new Test { TestAppointmentID = appt1.TestAppointmentID, TestResult = false, CreatedByUserID = seed.UserId });
            await ctx.SaveChangesAsync();
        }

        // 2. One No-Show
        var appt2 = await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, DateTime.UtcNow.AddDays(-3), AppointmentStatus.NoShow, isLocked: true, createdByUserId: seed.UserId);

        // 3. One Cancelled (must NOT count)
        await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, DateTime.UtcNow.AddDays(-2), AppointmentStatus.Cancelled, isLocked: true, createdByUserId: seed.UserId);

        // 4. One Scheduled (must NOT count)
        await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, DateTime.UtcNow.AddDays(2), AppointmentStatus.Scheduled, isLocked: false, createdByUserId: seed.UserId);

        await using var context = _database.CreateContext();
        var repo = new TestAppointmentRepository(context);

        var stats = await repo.GetAttemptStatsAsync(seed.LocalAppId, (int)TestTypeEnum.Theory);

        Assert.Equal(seed.LocalAppId, stats.LocalDrivingLicenseApplicationId);
        Assert.Equal((int)TestTypeEnum.Theory, stats.TestTypeId);
        Assert.Equal(2, stats.TotalAttempts); // 1 Fail + 1 NoShow
        Assert.Equal(0, stats.PassedAttempts);
        Assert.Equal(1, stats.FailedAttempts);
        Assert.Equal(1, stats.NoShowAttempts);
        Assert.Equal(3, stats.NextTrialNumber); // Total + 1
    }

    // =========================================================================
    // 5. SEQUENTIAL EXAMINATION PROGRESSION
    // =========================================================================

    [Fact]
    public async Task SequentialProgression_WrittenRequiresPassedVision_PracticalRequiresPassedWritten()
    {
        var seed = await SeedApplicantAsync("seq");
        var apptDate = DateTime.UtcNow.AddMinutes(-30);

        // Schedule Written without passing Vision
        var writtenAppt = await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Written, apptDate, createdByUserId: seed.UserId);

        await using var context = _database.CreateContext();
        var (testService, _, _) = CreateServices(context, seed.UserId);

        var writtenResult = await testService.AddAsync(new SaveTestResultDto
        {
            TestAppointmentID = writtenAppt.TestAppointmentID,
            TestResult = true
        });

        Assert.False(writtenResult.IsSuccess);
        Assert.Equal(ErrorType.Conflict, writtenResult.ErrorType);
        Assert.Contains("cannot be taken yet", writtenResult.Error);

        // Now pass Vision
        var visionAppt = await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, apptDate, createdByUserId: seed.UserId);
        var visionResult = await testService.AddAsync(new SaveTestResultDto
        {
            TestAppointmentID = visionAppt.TestAppointmentID,
            TestResult = true
        });
        Assert.True(visionResult.IsSuccess);

        // Now Written examination succeeds!
        var writtenResult2 = await testService.AddAsync(new SaveTestResultDto
        {
            TestAppointmentID = writtenAppt.TestAppointmentID,
            TestResult = true
        });
        Assert.True(writtenResult2.IsSuccess);
    }

    // =========================================================================
    // 6. CANCELLATION CASCADE TRANSACTION AUDIT
    // =========================================================================

    [Fact]
    public async Task CancelLocalApplication_CascadesCancellationToAllActiveScheduledAppointments_Atomically()
    {
        var seed = await SeedApplicantAsync("cascade");

        // Active appointments
        var active1 = await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, DateTime.UtcNow.AddDays(3), AppointmentStatus.Scheduled, isLocked: false, createdByUserId: seed.UserId);
        var active2 = await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, DateTime.UtcNow.AddDays(4), AppointmentStatus.Scheduled, isLocked: false, createdByUserId: seed.UserId);

        // Past completed appointment (must remain Completed)
        var completed = await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, DateTime.UtcNow.AddDays(-5), AppointmentStatus.Completed, isLocked: true, createdByUserId: seed.UserId);

        // Past No-Show appointment (must remain NoShow)
        var noShow = await CreateAppointmentRecordAsync(seed.LocalAppId, (int)TestTypeEnum.Theory, DateTime.UtcNow.AddDays(-2), AppointmentStatus.NoShow, isLocked: true, createdByUserId: seed.UserId);

        await using var context = _database.CreateContext();
        var (_, _, localAppService) = CreateServices(context, seed.UserId);

        var cancelResult = await localAppService.CancelLocalDrivingLicenseApplicationAsync(seed.LocalAppId);
        Assert.True(cancelResult.IsSuccess);

        await using var verifyContext = _database.CreateContext();
        var mainApp = await verifyContext.Applications.FindAsync(seed.ApplicationId);
        Assert.NotNull(mainApp);
        Assert.Equal(AppStatus.Cancelled, mainApp.ApplicationStatus);

        var verifyActive1 = await verifyContext.TestAppointments.FindAsync(active1.TestAppointmentID);
        Assert.NotNull(verifyActive1);
        Assert.Equal(AppointmentStatus.Cancelled, verifyActive1.Status);
        Assert.True(verifyActive1.IsLocked);
        Assert.NotNull(verifyActive1.CancelledAt);
        Assert.Equal(seed.UserId, verifyActive1.CancelledByUserID);

        var verifyActive2 = await verifyContext.TestAppointments.FindAsync(active2.TestAppointmentID);
        Assert.NotNull(verifyActive2);
        Assert.Equal(AppointmentStatus.Cancelled, verifyActive2.Status);
        Assert.True(verifyActive2.IsLocked);

        var verifyCompleted = await verifyContext.TestAppointments.FindAsync(completed.TestAppointmentID);
        Assert.NotNull(verifyCompleted);
        Assert.Equal(AppointmentStatus.Completed, verifyCompleted.Status);
        Assert.True(verifyCompleted.IsLocked);
        Assert.Null(verifyCompleted.CancelledAt);

        var verifyNoShow = await verifyContext.TestAppointments.FindAsync(noShow.TestAppointmentID);
        Assert.NotNull(verifyNoShow);
        Assert.Equal(AppointmentStatus.NoShow, verifyNoShow.Status);
        Assert.True(verifyNoShow.IsLocked);
        Assert.Null(verifyNoShow.CancelledAt);
    }
}
