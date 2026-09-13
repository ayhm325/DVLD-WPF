using API.IntegrationTests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using DVLD.Contracts.DetainedLicense;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Workflows;

public sealed class DetainedLicenseWorkflowTests
{
    [Fact]
    public async Task DetainLicense_WhenActiveLicenseIsValid_CreatesDetentionAndDeactivatesLicense()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedActiveLicenseScenarioAsync(factory);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var request = new CreateDetainedLicenseRequest
        {
            LicenseId = seed.LicenseId,
            FineFees = 150m
        };

        using var response = await client.PostAsJsonAsync(
            "/api/DetainedLicenses",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<DetainedLicenseResponse>();

        Assert.NotNull(result);

        Assert.True(result.DetainId > 0);
        Assert.Equal(seed.LicenseId, result.LicenseId);
        Assert.Equal(150m, result.FineFees);
        Assert.Equal(seed.UserId, result.CreatedByUserId);
        Assert.False(result.IsReleased);

        await using var verificationContext = factory.CreateDbContext();

        var license = await verificationContext.Licenses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.LicenseID == seed.LicenseId);

        Assert.NotNull(license);
        Assert.False(license.IsActive);

        var detention = await verificationContext.DetainedLicenses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.DetainID == result.DetainId);

        Assert.NotNull(detention);

        Assert.Equal(seed.LicenseId, detention.LicenseID);
        Assert.Equal(150m, detention.FineFees);
        Assert.Equal(seed.UserId, detention.CreatedByUserID);
        Assert.False(detention.IsReleased);
        Assert.Null(detention.ReleaseDate);
        Assert.Null(detention.ReleasedByUserID);
        Assert.Null(detention.ReleaseApplicationID);
    }

    [Fact]
    public async Task DetainLicense_WhenLicenseIsAlreadyDetained_ReturnsConflictAndDoesNotCreateDuplicate()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedActiveLicenseScenarioAsync(factory);

        await using (var seedContext = factory.CreateDbContext())
        {
            seedContext.DetainedLicenses.Add(
                new DetainedLicense
                {
                    LicenseID = seed.LicenseId,
                    DetainDate = DateTime.UtcNow.AddMinutes(-5),
                    FineFees = 100m,
                    CreatedByUserID = seed.UserId,
                    IsReleased = false
                });

            await seedContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var request = new CreateDetainedLicenseRequest
        {
            LicenseId = seed.LicenseId,
            FineFees = 200m
        };

        using var response = await client.PostAsJsonAsync(
            "/api/DetainedLicenses",
            request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var conflictBody = await response.Content
            .ReadFromJsonAsync<ConflictResponse>();

        Assert.NotNull(conflictBody);
        Assert.Equal(
            "License is already detained.",
            conflictBody.Error);

        await using var verificationContext = factory.CreateDbContext();

        var detentions = await verificationContext.DetainedLicenses
            .AsNoTracking()
            .Where(x => x.LicenseID == seed.LicenseId)
            .ToListAsync();

        Assert.Single(detentions);
        Assert.Equal(100m, detentions[0].FineFees);
        Assert.False(detentions[0].IsReleased);

        var license = await verificationContext.Licenses
            .AsNoTracking()
            .SingleAsync(
                x => x.LicenseID == seed.LicenseId);

        Assert.True(license.IsActive);
    }

    [Fact]
    public async Task DetainLicense_WhenLicenseIsInactive_ReturnsConflictAndDoesNotCreateDetention()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedActiveLicenseScenarioAsync(factory);

        await using (var updateContext = factory.CreateDbContext())
        {
            var affectedRows = await updateContext.Licenses
                .Where(x => x.LicenseID == seed.LicenseId)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        x => x.IsActive,
                        false));

            Assert.Equal(1, affectedRows);
        }

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var request = new CreateDetainedLicenseRequest
        {
            LicenseId = seed.LicenseId,
            FineFees = 100m
        };

        using var response = await client.PostAsJsonAsync(
            "/api/DetainedLicenses",
            request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var verificationContext = factory.CreateDbContext();

        var license = await verificationContext.Licenses
            .AsNoTracking()
            .SingleAsync(
                x => x.LicenseID == seed.LicenseId);

        Assert.False(license.IsActive);

        var detentions = await verificationContext.DetainedLicenses
            .AsNoTracking()
            .Where(x => x.LicenseID == seed.LicenseId)
            .ToListAsync();

        Assert.Empty(detentions);
    }

    [Fact]
    public async Task DetainLicense_WhenLicenseIsExpired_ReturnsConflictAndDoesNotCreateDetention()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedActiveLicenseScenarioAsync(factory);

        var expiredDate = DateTime.UtcNow.AddDays(-1);

        await using (var updateContext = factory.CreateDbContext())
        {
            var affectedRows = await updateContext.Licenses
                .Where(x => x.LicenseID == seed.LicenseId)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        x => x.ExpirationDate,
                        expiredDate));

            Assert.Equal(1, affectedRows);
        }

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var request = new CreateDetainedLicenseRequest
        {
            LicenseId = seed.LicenseId,
            FineFees = 100m
        };

        using var response = await client.PostAsJsonAsync(
            "/api/DetainedLicenses",
            request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var verificationContext = factory.CreateDbContext();

        var license = await verificationContext.Licenses
            .AsNoTracking()
            .SingleAsync(
                x => x.LicenseID == seed.LicenseId);

        Assert.True(license.IsActive);
        Assert.True(license.ExpirationDate <= DateTime.UtcNow);

        var detentions = await verificationContext.DetainedLicenses
            .AsNoTracking()
            .Where(x => x.LicenseID == seed.LicenseId)
            .ToListAsync();

        Assert.Empty(detentions);
    }

    [Fact]
    public async Task DetainLicense_WhenDatabaseInsertFails_RollsBackLicenseState()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedActiveLicenseScenarioAsync(factory);

        await using (var setupContext = factory.CreateDbContext())
        {
            await setupContext.Database.OpenConnectionAsync();

            try
            {
                await setupContext.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE DetainedLicenses
                    ADD CONSTRAINT CK_DetainedLicenses_IntegrationTest_ForceFailure
                    CHECK (FineFees < 0)
                    """);
            }
            finally
            {
                await setupContext.Database.CloseConnectionAsync();
            }
        }

        try
        {
            using var client = factory.CreateClient();
            ConfigureAuthenticatedClient(client, seed.UserId);

            var request = new CreateDetainedLicenseRequest
            {
                LicenseId = seed.LicenseId,
                FineFees = 150m
            };

            using var response = await client.PostAsJsonAsync(
                "/api/DetainedLicenses",
                request);

            Assert.Equal(
                HttpStatusCode.InternalServerError,
                response.StatusCode);

            await using var verificationContext =
                factory.CreateDbContext();

            var license = await verificationContext.Licenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.LicenseID == seed.LicenseId);

            Assert.NotNull(license);
            Assert.True(license.IsActive);

            var detentions = await verificationContext.DetainedLicenses
                .AsNoTracking()
                .Where(x => x.LicenseID == seed.LicenseId)
                .ToListAsync();

            Assert.Empty(detentions);
        }
        finally
        {
            await using var cleanupContext =
                factory.CreateDbContext();

            await cleanupContext.Database.OpenConnectionAsync();

            try
            {
                await cleanupContext.Database.ExecuteSqlRawAsync(
                    """
                    ALTER TABLE DetainedLicenses
                    DROP CONSTRAINT CK_DetainedLicenses_IntegrationTest_ForceFailure
                    """);
            }
            catch
            {
                // The factory may already be cleaning up the isolated test database.
            }
            finally
            {
                await cleanupContext.Database.CloseConnectionAsync();
            }
        }
    }

    [Fact]
    public async Task DetainLicense_WhenTwoRequestsAreSentConcurrently_AllowsOnlyOneDetention()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedActiveLicenseScenarioAsync(factory);

        using var clientA = factory.CreateClient();
        using var clientB = factory.CreateClient();

        ConfigureAuthenticatedClient(clientA, seed.UserId);
        ConfigureAuthenticatedClient(clientB, seed.UserId);

        var request = new CreateDetainedLicenseRequest
        {
            LicenseId = seed.LicenseId,
            FineFees = 150m
        };

        var startGate = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> SendAsync(
            HttpClient client)
        {
            await startGate.Task;

            return await client.PostAsJsonAsync(
                "/api/DetainedLicenses",
                request);
        }

        var taskA = SendAsync(clientA);
        var taskB = SendAsync(clientB);

        startGate.SetResult();

        using var responseA = await taskA;
        using var responseB = await taskB;

        var responses = new[]
        {
            responseA,
            responseB
        };

        var successfulResponses = responses
            .Where(x => x.StatusCode == HttpStatusCode.OK)
            .ToList();

        var conflictResponses = responses
            .Where(x => x.StatusCode == HttpStatusCode.Conflict)
            .ToList();

        Assert.Single(successfulResponses);
        Assert.Single(conflictResponses);

        var conflictBody = await conflictResponses[0].Content
            .ReadFromJsonAsync<ConflictResponse>();

        Assert.NotNull(conflictBody);

        Assert.Contains(
            conflictBody.Error,
            new[]
            {
                "Only an active license can be detained.",
                "License is already detained."
            });

        var successfulResult =
            await successfulResponses[0].Content
                .ReadFromJsonAsync<DetainedLicenseResponse>();

        Assert.NotNull(successfulResult);
        Assert.Equal(seed.LicenseId, successfulResult.LicenseId);
        Assert.Equal(150m, successfulResult.FineFees);
        Assert.Equal(seed.UserId, successfulResult.CreatedByUserId);
        Assert.False(successfulResult.IsReleased);

        await using var verificationContext =
            factory.CreateDbContext();

        var detentions = await verificationContext.DetainedLicenses
            .AsNoTracking()
            .Where(x => x.LicenseID == seed.LicenseId)
            .ToListAsync();

        Assert.Single(detentions);

        Assert.Equal(
            successfulResult.DetainId,
            detentions[0].DetainID);

        Assert.Equal(
            seed.UserId,
            detentions[0].CreatedByUserID);

        Assert.Equal(
            150m,
            detentions[0].FineFees);

        Assert.False(detentions[0].IsReleased);

        var license = await verificationContext.Licenses
            .AsNoTracking()
            .SingleAsync(
                x => x.LicenseID == seed.LicenseId);

        Assert.False(license.IsActive);
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

    private static async Task<SeedData> SeedActiveLicenseScenarioAsync(
        SqlServerApiWebApplicationFactory factory)
    {
        await using var context = factory.CreateDbContext();

        await SeedLookupDataAsync(context);

        var country = new Country
        {
            CountryName =
                $"Integration Country {Guid.NewGuid():N}"
        };

        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var userPerson = new Person
        {
            NationalNo = CreateNationalNumber(),
            FirstName = "Integration",
            SecondName = "Test",
            LastName = "User",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = Gender.Male,
            Address = "Integration Test Address",
            Phone = CreatePhone(),
            Email = $"user-{Guid.NewGuid():N}@test.local",
            NationalityCountryID = country.CountryId
        };

        var applicantPerson = new Person
        {
            NationalNo = CreateNationalNumber(),
            FirstName = "Detain",
            SecondName = "Integration",
            LastName = "Applicant",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = Gender.Male,
            Address = "Detain Applicant Address",
            Phone = CreatePhone(),
            Email = $"detain-{Guid.NewGuid():N}@test.local",
            NationalityCountryID = country.CountryId
        };

        context.People.AddRange(
            userPerson,
            applicantPerson);

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

        var application = new ApplicationD
        {
            ApplicantPersonID = applicantPerson.PersonId,
            ApplicationDate = DateTime.UtcNow.AddYears(-5),
            ApplicationTypeID = 1,
            ApplicationStatus = AppStatus.Completed,
            LastStatusDate = DateTime.UtcNow.AddYears(-5),
            PaidFees = 20m,
            CreatedByUserID = user.UserId
        };

        context.Applications.Add(application);
        await context.SaveChangesAsync();

        var license = new License
        {
            ApplicationID = application.ApplicationID,
            DriverID = driver.DriverID,
            LicenseClass = 1,
            IssueDate = DateTime.UtcNow.AddYears(-1),
            ExpirationDate = DateTime.UtcNow.AddYears(4),
            Notes = "Active integration-test license",
            PaidFees = 100m,
            IsActive = true,
            IssueReason = IssueReason.FirstTime,
            CreatedByUserID = user.UserId
        };

        context.Licenses.Add(license);
        await context.SaveChangesAsync();

        return new SeedData(
            UserId: user.UserId,
            LicenseId: license.LicenseID);
    }

    private static async Task SeedLookupDataAsync(
        DVLDDbContext context)
    {
        await context.Database.OpenConnectionAsync();

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT ApplicationTypes ON");

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
    }

    private static string CreateNationalNumber() =>
        Guid.NewGuid().ToString("N")[..18];

    private static string CreatePhone() =>
        $"07{Random.Shared.NextInt64(100000000, 999999999)}";

    private sealed record ConflictResponse(
        string? Error);

    private sealed record SeedData(
        int UserId,
        int LicenseId);
}
