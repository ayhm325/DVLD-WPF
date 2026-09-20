using API.IntegrationTests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using DVLD.Contracts.DetainedLicense;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.IntegrationTests.Workflows;

public sealed class ReleaseDetainedLicenseWorkflowTests
{
    [Fact]
    public async Task ReleaseDetainedLicense_WhenValid_CreatesReleaseApplicationAndRestoresLicense()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedDetainedLicenseScenarioAsync(factory);
        using var client = AuthClient(factory, seed.UserId);

        var response = await client.PostAsJsonAsync("/api/DetainedLicenses/release",
            new ReleaseDetainedLicenseRequest { DetainId = seed.DetainId });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var context = factory.CreateDbContext();

        var detention = await context.DetainedLicenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DetainID == seed.DetainId);

        Assert.NotNull(detention);
        Assert.True(detention.IsReleased);
        Assert.NotNull(detention.ReleaseDate);
        Assert.Equal(seed.UserId, detention.ReleasedByUserID);
        Assert.True(detention.ReleaseApplicationID > 0);

        var application = await context.Applications.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ApplicationID == detention.ReleaseApplicationID);

        Assert.NotNull(application);
        Assert.Equal(seed.PersonId, application.ApplicantPersonID);
        Assert.Equal(5, application.ApplicationTypeID);
        Assert.Equal(AppStatus.Completed, application.ApplicationStatus);
        Assert.Equal(seed.UserId, application.CreatedByUserID);
        Assert.Equal(seed.ReleaseApplicationFees, application.PaidFees);

        var license = await context.Licenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.LicenseID == seed.LicenseId);

        Assert.NotNull(license);
        Assert.True(license.IsActive);
    }

    [Fact]
    public async Task ReleaseDetainedLicense_WhenAlreadyReleased_ReturnsConflictAndDoesNotCreateAnotherApplication()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedDetainedLicenseScenarioAsync(factory, isReleased: true);
        using var client = AuthClient(factory, seed.UserId);

        var response = await client.PostAsJsonAsync("/api/DetainedLicenses/release",
            new ReleaseDetainedLicenseRequest { DetainId = seed.DetainId });

        await AssertProblemDetailsAsync(response, HttpStatusCode.Conflict,
            "Conflict", "License is already released.");

        await using var context = factory.CreateDbContext();

        var detention = await context.DetainedLicenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DetainID == seed.DetainId);

        Assert.NotNull(detention);
        Assert.True(detention.IsReleased);
        Assert.NotNull(detention.ReleaseDate);
        Assert.Null(detention.ReleasedByUserID);
        Assert.Null(detention.ReleaseApplicationID);

        var license = await context.Licenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.LicenseID == seed.LicenseId);

        Assert.NotNull(license);
        Assert.False(license.IsActive);

        Assert.Empty(await context.Applications.AsNoTracking()
            .Where(x => x.ApplicantPersonID == seed.PersonId && x.ApplicationTypeID == 5)
            .ToListAsync());
    }

    [Fact]
    public async Task ReleaseDetainedLicense_WhenDetentionDoesNotExist_ReturnsNotFound()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedDetainedLicenseScenarioAsync(factory);
        using var client = AuthClient(factory, seed.UserId);

        var response = await client.PostAsJsonAsync("/api/DetainedLicenses/release",
            new ReleaseDetainedLicenseRequest { DetainId = seed.DetainId + 999999 });

        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound,
            "Resource not found", "Detained license not found.");
    }

    [Fact]
    public async Task ReleaseDetainedLicense_WhenReleaseApplicationTypeDoesNotExist_ReturnsNotFoundAndDoesNotChangeState()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedDetainedLicenseScenarioAsync(
            factory, includeReleaseApplicationType: false);

        using var client = AuthClient(factory, seed.UserId);

        var response = await client.PostAsJsonAsync("/api/DetainedLicenses/release",
            new ReleaseDetainedLicenseRequest { DetainId = seed.DetainId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var context = factory.CreateDbContext();

        var detention = await context.DetainedLicenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DetainID == seed.DetainId);

        Assert.NotNull(detention);
        Assert.False(detention.IsReleased);
        Assert.Null(detention.ReleaseDate);
        Assert.Null(detention.ReleasedByUserID);
        Assert.Null(detention.ReleaseApplicationID);

        var license = await context.Licenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.LicenseID == seed.LicenseId);

        Assert.NotNull(license);
        Assert.False(license.IsActive);

        Assert.Empty(await context.Applications.AsNoTracking()
            .Where(x => x.ApplicantPersonID == seed.PersonId && x.ApplicationTypeID == 5)
            .ToListAsync());
    }

    [Fact]
    public async Task ReleaseDetainedLicense_WhenAnotherActiveLicenseExists_DoesNotReactivateReleasedLicense()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedDetainedLicenseScenarioAsync(
            factory, createAnotherActiveLicense: true);

        using var client = AuthClient(factory, seed.UserId);

        var response = await client.PostAsJsonAsync("/api/DetainedLicenses/release",
            new ReleaseDetainedLicenseRequest { DetainId = seed.DetainId });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var context = factory.CreateDbContext();

        var detention = await context.DetainedLicenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DetainID == seed.DetainId);

        Assert.NotNull(detention);
        Assert.True(detention.IsReleased);
        Assert.NotNull(detention.ReleaseApplicationID);

        var releasedLicense = await context.Licenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.LicenseID == seed.LicenseId);

        Assert.NotNull(releasedLicense);
        Assert.False(releasedLicense.IsActive);

        var otherLicense = await context.Licenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.LicenseID == seed.AnotherLicenseId);

        Assert.NotNull(otherLicense);
        Assert.True(otherLicense.IsActive);
    }

    [Fact]
    public async Task ReleaseDetainedLicense_WhenLicenseIsExpired_DoesNotReactivateLicense()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedDetainedLicenseScenarioAsync(factory, licenseExpired: true);
        using var client = AuthClient(factory, seed.UserId);

        var response = await client.PostAsJsonAsync("/api/DetainedLicenses/release",
            new ReleaseDetainedLicenseRequest { DetainId = seed.DetainId });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var context = factory.CreateDbContext();

        var detention = await context.DetainedLicenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DetainID == seed.DetainId);

        Assert.NotNull(detention);
        Assert.True(detention.IsReleased);
        Assert.NotNull(detention.ReleaseApplicationID);

        var license = await context.Licenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.LicenseID == seed.LicenseId);

        Assert.NotNull(license);
        Assert.False(license.IsActive);
        Assert.True(license.ExpirationDate <= DateTime.UtcNow);
    }

    [Fact]
    public async Task ReleaseDetainedLicense_WhenCompletionFails_RollsBackEntireWorkflow()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedDetainedLicenseScenarioAsync(factory);

        await using (var setupContext = factory.CreateDbContext())
        {
            await setupContext.Database.OpenConnectionAsync();

            try
            {
                await setupContext.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE Applications
                    ADD CONSTRAINT [CK_Applications_IntegrationTest_BlockCompletion]
                    CHECK (ApplicationStatus <> 3)
                    """);
            }
            finally
            {
                await setupContext.Database.CloseConnectionAsync();
            }
        }

        try
        {
            using var client = AuthClient(factory, seed.UserId);

            var response = await client.PostAsJsonAsync("/api/DetainedLicenses/release",
                new ReleaseDetainedLicenseRequest { DetainId = seed.DetainId });

            await AssertProblemDetailsAsync(
                response,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                "The server could not complete the request.",
                requireProblemContentType: false);

            await using var context = factory.CreateDbContext();

            var detention = await context.DetainedLicenses.AsNoTracking()
                .SingleOrDefaultAsync(x => x.DetainID == seed.DetainId);

            Assert.NotNull(detention);
            Assert.False(detention.IsReleased);
            Assert.Null(detention.ReleaseDate);
            Assert.Null(detention.ReleasedByUserID);
            Assert.Null(detention.ReleaseApplicationID);

            var license = await context.Licenses.AsNoTracking()
                .SingleOrDefaultAsync(x => x.LicenseID == seed.LicenseId);

            Assert.NotNull(license);
            Assert.False(license.IsActive);

            Assert.Empty(await context.Applications.AsNoTracking()
                .Where(x => x.ApplicantPersonID == seed.PersonId && x.ApplicationTypeID == 5)
                .ToListAsync());
        }
        finally
        {
            await RemoveCompletionConstraintAsync(factory);
        }
    }

    [Fact]
    public async Task ReleaseDetainedLicense_WhenTwoRequestsAreSentConcurrently_AllowsOnlyOneRelease()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedDetainedLicenseScenarioAsync(factory);
        using var clientA = AuthClient(factory, seed.UserId);
        using var clientB = AuthClient(factory, seed.UserId);

        var request = new ReleaseDetainedLicenseRequest { DetainId = seed.DetainId };
        var gate = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> SendAsync(HttpClient client)
        {
            await gate.Task;
            return await client.PostAsJsonAsync(
                "/api/DetainedLicenses/release", request);
        }

        var taskA = SendAsync(clientA);
        var taskB = SendAsync(clientB);
        gate.SetResult();

        var responses = await Task.WhenAll(taskA, taskB);

        Assert.Equal(1, responses.Count(x => x.StatusCode == HttpStatusCode.NoContent));
        Assert.Equal(1, responses.Count(x => x.StatusCode == HttpStatusCode.Conflict));

        var conflict = responses.Single(x => x.StatusCode == HttpStatusCode.Conflict);

        await AssertProblemDetailsAsync(
            conflict,
            HttpStatusCode.Conflict,
            "Conflict",
            "License is already released.");

        await using var context = factory.CreateDbContext();

        var detention = await context.DetainedLicenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DetainID == seed.DetainId);

        Assert.NotNull(detention);
        Assert.True(detention.IsReleased);
        Assert.NotNull(detention.ReleaseApplicationID);

        var applications = await context.Applications.AsNoTracking()
            .Where(x => x.ApplicantPersonID == seed.PersonId && x.ApplicationTypeID == 5)
            .ToListAsync();

        Assert.Single(applications);
        Assert.Equal(AppStatus.Completed, applications[0].ApplicationStatus);
    }

    private static HttpClient AuthClient(
        SqlServerApiWebApplicationFactory factory, int userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Username", "integration.test");
        client.DefaultRequestHeaders.Add("X-Test-FullName", "Integration Test User");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Staff");
        return client;
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode status,
        string title,
        string detail,
        bool requireProblemContentType = true)
    {
        Assert.Equal(status, response.StatusCode);

        if (requireProblemContentType)
            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var body = document.RootElement;

        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.Equal(title, body.GetProperty("title").GetString());
        Assert.Equal(detail, body.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            body.GetProperty("instance").GetString()));

        Assert.True(body.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    private static async Task RemoveCompletionConstraintAsync(
        SqlServerApiWebApplicationFactory factory)
    {
        await using var context = factory.CreateDbContext();
        await context.Database.OpenConnectionAsync();

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE Applications
                DROP CONSTRAINT [CK_Applications_IntegrationTest_BlockCompletion]
                """);
        }
        catch { }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task<SeedData> SeedDetainedLicenseScenarioAsync(
        SqlServerApiWebApplicationFactory factory,
        bool includeReleaseApplicationType = true,
        bool createAnotherActiveLicense = false,
        bool licenseExpired = false,
        bool isReleased = false)
    {
        await using var context = factory.CreateDbContext();
        await SeedLookupDataAsync(context, includeReleaseApplicationType);

        var country = new Country
        {
            CountryName = $"Integration Country {Guid.NewGuid():N}"
        };

        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var userPerson = new Person
        {
            NationalNo = CreateNationalNumber(),
            FirstName = "Integration",
            SecondName = "Test",
            LastName = "User",
            DateOfBirth = new(1990, 1, 1),
            Gender = Gender.Male,
            Address = "Integration Test Address",
            Phone = CreatePhone(),
            Email = $"user-{Guid.NewGuid():N}@test.local",
            NationalityCountryID = country.CountryId
        };

        var applicantPerson = new Person
        {
            NationalNo = CreateNationalNumber(),
            FirstName = "Release",
            SecondName = "Integration",
            LastName = "Applicant",
            DateOfBirth = new(1990, 1, 1),
            Gender = Gender.Male,
            Address = "Release Applicant Address",
            Phone = CreatePhone(),
            Email = $"release-{Guid.NewGuid():N}@test.local",
            NationalityCountryID = country.CountryId
        };

        context.People.AddRange(userPerson, applicantPerson);
        await context.SaveChangesAsync();

        var user = new User
        {
            PersonId = userPerson.PersonId,
            UserName = $"integration-{Guid.NewGuid():N}",
            Password = "TestPassword",
            IsActive = true,
            Role = UserRole.Staff
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var driver = new Driver
        {
            PersonID = applicantPerson.PersonId,
            CreatedByUserID = user.UserId,
            CreatedDate = DateTime.UtcNow
        };

        context.Drivers.Add(driver);
        await context.SaveChangesAsync();

        var originalApplication = new ApplicationD
        {
            ApplicantPersonID = applicantPerson.PersonId,
            ApplicationDate = DateTime.UtcNow.AddYears(-1),
            ApplicationTypeID = 1,
            ApplicationStatus = AppStatus.New,
            LastStatusDate = DateTime.UtcNow.AddYears(-1),
            PaidFees = 20m,
            CreatedByUserID = user.UserId
        };

        context.Applications.Add(originalApplication);
        await context.SaveChangesAsync();

        var license = new License
        {
            ApplicationID = originalApplication.ApplicationID,
            DriverID = driver.DriverID,
            LicenseClass = 1,
            IssueDate = DateTime.UtcNow.AddYears(-1),
            ExpirationDate = licenseExpired
                ? DateTime.UtcNow.AddDays(-1)
                : DateTime.UtcNow.AddYears(4),
            Notes = "Detained integration-test license",
            PaidFees = 100m,
            IsActive = false,
            IssueReason = IssueReason.FirstTime,
            CreatedByUserID = user.UserId
        };

        context.Licenses.Add(license);
        await context.SaveChangesAsync();

        License? anotherLicense = null;

        if (createAnotherActiveLicense)
        {
            var anotherApplication = new ApplicationD
            {
                ApplicantPersonID = applicantPerson.PersonId,
                ApplicationDate = DateTime.UtcNow.AddMonths(-6),
                ApplicationTypeID = 1,
                ApplicationStatus = AppStatus.New,
                LastStatusDate = DateTime.UtcNow.AddMonths(-6),
                PaidFees = 20m,
                CreatedByUserID = user.UserId
            };

            context.Applications.Add(anotherApplication);
            await context.SaveChangesAsync();

            anotherLicense = new License
            {
                ApplicationID = anotherApplication.ApplicationID,
                DriverID = driver.DriverID,
                LicenseClass = 1,
                IssueDate = DateTime.UtcNow.AddMonths(-6),
                ExpirationDate = DateTime.UtcNow.AddYears(4),
                Notes = "Another active integration-test license",
                PaidFees = 100m,
                IsActive = true,
                IssueReason = IssueReason.FirstTime,
                CreatedByUserID = user.UserId
            };

            context.Licenses.Add(anotherLicense);
            await context.SaveChangesAsync();
        }

        var detention = new DetainedLicense
        {
            LicenseID = license.LicenseID,
            DetainDate = DateTime.UtcNow.AddDays(-1),
            FineFees = 50m,
            CreatedByUserID = user.UserId,
            IsReleased = isReleased,
            ReleaseDate = isReleased ? DateTime.UtcNow.AddHours(-1) : null
        };

        context.DetainedLicenses.Add(detention);
        await context.SaveChangesAsync();

        return new(
            user.UserId,
            applicantPerson.PersonId,
            driver.DriverID,
            license.LicenseID,
            detention.DetainID,
            anotherLicense?.LicenseID ?? 0,
            5m);
    }

    private static async Task SeedLookupDataAsync(
        DVLDDbContext context,
        bool includeReleaseApplicationType)
    {
        await context.Database.OpenConnectionAsync();

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT ApplicationTypes ON");

            await context.Database.ExecuteSqlRawAsync(
                includeReleaseApplicationType
                    ? """
                      INSERT INTO ApplicationTypes
                          (ApplicationTypeId, ApplicationTypeTitle, ApplicationFees)
                      VALUES
                          (1, N'New Local Driving License', 20),
                          (5, N'Release Detained License', 5)
                      """
                    : """
                      INSERT INTO ApplicationTypes
                          (ApplicationTypeId, ApplicationTypeTitle, ApplicationFees)
                      VALUES
                          (1, N'New Local Driving License', 20)
                      """);

            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT ApplicationTypes OFF");

            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT LicenseClasses ON");

            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO LicenseClasses
                    (LicenseClassID, ClassName, ClassDescription,
                     MinimumAllowedAge, DefaultValidityLength, ClassFees)
                VALUES
                    (1, N'Integration Class',
                     N'Integration test license class',
                     18, 5, 100)
                """);

            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT LicenseClasses OFF");
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static string CreateNationalNumber() =>
        Guid.NewGuid().ToString("N")[..18];

    private static string CreatePhone() =>
        $"07{Random.Shared.NextInt64(100000000, 999999999)}";

    private sealed record SeedData(
        int UserId,
        int PersonId,
        int DriverId,
        int LicenseId,
        int DetainId,
        int AnotherLicenseId,
        decimal ReleaseApplicationFees);
}
