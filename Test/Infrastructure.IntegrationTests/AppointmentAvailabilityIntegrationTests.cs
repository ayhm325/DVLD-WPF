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

public sealed class AppointmentAvailabilityIntegrationTests : IAsyncLifetime
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

    private static DateTime GetNextWeekday(DayOfWeek dayOfWeek)
    {
        var currentJordan = TestAppointmentValidator.GetCurrentJordanTime().Date.AddDays(7);
        while (currentJordan.DayOfWeek != dayOfWeek)
        {
            currentJordan = currentJordan.AddDays(1);
        }
        return currentJordan;
    }

    [Fact]
    public async Task Scenario1_ReturnsOnlyGenuinelyAvailableSlots()
    {
        // 1. The endpoint returns only genuinely available slots.
        var applicant = await SeedApplicantAsync("Avail1");
        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        var sunday = GetNextWeekday(DayOfWeek.Sunday);
        var tuesday = sunday.AddDays(2);

        var result = await service.GetAvailabilityAsync(
            applicant.LocalApplicationId,
            (int)TestTypeEnum.Theory,
            sunday,
            tuesday);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.AvailableDays.Count); // Sun, Mon, Tue
        Assert.All(result.Value.AvailableDays, day =>
        {
            Assert.Equal(12, day.Slots.Count);
            Assert.All(day.Slots, slot =>
            {
                var time = TimeSpan.Parse(slot.StartTime);
                Assert.True(TestAppointmentValidator.IsValidSlotTime(time));
            });
        });
    }

    [Fact]
    public async Task Scenario2_OccupiedSlotsAreExcluded()
    {
        // 2. Occupied slots are excluded.
        var applicant = await SeedApplicantAsync("Avail2");
        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        var sunday = GetNextWeekday(DayOfWeek.Sunday);
        var bookedSlot = sunday.Add(new TimeSpan(8, 0, 0));

        // Book slot 08:00 via real ScheduleAsync
        var scheduleResult = await service.ScheduleAsync(
            applicant.LocalApplicationId,
            (int)TestTypeEnum.Theory,
            bookedSlot);
        Assert.True(scheduleResult.IsSuccess);

        // Another applicant checks availability for Theory
        var applicantB = await SeedApplicantAsync("Avail2B");
        await using var contextB = _database.CreateContext();
        var serviceB = CreateService(contextB, applicantB.UserId);

        var result = await serviceB.GetAvailabilityAsync(
            applicantB.LocalApplicationId,
            (int)TestTypeEnum.Theory,
            sunday,
            sunday);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.AvailableDays);
        var slots = result.Value.AvailableDays[0].Slots;
        Assert.Equal(11, slots.Count);
        Assert.DoesNotContain(slots, s => s.StartTime == "08:00");
        Assert.Contains(slots, s => s.StartTime == "08:40");
    }

    [Fact]
    public async Task Scenario3_DifferentTestTypesMayShareSameTime()
    {
        // 3. Different test types may share a time.
        var applicantA = await SeedApplicantAsync("Avail3A");
        await using var contextA = _database.CreateContext();
        var serviceA = CreateService(contextA, applicantA.UserId);

        var sunday = GetNextWeekday(DayOfWeek.Sunday);
        var bookedSlot = sunday.Add(new TimeSpan(8, 0, 0));

        // Applicant A books Theory at 08:00
        var schedA = await serviceA.ScheduleAsync(
            applicantA.LocalApplicationId,
            (int)TestTypeEnum.Theory,
            bookedSlot);
        Assert.True(schedA.IsSuccess);

        // Applicant B has passed Theory and wants to check Written availability
        var applicantB = await SeedApplicantAsync("Avail3B");
        await using var contextB = _database.CreateContext();

        // Pass Theory for Applicant B
        var theoryAppt = new TestAppointment
        {
            TestTypeID = (int)TestTypeEnum.Theory,
            LocalDrivingLicenseApplicationID = applicantB.LocalApplicationId,
            AppointmentDate = sunday.AddDays(-7),
            PaidFees = 10m,
            CreatedByUserID = applicantB.UserId,
            IsLocked = true
        };
        contextB.TestAppointments.Add(theoryAppt);
        await contextB.SaveChangesAsync();

        var passedTest = new Test
        {
            TestAppointmentID = theoryAppt.TestAppointmentID,
            TestResult = true,
            Notes = "Passed",
            CreatedByUserID = applicantB.UserId
        };
        contextB.Tests.Add(passedTest);
        await contextB.SaveChangesAsync();

        // Check Written availability for Applicant B on Sunday
        var serviceB = CreateService(contextB, applicantB.UserId);
        var result = await serviceB.GetAvailabilityAsync(
            applicantB.LocalApplicationId,
            (int)TestTypeEnum.Written,
            sunday,
            sunday);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.AvailableDays);
        var slots = result.Value.AvailableDays[0].Slots;
        // Slot 08:00 MUST still be available for Written even though Theory is booked!
        Assert.Contains(slots, s => s.StartTime == "08:00");
    }

    [Fact]
    public async Task Scenario4_ApplicantOtherAppointmentBlocksAllSlotsOnThatDay()
    {
        // 4. An applicant's other appointment blocks all slots on that day.
        var applicant = await SeedApplicantAsync("Avail4");
        var secondAppId = await CreateAdditionalApplicationForPersonAsync(applicant.PersonId, applicant.UserId);

        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        var sunday = GetNextWeekday(DayOfWeek.Sunday);
        var monday = sunday.AddDays(1);
        var bookedSlot = sunday.Add(new TimeSpan(9, 20, 0));

        // Schedule Theory on application 1 for Sunday
        var sched = await service.ScheduleAsync(
            applicant.LocalApplicationId,
            (int)TestTypeEnum.Theory,
            bookedSlot);
        Assert.True(sched.IsSuccess);

        // Now check availability for second application on Sunday & Monday
        var result = await service.GetAvailabilityAsync(
            secondAppId,
            (int)TestTypeEnum.Theory,
            sunday,
            monday);

        Assert.True(result.IsSuccess);
        // Sunday must be completely blocked (no slots available for Sunday)
        Assert.Single(result.Value!.AvailableDays);
        Assert.Equal(monday.ToString("yyyy-MM-dd"), result.Value.AvailableDays[0].Date);
    }

    [Fact]
    public async Task Scenario5_WeekendsAreExcluded()
    {
        // 5. Weekends are excluded.
        var applicant = await SeedApplicantAsync("Avail5");
        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        var friday = GetNextWeekday(DayOfWeek.Friday);
        var saturday = friday.AddDays(1);

        var result = await service.GetAvailabilityAsync(
            applicant.LocalApplicationId,
            (int)TestTypeEnum.Theory,
            friday,
            saturday);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.AvailableDays);
    }

    [Fact]
    public async Task Scenario6_NoDaysWithoutAvailableSlotsAreReturned()
    {
        // 6. No days without available slots are returned.
        var applicant = await SeedApplicantAsync("Avail6");
        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        var sunday = GetNextWeekday(DayOfWeek.Sunday);

        // Book all 12 slots on Sunday for Theory using other applicants
        foreach (var slotTime in TestAppointmentValidator.ValidSlotTimes)
        {
            var other = await SeedApplicantAsync("Other");
            await using var otherContext = _database.CreateContext();
            var otherService = CreateService(otherContext, other.UserId);
            var res = await otherService.ScheduleAsync(
                other.LocalApplicationId,
                (int)TestTypeEnum.Theory,
                sunday.Add(slotTime));
            Assert.True(res.IsSuccess);
        }

        // Now applicant checks availability for Sunday through Monday
        var monday = sunday.AddDays(1);
        var result = await service.GetAvailabilityAsync(
            applicant.LocalApplicationId,
            (int)TestTypeEnum.Theory,
            sunday,
            monday);

        Assert.True(result.IsSuccess);
        // Sunday has 0 available slots, so Sunday MUST NOT be returned!
        Assert.Single(result.Value!.AvailableDays);
        Assert.Equal(monday.ToString("yyyy-MM-dd"), result.Value.AvailableDays[0].Date);
        Assert.All(result.Value.AvailableDays, d => Assert.NotEmpty(d.Slots));
    }

    [Fact]
    public async Task Scenario7_InvalidOrIneligibleRequests_ReturnExpectedErrors()
    {
        // 7. Invalid or ineligible requests return the expected API errors.
        var applicant = await SeedApplicantAsync("Avail7");
        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        var sunday = GetNextWeekday(DayOfWeek.Sunday);

        // A: Application does not exist -> NotFound
        var resNotFound = await service.GetAvailabilityAsync(
            999999,
            (int)TestTypeEnum.Theory,
            sunday,
            sunday.AddDays(1));
        Assert.True(resNotFound.IsFailure);
        Assert.Equal(ErrorType.NotFound, resNotFound.ErrorType);

        // B: Ineligible test type (Written before Theory passed) -> Conflict
        var resConflict = await service.GetAvailabilityAsync(
            applicant.LocalApplicationId,
            (int)TestTypeEnum.Written,
            sunday,
            sunday.AddDays(1));
        Assert.True(resConflict.IsFailure);
        Assert.Equal(ErrorType.Conflict, resConflict.ErrorType);

        // C: StartDate > EndDate -> Validation
        var resValidation = await service.GetAvailabilityAsync(
            applicant.LocalApplicationId,
            (int)TestTypeEnum.Theory,
            sunday.AddDays(5),
            sunday);
        Assert.True(resValidation.IsFailure);
        Assert.Equal(ErrorType.Validation, resValidation.ErrorType);
    }

    [Fact]
    public async Task Scenario8_JordanDateHandlingRemainsCorrect()
    {
        // 8. Jordan date handling remains correct.
        var applicant = await SeedApplicantAsync("Avail8");
        await using var context = _database.CreateContext();
        var service = CreateService(context, applicant.UserId);

        var sunday = GetNextWeekday(DayOfWeek.Sunday);

        // Pass UTC DateTime equivalent to Sunday Jordan 00:00:00 (which is Saturday 21:00 UTC)
        var jordanMidnight = DateTime.SpecifyKind(sunday, DateTimeKind.Unspecified);
        var utcEquivalent = TimeZoneInfo.ConvertTimeToUtc(jordanMidnight, TestAppointmentValidator.JordanTimeZone);

        var result = await service.GetAvailabilityAsync(
            applicant.LocalApplicationId,
            (int)TestTypeEnum.Theory,
            utcEquivalent,
            utcEquivalent);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.AvailableDays);
        // The resolved date string must match the Jordan facility calendar date (Sunday)
        Assert.Equal(sunday.ToString("yyyy-MM-dd"), result.Value.AvailableDays[0].Date);
        Assert.Equal(12, result.Value.AvailableDays[0].Slots.Count);
    }
}
