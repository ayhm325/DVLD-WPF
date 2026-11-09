using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.TestAppointmentDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.IntegrationTests;

public sealed class AppointmentConcurrencyIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerTestDatabase _database = new();
    private string _createdDatabaseName = string.Empty;

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();

        // Safety assertion: Ensure database has the mandatory prefix
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

        // 1. Seed Test Types (1: Theory, 2: Written, 3: Practical)
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

        // 2. Seed Application Types (1: New License, 7: Retake Test)
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

    private sealed record TestApplicantSeed(
        int PersonId,
        int UserId,
        int ApplicationId,
        int LocalApplicationId);

    private async Task<TestApplicantSeed> SeedApplicantAsync(string prefix = "T")
    {
        await using var context = _database.CreateContext();
        var uid = Guid.NewGuid().ToString("N");

        var country = new Country { CountryName = $"Country_{uid[..8]}" };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var person = new Person
        {
            NationalNo = $"N{uid[..14]}",
            FirstName = $"{prefix}_{uid[..4]}",
            SecondName = "App",
            LastName = "User",
            DateOfBirth = new DateTime(1995, 5, 15),
            Gender = Gender.Male,
            Address = "Amman, Jordan",
            Phone = $"07{uid[..8]}",
            Email = $"applicant_{uid}@dvld.jo",
            NationalityCountryID = country.CountryId
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var user = new User
        {
            PersonId = person.PersonId,
            UserName = $"staff_{uid[..12]}",
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

    private async Task<int> CreateAdditionalApplicationForPersonAsync(int personId, int userId)
    {
        await using var context = _database.CreateContext();
        var uid = Guid.NewGuid().ToString("N");

        var licenseClass = new LicenseClass
        {
            ClassName = $"ClassB_{uid[..6]}",
            ClassDescription = "Motorcycle class",
            MinimumAllowedAge = 18,
            DefaultValidityLength = 10,
            ClassFees = 25m
        };
        context.LicenseClasses.Add(licenseClass);

        var application = new ApplicationD
        {
            ApplicantPersonID = personId,
            CreatedByUserID = userId,
            ApplicationTypeID = 1,
            ApplicationDate = DateTime.UtcNow,
            ApplicationStatus = AppStatus.New,
            LastStatusDate = DateTime.UtcNow,
            PaidFees = 20m
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

        return localApp.LocalDrivingLicenseApplicationID;
    }

    // =========================================================================
    // SERVICE FACTORY HELPER (REAL PRODUCTION SERVICE COMPOSITION)
    // =========================================================================

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

    private static TestAppointmentService CreateService(
        DVLDDbContext context,
        int staffUserId,
        ITestAppointmentRepository? customAppointmentRepo = null)
    {
        var unitOfWork = new UnitOfWork(context);
        var appointmentRepo = customAppointmentRepo ?? new TestAppointmentRepository(context);
        var testTypeRepo = new TestTypeRepository(context);
        var appTypeRepo = new ApplicationTypeRepository(context);
        var appRepo = new ApplicationRepository(context);
        var localAppRepo = new LocalDrivingLicenseApplicationRepository(context);
        var licenseRepo = new LicenseRepository(context);
        var licenseClassRepo = new LicenseClassRepository(context);

        var clock = new Infrastructure.Services.JordanClock();
        var currentUserService = new TestCurrentUserService(staffUserId);
        var applicationService = new ApplicationService(appRepo, appTypeRepo, unitOfWork, currentUserService, clock);
        var applicationTypeService = new ApplicationTypeService(appTypeRepo, unitOfWork);
        var licenseClassService = new LicenseClassService(licenseClassRepo, unitOfWork);
        var localAppService = new LocalDrivingLicenseApplicationService(
            localAppRepo, licenseRepo, unitOfWork, appRepo, appTypeRepo, licenseClassService, currentUserService, applicationService, clock);
        var workflowService = new TestWorkflowService(appointmentRepo, clock);

        return new TestAppointmentService(
            unitOfWork,
            appointmentRepo,
            testTypeRepo,
            localAppService,
            applicationTypeService,
            currentUserService,
            workflowService,
            applicationService,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TestAppointmentService>.Instance,
            clock);
    }

    // =========================================================================
    // REPOSITORY-LEVEL WORKFLOW HELPER
    // =========================================================================

    private async Task<Result> TryBookSlotWorkflowAsync(
        int localAppId,
        int applicantPersonId,
        int testTypeId,
        DateTime appointmentDate,
        int staffUserId,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        await using var context = _database.CreateContext();
        var repository = new TestAppointmentRepository(context);

        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(isolationLevel);

            // 1. Acquire transaction-scoped deterministic locks
            var lockAcquired = await repository.AcquireSchedulingLocksAsync(
                applicantPersonId,
                testTypeId,
                appointmentDate,
                timeoutMs: 3000);

            if (!lockAcquired)
            {
                await transaction.RollbackAsync();
                return Result.Conflict("Lock acquisition timed out.");
            }

            // 2. Conflict checks
            if (await repository.IsSlotOccupiedAsync(testTypeId, appointmentDate))
            {
                await transaction.RollbackAsync();
                return Result.Conflict("Slot already booked.");
            }

            if (await repository.HasApplicantSameDayConflictAsync(applicantPersonId, appointmentDate))
            {
                await transaction.RollbackAsync();
                return Result.Conflict("Applicant already has test on same day.");
            }

            // 3. Create appointment
            var appointment = new TestAppointment
            {
                TestTypeID = testTypeId,
                LocalDrivingLicenseApplicationID = localAppId,
                AppointmentDate = appointmentDate,
                PaidFees = 10m,
                CreatedByUserID = staffUserId,
                IsLocked = false
            };

            await repository.AddAsync(appointment);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            return Result.Success();
        });
    }

    // =========================================================================
    // REPOSITORY-LEVEL TESTS (4 SCENARIOS)
    // =========================================================================

    [Fact]
    public async Task RepoScenario1_SameTestType_SameSlot_ExactlyOneSucceeds()
    {
        var applicantA = await SeedApplicantAsync("RAppA");
        var applicantB = await SeedApplicantAsync("RAppB");

        var testTypeId = (int)TestTypeEnum.Theory;
        var slot = new DateTime(2026, 12, 10, 8, 0, 0);

        using var barrier = new Barrier(2);

        var taskA = Task.Run(async () =>
        {
            barrier.SignalAndWait(5000);
            return await TryBookSlotWorkflowAsync(applicantA.LocalApplicationId, applicantA.PersonId, testTypeId, slot, applicantA.UserId);
        });

        var taskB = Task.Run(async () =>
        {
            barrier.SignalAndWait(5000);
            return await TryBookSlotWorkflowAsync(applicantB.LocalApplicationId, applicantB.PersonId, testTypeId, slot, applicantB.UserId);
        });

        var results = await Task.WhenAll(taskA, taskB);

        var successCount = results.Count(r => r.IsSuccess);
        var conflictCount = results.Count(r => r.IsFailure);

        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);

        await using var verifyContext = _database.CreateContext();
        var bookedCount = await verifyContext.TestAppointments
            .CountAsync(a => a.TestTypeID == testTypeId && a.AppointmentDate == slot);

        Assert.Equal(1, bookedCount);
    }

    [Fact]
    public async Task RepoScenario2_DifferentTestTypes_SameSlot_BothSucceed()
    {
        var applicantA = await SeedApplicantAsync("RAppA2");
        var applicantB = await SeedApplicantAsync("RAppB2");

        var testTypeVision = (int)TestTypeEnum.Theory;
        var testTypeWritten = (int)TestTypeEnum.Written;
        var slot = new DateTime(2026, 12, 11, 8, 0, 0);

        using var barrier = new Barrier(2);

        var taskA = Task.Run(async () =>
        {
            barrier.SignalAndWait(5000);
            return await TryBookSlotWorkflowAsync(applicantA.LocalApplicationId, applicantA.PersonId, testTypeVision, slot, applicantA.UserId);
        });

        var taskB = Task.Run(async () =>
        {
            barrier.SignalAndWait(5000);
            return await TryBookSlotWorkflowAsync(applicantB.LocalApplicationId, applicantB.PersonId, testTypeWritten, slot, applicantB.UserId);
        });

        var results = await Task.WhenAll(taskA, taskB);

        Assert.True(results[0].IsSuccess, $"Task A failed: {results[0].Error}");
        Assert.True(results[1].IsSuccess, $"Task B failed: {results[1].Error}");

        await using var verifyContext = _database.CreateContext();
        var visionCount = await verifyContext.TestAppointments
            .CountAsync(a => a.TestTypeID == testTypeVision && a.AppointmentDate == slot);
        var writtenCount = await verifyContext.TestAppointments
            .CountAsync(a => a.TestTypeID == testTypeWritten && a.AppointmentDate == slot);

        Assert.Equal(1, visionCount);
        Assert.Equal(1, writtenCount);
    }

    [Fact]
    public async Task RepoScenario3_SameApplicant_DifferentApplications_SameDay_AtMostOneSucceeds()
    {
        var applicant = await SeedApplicantAsync("RAppSame");
        var secondLocalAppId = await CreateAdditionalApplicationForPersonAsync(applicant.PersonId, applicant.UserId);

        var testType1 = (int)TestTypeEnum.Theory;
        var testType2 = (int)TestTypeEnum.Written;

        var slot1 = new DateTime(2026, 12, 12, 8, 0, 0);
        var slot2 = new DateTime(2026, 12, 12, 10, 0, 0);

        using var barrier = new Barrier(2);

        var task1 = Task.Run(async () =>
        {
            barrier.SignalAndWait(5000);
            return await TryBookSlotWorkflowAsync(applicant.LocalApplicationId, applicant.PersonId, testType1, slot1, applicant.UserId);
        });

        var task2 = Task.Run(async () =>
        {
            barrier.SignalAndWait(5000);
            return await TryBookSlotWorkflowAsync(secondLocalAppId, applicant.PersonId, testType2, slot2, applicant.UserId);
        });

        var results = await Task.WhenAll(task1, task2);

        var successCount = results.Count(r => r.IsSuccess);
        var conflictCount = results.Count(r => r.IsFailure);

        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);

        await using var verifyContext = _database.CreateContext();
        var dayStart = new DateTime(2026, 12, 12, 0, 0, 0);
        var dayEnd = dayStart.AddDays(1);

        var appointmentsOnDay = await verifyContext.TestAppointments
            .CountAsync(a => a.LocalDrivingLicenseApplication.Application.ApplicantPersonID == applicant.PersonId &&
                             a.AppointmentDate >= dayStart &&
                             a.AppointmentDate < dayEnd);

        Assert.Equal(1, appointmentsOnDay);
    }

    [Fact]
    public async Task RepoScenario4_TransactionRollback_ReleasesLocks_LeavesNoPartialRecords()
    {
        var applicant = await SeedApplicantAsync("RRollback");
        var testTypeId = (int)TestTypeEnum.Theory;
        var slot = new DateTime(2026, 12, 13, 8, 0, 0);

        int rolledBackAppId = 0;

        await using (var context1 = _database.CreateContext())
        {
            var repository1 = new TestAppointmentRepository(context1);
            await using var tx1 = await context1.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            var locksAcquired = await repository1.AcquireSchedulingLocksAsync(
                applicant.PersonId,
                testTypeId,
                slot,
                timeoutMs: 2000);

            Assert.True(locksAcquired);

            var retakeApp = new ApplicationD
            {
                ApplicantPersonID = applicant.PersonId,
                CreatedByUserID = applicant.UserId,
                ApplicationTypeID = 7,
                ApplicationDate = DateTime.UtcNow,
                ApplicationStatus = AppStatus.New,
                LastStatusDate = DateTime.UtcNow,
                PaidFees = 5m
            };
            context1.Applications.Add(retakeApp);
            await context1.SaveChangesAsync();
            rolledBackAppId = retakeApp.ApplicationID;

            var appointment = new TestAppointment
            {
                TestTypeID = testTypeId,
                LocalDrivingLicenseApplicationID = applicant.LocalApplicationId,
                AppointmentDate = slot,
                PaidFees = 10m,
                CreatedByUserID = applicant.UserId,
                IsLocked = false,
                RetakeTestApplicationID = retakeApp.ApplicationID
            };
            context1.TestAppointments.Add(appointment);
            await context1.SaveChangesAsync();

            await tx1.RollbackAsync();
        }

        await using (var verifyContext = _database.CreateContext())
        {
            var appExists = await verifyContext.Applications.AnyAsync(a => a.ApplicationID == rolledBackAppId);
            var appointmentExists = await verifyContext.TestAppointments.AnyAsync(a => a.TestTypeID == testTypeId && a.AppointmentDate == slot);

            Assert.False(appExists, "Rolled back retake application must not exist in database.");
            Assert.False(appointmentExists, "Rolled back appointment must not exist in database.");
        }

        var bookingResult = await TryBookSlotWorkflowAsync(
            applicant.LocalApplicationId,
            applicant.PersonId,
            testTypeId,
            slot,
            applicant.UserId);

        Assert.True(bookingResult.IsSuccess, $"Booking after rollback failed: {bookingResult.Error}");

        await using (var finalContext = _database.CreateContext())
        {
            var finalAppointment = await finalContext.TestAppointments
                .FirstOrDefaultAsync(a => a.TestTypeID == testTypeId && a.AppointmentDate == slot);

            Assert.NotNull(finalAppointment);
            Assert.Equal(applicant.LocalApplicationId, finalAppointment.LocalDrivingLicenseApplicationID);
        }
    }

    // =========================================================================
    // SERVICE-LEVEL INTEGRATION TESTS (INVOKING TestAppointmentService.ScheduleAsync)
    // =========================================================================

    [Fact]
    public async Task ServiceScenario1_SameTestType_SameSlot_OnlyOneSucceeds()
    {
        // Arrange: 2 distinct applicants
        var applicantA = await SeedApplicantAsync("SvcAppA");
        var applicantB = await SeedApplicantAsync("SvcAppB");

        var testTypeId = (int)TestTypeEnum.Theory; // 1
        var slot = new DateTime(2026, 12, 20, 8, 0, 0); // 08:00 AM

        using var barrier = new Barrier(2);

        // Act: Invoke actual TestAppointmentService.ScheduleAsync concurrently on independent connections
        var taskA = Task.Run(async () =>
        {
            await using var contextA = _database.CreateContext();
            var serviceA = CreateService(contextA, applicantA.UserId);
            barrier.SignalAndWait(5000);
            return await serviceA.ScheduleAsync(applicantA.LocalApplicationId, testTypeId, slot);
        });

        var taskB = Task.Run(async () =>
        {
            await using var contextB = _database.CreateContext();
            var serviceB = CreateService(contextB, applicantB.UserId);
            barrier.SignalAndWait(5000);
            return await serviceB.ScheduleAsync(applicantB.LocalApplicationId, testTypeId, slot);
        });

        var results = await Task.WhenAll(taskA, taskB);

        // Assert: Exactly 1 succeeded, 1 received conflict
        var successCount = results.Count(r => r.IsSuccess);
        var conflictCount = results.Count(r => r.IsFailure);

        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);

        var conflictResult = results.First(r => r.IsFailure);
        Assert.True(conflictResult.Error.Contains("already booked") || conflictResult.Error.Contains("lock"),
            $"Unexpected error message: {conflictResult.Error}");

        // Verify database: Exactly 1 record persisted
        await using var verifyContext = _database.CreateContext();
        var bookedCount = await verifyContext.TestAppointments
            .CountAsync(a => a.TestTypeID == testTypeId && a.AppointmentDate == slot);

        Assert.Equal(1, bookedCount);
    }

    private async Task PassTheoryTestAsync(int localAppId, int staffUserId)
    {
        await using var context = _database.CreateContext();
        var priorAppointment = new TestAppointment
        {
            TestTypeID = (int)TestTypeEnum.Theory,
            LocalDrivingLicenseApplicationID = localAppId,
            AppointmentDate = DateTime.UtcNow.AddDays(-5),
            PaidFees = 10m,
            CreatedByUserID = staffUserId,
            IsLocked = true
        };
        context.TestAppointments.Add(priorAppointment);
        await context.SaveChangesAsync();

        var test = new Test
        {
            TestAppointmentID = priorAppointment.TestAppointmentID,
            TestResult = true,
            Notes = "Passed Theory test",
            CreatedByUserID = staffUserId
        };
        context.Tests.Add(test);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ServiceScenario2_DifferentTestTypes_SameSlot_BothSucceed()
    {
        // Arrange: 2 distinct applicants
        var applicantA = await SeedApplicantAsync("SvcAppA2");
        var applicantB = await SeedApplicantAsync("SvcAppB2");

        // Applicant B passes Theory first so they are eligible to schedule Written
        await PassTheoryTestAsync(applicantB.LocalApplicationId, applicantB.UserId);

        var testTypeVision = (int)TestTypeEnum.Theory;   // 1
        var testTypeWritten = (int)TestTypeEnum.Written; // 2
        var slot = new DateTime(2026, 12, 21, 8, 0, 0); // 08:00 AM

        using var barrier = new Barrier(2);

        // Act: Invoke actual TestAppointmentService.ScheduleAsync concurrently
        // Both execute under production transaction isolation level (ReadCommitted)
        var taskA = Task.Run(async () =>
        {
            await using var contextA = _database.CreateContext();
            var serviceA = CreateService(contextA, applicantA.UserId);
            barrier.SignalAndWait(5000);
            return await serviceA.ScheduleAsync(applicantA.LocalApplicationId, testTypeVision, slot);
        });

        var taskB = Task.Run(async () =>
        {
            await using var contextB = _database.CreateContext();
            var serviceB = CreateService(contextB, applicantB.UserId);
            barrier.SignalAndWait(5000);
            return await serviceB.ScheduleAsync(applicantB.LocalApplicationId, testTypeWritten, slot);
        });

        var results = await Task.WhenAll(taskA, taskB);

        // Assert: Both must succeed without SQL Server deadlock (Error 1205)
        Assert.True(results[0].IsSuccess, $"Service Task A failed: {results[0].Error}");
        Assert.True(results[1].IsSuccess, $"Service Task B failed: {results[1].Error}");

        // Verify database: Both records persisted
        await using var verifyContext = _database.CreateContext();
        var visionCount = await verifyContext.TestAppointments
            .CountAsync(a => a.TestTypeID == testTypeVision && a.AppointmentDate == slot);
        var writtenCount = await verifyContext.TestAppointments
            .CountAsync(a => a.TestTypeID == testTypeWritten && a.AppointmentDate == slot);

        Assert.Equal(1, visionCount);
        Assert.Equal(1, writtenCount);
    }

    [Fact]
    public async Task ServiceScenario3_SameApplicant_DifferentApplications_SameDay_AtMostOneSucceeds()
    {
        // Arrange: 1 applicant with 2 distinct applications
        var applicant = await SeedApplicantAsync("SvcAppSame");
        var secondLocalAppId = await CreateAdditionalApplicationForPersonAsync(applicant.PersonId, applicant.UserId);

        // Application 2 passes Theory on an earlier date so it is eligible to schedule Written
        await PassTheoryTestAsync(secondLocalAppId, applicant.UserId);

        var testType1 = (int)TestTypeEnum.Theory;
        var testType2 = (int)TestTypeEnum.Written;

        var slot1 = new DateTime(2026, 12, 22, 8, 0, 0);  // 08:00 AM
        var slot2 = new DateTime(2026, 12, 22, 10, 0, 0); // 10:00 AM

        using var barrier = new Barrier(2);

        // Act: Invoke actual TestAppointmentService.ScheduleAsync concurrently for same person
        var task1 = Task.Run(async () =>
        {
            await using var context1 = _database.CreateContext();
            var service1 = CreateService(context1, applicant.UserId);
            barrier.SignalAndWait(5000);
            return await service1.ScheduleAsync(applicant.LocalApplicationId, testType1, slot1);
        });

        var task2 = Task.Run(async () =>
        {
            await using var context2 = _database.CreateContext();
            var service2 = CreateService(context2, applicant.UserId);
            barrier.SignalAndWait(5000);
            return await service2.ScheduleAsync(secondLocalAppId, testType2, slot2);
        });

        var results = await Task.WhenAll(task1, task2);

        // Assert: At most one succeeded, competing request received applicant conflict
        var successCount = results.Count(r => r.IsSuccess);
        var conflictCount = results.Count(r => r.IsFailure);

        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);

        var conflictResult = results.First(r => r.IsFailure);
        Assert.True(conflictResult.Error.Contains("calendar day") || conflictResult.Error.Contains("lock"),
            $"Unexpected error message: {conflictResult.Error}");

        // Verify database: Exactly 1 record on that calendar day
        await using var verifyContext = _database.CreateContext();
        var dayStart = new DateTime(2026, 12, 22, 0, 0, 0);
        var dayEnd = dayStart.AddDays(1);

        var countOnDay = await verifyContext.TestAppointments
            .CountAsync(a => a.LocalDrivingLicenseApplication.Application.ApplicantPersonID == applicant.PersonId &&
                             a.AppointmentDate >= dayStart &&
                             a.AppointmentDate < dayEnd);

        Assert.Equal(1, countOnDay);
    }

    [Fact]
    public async Task ServiceScenario4_ActualServiceLevelRollback_ReleasesLocks_LeavesNoPartialRecords()
    {
        // Arrange
        var applicant = await SeedApplicantAsync("SvcRollback");
        var testTypeId = (int)TestTypeEnum.Theory;
        var slot = new DateTime(2026, 12, 23, 8, 0, 0);

        // Step 1: Add a prior locked appointment for this applicant to trigger a retake test (trial = 2)
        await using (var seedContext = _database.CreateContext())
        {
            var priorAppointment = new TestAppointment
            {
                TestTypeID = testTypeId,
                LocalDrivingLicenseApplicationID = applicant.LocalApplicationId,
                AppointmentDate = new DateTime(2026, 12, 1, 8, 0, 0),
                PaidFees = 10m,
                CreatedByUserID = applicant.UserId,
                IsLocked = true,
                Status = AppointmentStatus.Completed
            };
            seedContext.TestAppointments.Add(priorAppointment);
            await seedContext.SaveChangesAsync();

            var test = new Test
            {
                TestAppointmentID = priorAppointment.TestAppointmentID,
                TestResult = false,
                Notes = "Failed initial test",
                CreatedByUserID = applicant.UserId
            };
            seedContext.Tests.Add(test);
            await seedContext.SaveChangesAsync();
        }

        // Count initial applications for this person
        int initialAppCount;
        await using (var verifyContext = _database.CreateContext())
        {
            initialAppCount = await verifyContext.Applications
                .CountAsync(a => a.ApplicantPersonID == applicant.PersonId);
        }

        // Step 2: Invoke TestAppointmentService.ScheduleAsync with a decorator that throws on AddAsync
        // (AFTER the service created the retake application inside the transaction)
        await using (var context1 = _database.CreateContext())
        {
            var innerRepo = new TestAppointmentRepository(context1);
            var failingRepo = new RollbackTriggerAppointmentRepository(innerRepo);
            var failingService = CreateService(context1, applicant.UserId, failingRepo);

            // Execute through real TestAppointmentService workflow
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => failingService.ScheduleAsync(applicant.LocalApplicationId, testTypeId, slot));

            Assert.Contains("Deliberate failure triggered after retake application creation", ex.Message);
        }

        // Verify: Service transaction rolled back; NO new retake application was persisted
        await using (var verifyContext = _database.CreateContext())
        {
            var currentAppCount = await verifyContext.Applications
                .CountAsync(a => a.ApplicantPersonID == applicant.PersonId);
            Assert.Equal(initialAppCount, currentAppCount);

            var appointmentExists = await verifyContext.TestAppointments
                .AnyAsync(a => a.TestTypeID == testTypeId && a.AppointmentDate == slot);
            Assert.False(appointmentExists, "Rolled back appointment must not exist.");
        }

        // Step 3: Now invoke real TestAppointmentService.ScheduleAsync without the decorator
        // Proves that transaction-scoped sp_getapplock locks were released upon rollback
        ScheduleTestDto? successfulScheduleDto = null;
        await using (var context2 = _database.CreateContext())
        {
            var normalService = CreateService(context2, applicant.UserId);
            var scheduleResult = await normalService.ScheduleAsync(applicant.LocalApplicationId, testTypeId, slot);

            Assert.True(scheduleResult.IsSuccess, $"Normal service schedule failed: {scheduleResult.Error}");
            successfulScheduleDto = scheduleResult.Value;
        }

        Assert.NotNull(successfulScheduleDto);
        Assert.True(successfulScheduleDto.AppointmentID > 0);
        Assert.NotNull(successfulScheduleDto.RetakeTestApplicationID);

        // Verify: The appointment and retake application are now cleanly persisted
        await using (var finalContext = _database.CreateContext())
        {
            var finalAppointment = await finalContext.TestAppointments
                .FirstOrDefaultAsync(a => a.TestTypeID == testTypeId && a.AppointmentDate == slot);

            Assert.NotNull(finalAppointment);
            Assert.Equal(applicant.LocalApplicationId, finalAppointment.LocalDrivingLicenseApplicationID);
            Assert.NotNull(finalAppointment.RetakeTestApplicationID);
        }
    }

    // =========================================================================
    // DECORATOR TO TEST REAL SERVICE ROLLBACK AFTER RETAKE APP CREATION
    // =========================================================================

    private sealed class RollbackTriggerAppointmentRepository(ITestAppointmentRepository inner) : ITestAppointmentRepository
    {
        public Task<TestAppointment?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => inner.GetByIdAsync(id, cancellationToken);
        public Task<TestAppointment?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) => inner.GetForUpdateAsync(id, cancellationToken);
        public Task<List<TestAppointment>> GetAllAsync(CancellationToken cancellationToken = default) => inner.GetAllAsync(cancellationToken);
        public Task<List<TestAppointment>> GetByLocalDrivingLicenseApplicationIdAsync(int localAppId, CancellationToken cancellationToken = default) => inner.GetByLocalDrivingLicenseApplicationIdAsync(localAppId, cancellationToken);
        public Task<List<TestAppointment>> GetByTestTypeIdAsync(TestTypeEnum testType, CancellationToken cancellationToken = default) => inner.GetByTestTypeIdAsync(testType, cancellationToken);
        public Task<List<TestAppointment>> GetByCreatedUserIdAsync(int userId, CancellationToken cancellationToken = default) => inner.GetByCreatedUserIdAsync(userId, cancellationToken);
        public Task<TestAppointment?> GetScheduleInfoAsync(int appointmentId, CancellationToken cancellationToken = default) => inner.GetScheduleInfoAsync(appointmentId, cancellationToken);
        public Task<HashSet<int>> GetPassedTestTypeIdsAsync(int localAppId, CancellationToken cancellationToken = default) => inner.GetPassedTestTypeIdsAsync(localAppId, cancellationToken);
        public Task<bool> HasUserConflictAsync(int userId, DateTime dateTime, int? excludeAppointmentId = null, CancellationToken cancellationToken = default) => inner.HasUserConflictAsync(userId, dateTime, excludeAppointmentId, cancellationToken);
        public Task<bool> HasLocalApplicationConflictAsync(int localAppId, DateTime dateTime, int? excludeAppointmentId = null, CancellationToken cancellationToken = default) => inner.HasLocalApplicationConflictAsync(localAppId, dateTime, excludeAppointmentId, cancellationToken);
        public Task<bool> IsSlotOccupiedAsync(int testTypeId, DateTime appointmentDate, int? excludeAppointmentId = null, CancellationToken cancellationToken = default) => inner.IsSlotOccupiedAsync(testTypeId, appointmentDate, excludeAppointmentId, cancellationToken);
        public Task<bool> HasApplicantSameDayConflictAsync(int applicantPersonId, DateTime appointmentDate, int? excludeAppointmentId = null, CancellationToken cancellationToken = default) => inner.HasApplicantSameDayConflictAsync(applicantPersonId, appointmentDate, excludeAppointmentId, cancellationToken);
        public Task<int?> GetApplicantPersonIdByLocalAppIdAsync(int localAppId, CancellationToken cancellationToken = default) => inner.GetApplicantPersonIdByLocalAppIdAsync(localAppId, cancellationToken);
        public Task<bool> AcquireSchedulingLocksAsync(int applicantPersonId, int testTypeId, DateTime appointmentDate, int timeoutMs = 5000, CancellationToken cancellationToken = default) => inner.AcquireSchedulingLocksAsync(applicantPersonId, testTypeId, appointmentDate, timeoutMs, cancellationToken);
        public Task<bool> IsAppointmentAlreadyScheduledAsync(int localAppId, int testTypeId, CancellationToken cancellationToken = default) => inner.IsAppointmentAlreadyScheduledAsync(localAppId, testTypeId, cancellationToken);
        public Task<AppStatus?> GetApplicationStatusAsync(int localAppId, CancellationToken cancellationToken = default) => inner.GetApplicationStatusAsync(localAppId, cancellationToken);
        public Task<int> GetTrialCountAsync(int localAppId, int testTypeId, CancellationToken cancellationToken = default) => inner.GetTrialCountAsync(localAppId, testTypeId, cancellationToken);
        public Task<bool> LockLocalApplicationForSchedulingAsync(int localAppId, CancellationToken cancellationToken = default) => inner.LockLocalApplicationForSchedulingAsync(localAppId, cancellationToken);
        public void Delete(TestAppointment appointment) => inner.Delete(appointment);
        public Task<(HashSet<DateTime> OccupiedSlots, HashSet<DateTime> ApplicantAppointmentDates, DateTime? LastTestDate)> GetAvailabilityDataAsync(int testTypeId, int applicantPersonId, int localAppId, DateTime rangeStart, DateTime rangeEnd, CancellationToken cancellationToken = default) => inner.GetAvailabilityDataAsync(testTypeId, applicantPersonId, localAppId, rangeStart, rangeEnd, cancellationToken);
        public Task<List<int>> GetPendingNoShowAppointmentIdsAsync(DateTime cutoffJordanTime, int batchSize, CancellationToken cancellationToken = default) => inner.GetPendingNoShowAppointmentIdsAsync(cutoffJordanTime, batchSize, cancellationToken);
        public Task<bool> AcquireAppointmentLockAsync(int appointmentId, int timeoutMs = 5000, CancellationToken cancellationToken = default) => inner.AcquireAppointmentLockAsync(appointmentId, timeoutMs, cancellationToken);
        public Task<Application.DTOs.TestAppointmentDTO.ExaminationAttemptStats> GetAttemptStatsAsync(int localAppId, int testTypeId, CancellationToken cancellationToken = default) => inner.GetAttemptStatsAsync(localAppId, testTypeId, cancellationToken);
        public Task<List<TestAppointment>> GetActiveScheduledByLocalAppIdForUpdateAsync(int localAppId, CancellationToken cancellationToken = default) => inner.GetActiveScheduledByLocalAppIdForUpdateAsync(localAppId, cancellationToken);

        public async Task AddAsync(TestAppointment appointment, CancellationToken cancellationToken = default)
        {
            await inner.AddAsync(appointment, cancellationToken);
            throw new InvalidOperationException("Deliberate failure triggered after retake application creation.");
        }
    }
}
