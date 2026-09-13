using API.IntegrationTests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using DVLD.Contracts.DetainedLicense;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Workflows;

public sealed class ReleaseDetainedLicenseWorkflowTests
{
    [Fact]
    public async Task ReleaseDetainedLicense_WhenValid_CreatesReleaseApplicationAndRestoresLicense()
    {
        await using var factory =
        new SqlServerApiWebApplicationFactory();

        var seed =
        await SeedDetainedLicenseScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var request =
            new ReleaseDetainedLicenseRequest
            {
                DetainId = seed.DetainId
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/DetainedLicenses/release",
                request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var context =
            factory.CreateDbContext();

        var detention =
            await context.DetainedLicenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.DetainID == seed.DetainId);

        Assert.NotNull(detention);

        Assert.True(detention.IsReleased);
        Assert.NotNull(detention.ReleaseDate);
        Assert.Equal(
            seed.UserId,
            detention.ReleasedByUserID);

        Assert.True(
            detention.ReleaseApplicationID > 0);

        var application =
            await context.Applications
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.ApplicationID ==
                        detention.ReleaseApplicationID);

        Assert.NotNull(application);

        Assert.Equal(
            seed.PersonId,
            application.ApplicantPersonID);

        Assert.Equal(
            5,
            application.ApplicationTypeID);

        Assert.Equal(
            AppStatus.Completed,
            application.ApplicationStatus);

        Assert.Equal(
            seed.UserId,
            application.CreatedByUserID);

        Assert.Equal(
            seed.ReleaseApplicationFees,
            application.PaidFees);

