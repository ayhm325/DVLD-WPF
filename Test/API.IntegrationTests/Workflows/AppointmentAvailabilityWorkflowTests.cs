using API.IntegrationTests.Infrastructure;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;
using DVLD.Contracts.TestAppointment;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace API.IntegrationTests.Workflows;

public sealed class AppointmentAvailabilityWorkflowTests
{
    private sealed record SeedData(
        int PersonId,
        int UserId,
        int LicenseClassId,
        int ApplicationId,
        int LocalApplicationId);

    private static DateTime GetNextWeekday(DayOfWeek dayOfWeek)
    {
        var currentJordan = new global::Infrastructure.Services.JordanClock().Now.Date.AddDays(7);
        while (currentJordan.DayOfWeek != dayOfWeek)
        {
            currentJordan = currentJordan.AddDays(1);
        }
        return currentJordan;
    }

    private static void ConfigureStaffClient(HttpClient client, int userId)
    {
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Username", "staff_test");
        client.DefaultRequestHeaders.Add("X-Test-FullName", "Staff Test");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Staff");
    }

    private static async Task<SeedData> SeedScenarioAsync(SqlServerApiWebApplicationFactory factory)
    {
        await using var context = factory.CreateDbContext();
        await context.Database.OpenConnectionAsync();

        try
        {
            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT ApplicationTypes ON;");
            var hasAppTypes = await context.ApplicationTypes.AnyAsync();
            if (!hasAppTypes)
            {
                await context.Database.ExecuteSqlRawAsync("""
                    INSERT INTO ApplicationTypes (ApplicationTypeId, ApplicationTypeTitle, ApplicationFees)
                    VALUES (1, N'New Local Driving License', 20), (7, N'Retake Test', 5);
                    """);
            }

            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT ApplicationTypes OFF;");

            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT TestTypes ON;");
            var hasTestTypes = await context.TestTypes.AnyAsync();
            if (!hasTestTypes)
            {
                await context.Database.ExecuteSqlRawAsync("""
                    INSERT INTO TestTypes (TestTypeId, TestTypeTitle, TestTypeDescription, TestTypeFees)
                    VALUES
                    (1, N'Theory Test', N'Theory Exam', 10),
                    (2, N'Written Test', N'Written Exam', 15),
                    (3, N'Practical Test', N'Practical Exam', 20);
                    """);
            }
            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT TestTypes OFF;");
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }

        var uid = Guid.NewGuid().ToString("N");

        var country = new Country { CountryName = $"Country_{uid[..8]}" };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var person = new Person
        {
            NationalNo = $"N{uid[..14]}",
            FirstName = "Avail",
            SecondName = "Http",
            LastName = "Test",
            DateOfBirth = new DateTime(1995, 1, 1),
            Gender = Gender.Male,
            Address = "Amman, Jordan",
            Phone = $"07{uid[..8]}",
            Email = $"avail_{uid}@dvld.jo",
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

        return new SeedData(
            person.PersonId,
            user.UserId,
            licenseClass.LicenseClassID,
            application.ApplicationID,
            localApp.LocalDrivingLicenseApplicationID);
    }

    [Fact]
    public async Task GetAvailability_OverRealHttpPipeline_WithRealDatabase_ReturnsAvailableWorkingDaysAndSlots()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedScenarioAsync(factory);

        using var client = factory.CreateClient();
        ConfigureStaffClient(client, seed.UserId);

        var sunday = GetNextWeekday(DayOfWeek.Sunday);
        var tuesday = sunday.AddDays(2);

        var url = $"/api/TestAppointments/availability?localAppId={seed.LocalApplicationId}&testTypeId=1&startDate={sunday:yyyy-MM-dd}&endDate={tuesday:yyyy-MM-dd}";
        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AppointmentAvailabilityResponse>();
        Assert.NotNull(result);
        Assert.Equal(seed.LocalApplicationId, result.LocalAppId);
        Assert.Equal(1, result.TestTypeId);
        Assert.Equal(3, result.AvailableDays.Count); // Sunday, Monday, Tuesday
        Assert.All(result.AvailableDays, day =>
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
    public async Task GetAvailability_OverRealHttpPipeline_AfterSchedulingSlot_ExcludesBookedSlot()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seedA = await SeedScenarioAsync(factory);
        var seedB = await SeedScenarioAsync(factory);

        using var client = factory.CreateClient();
        ConfigureStaffClient(client, seedA.UserId);

        var sunday = GetNextWeekday(DayOfWeek.Sunday);
        var slotToBook = sunday.Add(new TimeSpan(8, 0, 0));

        // 1. Applicant A schedules Sunday 08:00 via real HTTP POST /api/TestAppointments/schedule
        var scheduleRequest = new ScheduleTestRequest
        {
            LocalDrivingLicenseApplicationId = seedA.LocalApplicationId,
            TestTypeId = 1,
            AppointmentDate = new DateTimeOffset(DateTime.SpecifyKind(slotToBook, DateTimeKind.Unspecified), Application.Common.Time.JordanTimeZoneProvider.TimeZone.GetUtcOffset(DateTime.SpecifyKind(slotToBook, DateTimeKind.Unspecified)))
        };

        using var scheduleResponse = await client.PostAsJsonAsync(
            "/api/TestAppointments/schedule",
            scheduleRequest);

        Assert.Equal(HttpStatusCode.NoContent, scheduleResponse.StatusCode);

        // 2. Query availability for Applicant A -> Must return 409 Conflict because Applicant A already has an active appointment
        var urlA = $"/api/TestAppointments/availability?localAppId={seedA.LocalApplicationId}&testTypeId=1&startDate={sunday:yyyy-MM-dd}&endDate={sunday:yyyy-MM-dd}";
        using var availResponseA = await client.GetAsync(urlA);
        Assert.Equal(HttpStatusCode.Conflict, availResponseA.StatusCode);

        // 3. Query availability for Applicant B -> Must return 200 OK, but slot 08:00 must be excluded
        var urlB = $"/api/TestAppointments/availability?localAppId={seedB.LocalApplicationId}&testTypeId=1&startDate={sunday:yyyy-MM-dd}&endDate={sunday:yyyy-MM-dd}";
        using var availResponseB = await client.GetAsync(urlB);
        Assert.Equal(HttpStatusCode.OK, availResponseB.StatusCode);

        var resultB = await availResponseB.Content.ReadFromJsonAsync<AppointmentAvailabilityResponse>();
        Assert.NotNull(resultB);
        Assert.Single(resultB.AvailableDays);

        var slots = resultB.AvailableDays[0].Slots;
        Assert.Equal(11, slots.Count);
        Assert.DoesNotContain(slots, s => s.StartTime == "08:00");
        Assert.Contains(slots, s => s.StartTime == "08:40");
    }
}