        var license =
            await context.Licenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.LicenseID == seed.LicenseId);

        Assert.NotNull(license);

        Assert.True(license.IsActive);
    }

    [Fact]
    public async Task ReleaseDetainedLicense_WhenAlreadyReleased_ReturnsConflictAndDoesNotCreateAnotherApplication()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedDetainedLicenseScenarioAsync(
                factory,
                isReleased: true);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var request =
            new ReleaseDetainedLicenseRequest
            {
                DetainId = seed.DetainId
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/DetainedLicenses/release",
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ConflictResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            "License is already released.",
            body.Error);

        await using var context =
            factory.CreateDbContext();

        var detention =
            await context.DetainedLicenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.DetainID == seed.DetainId);

        Assert.NotNull(detention);

        Assert.True(detention.IsReleased);
        Assert.NotNull(detention.ReleaseDate);
        Assert.Null(detention.ReleasedByUserID);
        Assert.Null(detention.ReleaseApplicationID);

        var license =
            await context.Licenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.LicenseID == seed.LicenseId);

        Assert.NotNull(license);

        Assert.False(license.IsActive);

        var applications =
            await context.Applications
                .AsNoTracking()
                .Where(
                    x =>
                        x.ApplicantPersonID ==
                            seed.PersonId &&
                        x.ApplicationTypeID == 5)
                .ToListAsync();

        Assert.Empty(applications);
    }

    [Fact]
    public async Task ReleaseDetainedLicense_WhenDetentionDoesNotExist_ReturnsNotFound()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedDetainedLicenseScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var request =
            new ReleaseDetainedLicenseRequest
            {
                DetainId = seed.DetainId + 999999
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/DetainedLicenses/release",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            "Detained license not found.",
            body.Error);
    }

    [Fact]
    public async Task ReleaseDetainedLicense_WhenReleaseApplicationTypeDoesNotExist_ReturnsNotFoundAndDoesNotChangeState()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedDetainedLicenseScenarioAsync(
                factory,
                includeReleaseApplicationType: false);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var request =
            new ReleaseDetainedLicenseRequest
            {
                DetainId = seed.DetainId
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/DetainedLicenses/release",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await using var context =
            factory.CreateDbContext();

        var detention =
            await context.DetainedLicenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.DetainID == seed.DetainId);

        Assert.NotNull(detention);

        Assert.False(detention.IsReleased);
        Assert.Null(detention.ReleaseDate);
        Assert.Null(detention.ReleasedByUserID);
        Assert.Null(detention.ReleaseApplicationID);

        var license =
            await context.Licenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.LicenseID == seed.LicenseId);

        Assert.NotNull(license);

        Assert.False(license.IsActive);

        var releaseApplications =
            await context.Applications
                .AsNoTracking()
                .Where(
                    x =>
                        x.ApplicantPersonID ==
                            seed.PersonId &&
                        x.ApplicationTypeID == 5)
                .ToListAsync();

        Assert.Empty(releaseApplications);
    }

    [Fact]
    public async Task ReleaseDetainedLicense_WhenAnotherActiveLicenseExists_DoesNotReactivateReleasedLicense()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedDetainedLicenseScenarioAsync(
                factory,
                createAnotherActiveLicense: true);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var request =
            new ReleaseDetainedLicenseRequest
            {
                DetainId = seed.DetainId
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/DetainedLicenses/release",
                request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var context =
            factory.CreateDbContext();

        var detention =
            await context.DetainedLicenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.DetainID == seed.DetainId);

        Assert.NotNull(detention);
        Assert.True(detention.IsReleased);
        Assert.NotNull(detention.ReleaseApplicationID);

        var releasedLicense =
            await context.Licenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.LicenseID == seed.LicenseId);

        Assert.NotNull(releasedLicense);

        Assert.False(releasedLicense.IsActive);

        var otherLicense =
            await context.Licenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.LicenseID == seed.AnotherLicenseId);

        Assert.NotNull(otherLicense);

        Assert.True(otherLicense.IsActive);
    }

    [Fact]
    public async Task ReleaseDetainedLicense_WhenLicenseIsExpired_DoesNotReactivateLicense()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedDetainedLicenseScenarioAsync(
                factory,
                licenseExpired: true);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var request =
            new ReleaseDetainedLicenseRequest
            {
                DetainId = seed.DetainId
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/DetainedLicenses/release",
                request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var context =
            factory.CreateDbContext();

        var detention =
            await context.DetainedLicenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.DetainID == seed.DetainId);

        Assert.NotNull(detention);

        Assert.True(detention.IsReleased);
        Assert.NotNull(detention.ReleaseApplicationID);

        var license =
            await context.Licenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.LicenseID == seed.LicenseId);

        Assert.NotNull(license);

        Assert.False(license.IsActive);
        Assert.True(
            license.ExpirationDate <= DateTime.UtcNow);
    }

    [Fact]
    public async Task ReleaseDetainedLicense_WhenCompletionFails_RollsBackEntireWorkflow()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedDetainedLicenseScenarioAsync(factory);

        const string constraintName =
            "CK_Applications_IntegrationTest_BlockCompletion";

        await using var setupContext =
            factory.CreateDbContext();

        await setupContext.Database.OpenConnectionAsync();

        try
        {
            var completedStatusValue =
                (int)AppStatus.Completed;

            await setupContext.Database.ExecuteSqlRawAsync(
                $"""
            ALTER TABLE Applications
            ADD CONSTRAINT [{constraintName}]
            CHECK (ApplicationStatus <> @completedStatus)
            """,
                new SqlParameter(
                    "@completedStatus",
                    completedStatusValue));
        }
        finally
        {
            await setupContext.Database.CloseConnectionAsync();
        }

        try
        {
            using var client =
                factory.CreateClient();

            ConfigureAuthenticatedClient(
                client,
                seed.UserId);

            var request =
                new ReleaseDetainedLicenseRequest
                {
                    DetainId =
                        seed.DetainId
                };

            var response =
                await client.PostAsJsonAsync(
                    "/api/DetainedLicenses/release",
                    request);

            Assert.Equal(
                HttpStatusCode.InternalServerError,
                response.StatusCode);

            await using var context =
                factory.CreateDbContext();

            var detention =
                await context.DetainedLicenses
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x =>
                            x.DetainID ==
                            seed.DetainId);

            Assert.NotNull(detention);

            Assert.False(
                detention.IsReleased);

            Assert.Null(
                detention.ReleaseDate);

            Assert.Null(
                detention.ReleasedByUserID);

            Assert.Null(
                detention.ReleaseApplicationID);

            var license =
                await context.Licenses
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x =>
                            x.LicenseID ==
                            seed.LicenseId);

            Assert.NotNull(license);

            Assert.False(
                license.IsActive);

            var releaseApplications =
                await context.Applications
                    .AsNoTracking()
                    .Where(
                        x =>
                            x.ApplicantPersonID ==
                                seed.PersonId &&
                            x.ApplicationTypeID ==
                                5)
                    .ToListAsync();

            Assert.Empty(
                releaseApplications);
        }
        finally
        {
            await using var cleanupContext =
                factory.CreateDbContext();

            await cleanupContext.Database.OpenConnectionAsync();

            try
            {
                await cleanupContext.Database.ExecuteSqlRawAsync(
                    $"""
                ALTER TABLE Applications
                DROP CONSTRAINT [{constraintName}]
                """);
            }
            catch
            {
                // Constraint may already be removed
                // during database cleanup.
            }
            finally
            {
                await cleanupContext.Database.CloseConnectionAsync();
            }
        }
    }

    [Fact]
    public async Task ReleaseDetainedLicense_WhenTwoRequestsAreSentConcurrently_AllowsOnlyOneRelease()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedDetainedLicenseScenarioAsync(factory);

        using var clientA =
            factory.CreateClient();

        using var clientB =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            clientA,
            seed.UserId);

        ConfigureAuthenticatedClient(
            clientB,
            seed.UserId);

        var request =
            new ReleaseDetainedLicenseRequest
            {
                DetainId = seed.DetainId
            };

        var gate =
            new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> SendAsync(
            HttpClient client)
        {
            await gate.Task;

            return await client.PostAsJsonAsync(
                "/api/DetainedLicenses/release",
                request);
        }

        var taskA =
            SendAsync(clientA);

        var taskB =
            SendAsync(clientB);

        gate.SetResult();

        var responses =
            await Task.WhenAll(
                taskA,
                taskB);

        Assert.Equal(
            1,
            responses.Count(
                x =>
                    x.StatusCode ==
                    HttpStatusCode.NoContent));

        Assert.Equal(
            1,
            responses.Count(
                x =>
                    x.StatusCode ==
                    HttpStatusCode.Conflict));

        var conflictResponse =
            responses.Single(
                x =>
                    x.StatusCode ==
                    HttpStatusCode.Conflict);

        var conflictBody =
            await conflictResponse.Content
                .ReadFromJsonAsync<ConflictResponse>();

        Assert.NotNull(conflictBody);

        Assert.Equal(
            "License is already released.",
            conflictBody.Error);

        await using var context =
            factory.CreateDbContext();

        var detention =
            await context.DetainedLicenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.DetainID == seed.DetainId);

        Assert.NotNull(detention);

        Assert.True(detention.IsReleased);
        Assert.NotNull(detention.ReleaseApplicationID);

        var releaseApplications =
            await context.Applications
                .AsNoTracking()
                .Where(
                    x =>
                        x.ApplicantPersonID ==
                            seed.PersonId &&
                        x.ApplicationTypeID == 5)
                .ToListAsync();

        Assert.Single(releaseApplications);

        Assert.Equal(
            AppStatus.Completed,
            releaseApplications[0].ApplicationStatus);
    }

    private static void ConfigureAuthenticatedClient(
        HttpClient client,
        int userId)
    {
        client.DefaultRequestHeaders.Add(
            "X-Test-User-Id",
            userId.ToString());

        client.DefaultRequestHeaders.Add(
            "X-Test-Username",
            "integration.test");

        client.DefaultRequestHeaders.Add(
            "X-Test-FullName",
            "Integration Test User");

        client.DefaultRequestHeaders.Add(
            "X-Test-Role",
            "Staff");
    }

    private static async Task<SeedData>
        SeedDetainedLicenseScenarioAsync(
            SqlServerApiWebApplicationFactory factory,
            bool includeReleaseApplicationType = true,
            bool createAnotherActiveLicense = false,
            bool licenseExpired = false,
            bool isReleased = false)
    {
        await using var context =
            factory.CreateDbContext();

        await context.Database.OpenConnectionAsync();

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT ApplicationTypes ON");

            if (includeReleaseApplicationType)
            {
                await context.Database.ExecuteSqlRawAsync(
                    """
                INSERT INTO ApplicationTypes
                    (
                        ApplicationTypeId,
                        ApplicationTypeTitle,
                        ApplicationFees
                    )
                VALUES
                    (
                        1,
                        N'New Local Driving License',
                        20
                    ),
                    (
                        5,
                        N'Release Detained License',
                        5
                    )
                """);
            }
            else
            {
                await context.Database.ExecuteSqlRawAsync(
                    """
                INSERT INTO ApplicationTypes
                    (
                        ApplicationTypeId,
                        ApplicationTypeTitle,
                        ApplicationFees
                    )
                VALUES
                    (
                        1,
                        N'New Local Driving License',
                        20
                    )
                """);
            }

            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT ApplicationTypes OFF");

            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT LicenseClasses ON");

            await context.Database.ExecuteSqlRawAsync(
                """
            INSERT INTO LicenseClasses
                (
                    LicenseClassID,
                    ClassName,
                    ClassDescription,
                    MinimumAllowedAge,
                    DefaultValidityLength,
                    ClassFees
                )
            VALUES
                (
                    1,
                    N'Integration Class',
                    N'Integration test license class',
                    18,
                    5,
                    100
                )
            """);

            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT LicenseClasses OFF");
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }

        var country =
            new Country
            {
                CountryName =
                    $"Integration Country {Guid.NewGuid():N}"
            };

        context.Countries.Add(country);

        await context.SaveChangesAsync();

        var userPerson =
            new Person
            {
                NationalNo =
                    CreateNationalNumber(),

                FirstName =
                    "Integration",

                SecondName =
                    "Test",

                LastName =
                    "User",

                DateOfBirth =
                    new DateTime(1990, 1, 1),

                Gender =
                    Gender.Male,

                Address =
                    "Integration Test Address",

                Phone =
                    CreatePhone(),

                Email =
                    $"user-{Guid.NewGuid():N}@test.local",

                NationalityCountryID =
                    country.CountryId
            };

        var applicantPerson =
            new Person
            {
                NationalNo =
                    CreateNationalNumber(),

                FirstName =
                    "Release",

                SecondName =
                    "Integration",

                LastName =
                    "Applicant",

                DateOfBirth =
                    new DateTime(1990, 1, 1),

                Gender =
                    Gender.Male,

                Address =
                    "Release Applicant Address",

                Phone =
                    CreatePhone(),

                Email =
                    $"release-{Guid.NewGuid():N}@test.local",

                NationalityCountryID =
                    country.CountryId
            };

        context.People.AddRange(
            userPerson,
            applicantPerson);

        await context.SaveChangesAsync();

        var user =
            new User
            {
                PersonId =
                    userPerson.PersonId,

                UserName =
                    $"integration-{Guid.NewGuid():N}",

                Password =
                    "TestPassword",

                IsActive =
                    true,

                Role =
                    UserRole.Staff
            };

        context.Users.Add(user);

        await context.SaveChangesAsync();

        var driver =
            new Driver
            {
                PersonID =
                    applicantPerson.PersonId,

                CreatedByUserID =
                    user.UserId,

                CreatedDate =
                    DateTime.UtcNow
            };

        context.Drivers.Add(driver);

        await context.SaveChangesAsync();

        var originalApplication =
            new ApplicationD
            {
                ApplicantPersonID =
                    applicantPerson.PersonId,

                ApplicationDate =
                    DateTime.UtcNow.AddYears(-1),

                ApplicationTypeID =
                    1,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    DateTime.UtcNow.AddYears(-1),

                PaidFees =
                    20m,

                CreatedByUserID =
                    user.UserId
            };

        context.Applications.Add(
            originalApplication);

        await context.SaveChangesAsync();

        var issueDate =
            DateTime.UtcNow.AddYears(-1);

        var expirationDate =
            licenseExpired
                ? DateTime.UtcNow.AddDays(-1)
                : DateTime.UtcNow.AddYears(4);

        var license =
            new License
            {
                ApplicationID =
                    originalApplication.ApplicationID,

                DriverID =
                    driver.DriverID,

                LicenseClass =
                    1,

                IssueDate =
                    issueDate,

                ExpirationDate =
                    expirationDate,

                Notes =
                    "Detained integration-test license",

                PaidFees =
                    100m,

                IsActive =
                    false,

                IssueReason =
                    IssueReason.FirstTime,

                CreatedByUserID =
                    user.UserId
            };

        context.Licenses.Add(license);

        await context.SaveChangesAsync();

        License? anotherLicense =
            null;

        if (createAnotherActiveLicense)
        {
            var anotherApplication =
                new ApplicationD
                {
                    ApplicantPersonID =
                        applicantPerson.PersonId,

                    ApplicationDate =
                        DateTime.UtcNow.AddMonths(-6),

                    ApplicationTypeID =
                        1,

                    ApplicationStatus =
                        AppStatus.New,

                    LastStatusDate =
                        DateTime.UtcNow.AddMonths(-6),

                    PaidFees =
                        20m,

                    CreatedByUserID =
                        user.UserId
                };

            context.Applications.Add(
                anotherApplication);

            await context.SaveChangesAsync();

            anotherLicense =
                new License
                {
                    ApplicationID =
                        anotherApplication.ApplicationID,

                    DriverID =
                        driver.DriverID,

                    LicenseClass =
                        1,

                    IssueDate =
                        DateTime.UtcNow.AddMonths(-6),

                    ExpirationDate =
                        DateTime.UtcNow.AddYears(4),

                    Notes =
                        "Another active integration-test license",

                    PaidFees =
                        100m,

                    IsActive =
                        true,

                    IssueReason =
                        IssueReason.FirstTime,

                    CreatedByUserID =
                        user.UserId
                };

            context.Licenses.Add(
                anotherLicense);

            await context.SaveChangesAsync();
        }

        var detention =
            new DetainedLicense
            {
                LicenseID =
                    license.LicenseID,

                DetainDate =
                    DateTime.UtcNow.AddDays(-1),

                FineFees =
                    50m,

                CreatedByUserID =
                    user.UserId,

                IsReleased =
                    isReleased,

                ReleaseDate =
                    isReleased
                        ? DateTime.UtcNow.AddHours(-1)
                        : null,

                ReleasedByUserID =
                    null,

                ReleaseApplicationID =
                    null
            };

        context.DetainedLicenses.Add(
            detention);

        await context.SaveChangesAsync();

        return new SeedData(
            UserId:
                user.UserId,

            PersonId:
                applicantPerson.PersonId,

            DriverId:
                driver.DriverID,

            LicenseId:
                license.LicenseID,

            DetainId:
                detention.DetainID,

            AnotherLicenseId:
                anotherLicense?.LicenseID ?? 0,

            ReleaseApplicationFees:
                5m);
    }

    private static string CreateNationalNumber() =>
        Guid.NewGuid()
            .ToString("N")[..18];

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

    private sealed record ErrorResponse(
        string? Error);

    private sealed record ConflictResponse(
        string? Error);
}
