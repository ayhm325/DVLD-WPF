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

public sealed class DetainedLicenseWorkflowTests
{
    [Fact]
    public async Task DetainLicense_WhenActiveLicenseIsValid_CreatesDetentionAndDeactivatesLicense()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedActiveLicenseScenarioAsync(factory);
        using var client = AuthClient(factory, seed.UserId);

        var response = await client.PostAsJsonAsync("/api/DetainedLicenses",
            new CreateDetainedLicenseRequest { LicenseId = seed.LicenseId, FineFees = 150m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DetainedLicenseResponse>();
        Assert.NotNull(result);
        Assert.True(result.DetainId > 0);
        Assert.Equal(seed.LicenseId, result.LicenseId);
        Assert.Equal(150m, result.FineFees);
        Assert.Equal(seed.UserId, result.CreatedByUserId);
        Assert.False(result.IsReleased);

        await using var context = factory.CreateDbContext();

        var license = await context.Licenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.LicenseID == seed.LicenseId);

        Assert.NotNull(license);
        Assert.False(license.IsActive);

        var detention = await context.DetainedLicenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DetainID == result.DetainId);

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

        await using (var context = factory.CreateDbContext())
        {
            context.DetainedLicenses.Add(new DetainedLicense
            {
                LicenseID = seed.LicenseId,
                DetainDate = DateTime.UtcNow.AddMinutes(-5),
                FineFees = 100m,
                CreatedByUserID = seed.UserId,
                IsReleased = false
            });
            await context.SaveChangesAsync();
        }

        using var client = AuthClient(factory, seed.UserId);

        var response = await client.PostAsJsonAsync("/api/DetainedLicenses",
            new CreateDetainedLicenseRequest { LicenseId = seed.LicenseId, FineFees = 200m });

        await AssertProblemDetailsAsync(response, HttpStatusCode.Conflict,
            "Conflict", "License is already detained.");

        await using var context2 = factory.CreateDbContext();

        var detentions = await context2.DetainedLicenses.AsNoTracking()
            .Where(x => x.LicenseID == seed.LicenseId)
            .ToListAsync();

        Assert.Single(detentions);
        Assert.Equal(100m, detentions[0].FineFees);
        Assert.False(detentions[0].IsReleased);

        var license = await context2.Licenses.AsNoTracking()
            .SingleAsync(x => x.LicenseID == seed.LicenseId);

        Assert.True(license.IsActive);
    }

    [Fact]
    public async Task DetainLicense_WhenLicenseIsInactive_ReturnsConflictAndDoesNotCreateDetention()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedActiveLicenseScenarioAsync(factory);

        await using (var context = factory.CreateDbContext())
            Assert.Equal(1, await context.Licenses
                .Where(x => x.LicenseID == seed.LicenseId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false)));

        using var client = AuthClient(factory, seed.UserId);

        var response = await client.PostAsJsonAsync("/api/DetainedLicenses",
            new CreateDetainedLicenseRequest { LicenseId = seed.LicenseId, FineFees = 100m });

        await AssertProblemDetailsAsync(response, HttpStatusCode.Conflict,
            "Conflict", "Only an active license can be detained.");

        await using var context2 = factory.CreateDbContext();

        var license = await context2.Licenses.AsNoTracking()
            .SingleAsync(x => x.LicenseID == seed.LicenseId);

        Assert.False(license.IsActive);
        Assert.Empty(await context2.DetainedLicenses.AsNoTracking()
            .Where(x => x.LicenseID == seed.LicenseId)
            .ToListAsync());
    }

    [Fact]
    public async Task DetainLicense_WhenLicenseIsExpired_ReturnsConflictAndDoesNotCreateDetention()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedActiveLicenseScenarioAsync(factory);

        await using (var context = factory.CreateDbContext())
            Assert.Equal(1, await context.Licenses
                .Where(x => x.LicenseID == seed.LicenseId)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    x => x.ExpirationDate, DateTime.UtcNow.AddDays(-1))));

        using var client = AuthClient(factory, seed.UserId);

        var response = await client.PostAsJsonAsync("/api/DetainedLicenses",
            new CreateDetainedLicenseRequest { LicenseId = seed.LicenseId, FineFees = 100m });

        await AssertProblemDetailsAsync(response, HttpStatusCode.Conflict,
            "Conflict", "An expired license cannot be detained.");

        await using var context2 = factory.CreateDbContext();

        var license = await context2.Licenses.AsNoTracking()
            .SingleAsync(x => x.LicenseID == seed.LicenseId);

        Assert.True(license.IsActive);
        Assert.True(license.ExpirationDate <= DateTime.UtcNow);

        Assert.Empty(await context2.DetainedLicenses.AsNoTracking()
            .Where(x => x.LicenseID == seed.LicenseId)
            .ToListAsync());
    }

    [Fact]
    public async Task DetainLicense_WhenDatabaseInsertFails_RollsBackLicenseState()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedActiveLicenseScenarioAsync(factory);
        await AddFailureConstraintAsync(factory);

        try
        {
            using var client = AuthClient(factory, seed.UserId);

            var response = await client.PostAsJsonAsync("/api/DetainedLicenses",
                new CreateDetainedLicenseRequest
                {
                    LicenseId = seed.LicenseId,
                    FineFees = 150m
                });

            await AssertProblemDetailsAsync(response, HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                "The server could not complete the request.",
                requireProblemContentType: false);

            await using var context = factory.CreateDbContext();

            var license = await context.Licenses.AsNoTracking()
                .SingleOrDefaultAsync(x => x.LicenseID == seed.LicenseId);

            Assert.NotNull(license);
            Assert.True(license.IsActive);

            Assert.Empty(await context.DetainedLicenses.AsNoTracking()
                .Where(x => x.LicenseID == seed.LicenseId)
                .ToListAsync());
        }
        finally
        {
            await RemoveFailureConstraintAsync(factory);
        }
    }

    [Fact]
    public async Task DetainLicense_WhenTwoRequestsAreSentConcurrently_AllowsOnlyOneDetention()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedActiveLicenseScenarioAsync(factory);
        using var clientA = AuthClient(factory, seed.UserId);
        using var clientB = AuthClient(factory, seed.UserId);

        var request = new CreateDetainedLicenseRequest
        {
            LicenseId = seed.LicenseId,
            FineFees = 150m
        };

        var gate = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> SendAsync(HttpClient client)
        {
            await gate.Task;
            return await client.PostAsJsonAsync("/api/DetainedLicenses", request);
        }

        var taskA = SendAsync(clientA);
        var taskB = SendAsync(clientB);
        gate.SetResult();

        using var responseA = await taskA;
        using var responseB = await taskB;

        var responses = new[] { responseA, responseB };

        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);

        var conflict = responses.Single(x => x.StatusCode == HttpStatusCode.Conflict);
        var conflictBody = await ReadProblemDetailsAsync(conflict);

        Assert.Contains(conflictBody.Detail, new[]
        {
            "Only an active license can be detained.",
            "License is already detained."
        });

        var successfulResponse = responses.Single(x => x.StatusCode == HttpStatusCode.OK);
        var result = await successfulResponse.Content
            .ReadFromJsonAsync<DetainedLicenseResponse>();

        Assert.NotNull(result);
        Assert.Equal(seed.LicenseId, result.LicenseId);
        Assert.Equal(150m, result.FineFees);
        Assert.Equal(seed.UserId, result.CreatedByUserId);
        Assert.False(result.IsReleased);

        await using var context = factory.CreateDbContext();

        var detentions = await context.DetainedLicenses.AsNoTracking()
            .Where(x => x.LicenseID == seed.LicenseId)
            .ToListAsync();

        Assert.Single(detentions);
        Assert.Equal(result.DetainId, detentions[0].DetainID);
        Assert.Equal(seed.UserId, detentions[0].CreatedByUserID);
        Assert.Equal(150m, detentions[0].FineFees);
        Assert.False(detentions[0].IsReleased);

        var license = await context.Licenses.AsNoTracking()
            .SingleAsync(x => x.LicenseID == seed.LicenseId);

        Assert.False(license.IsActive);
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
        var problem = await ReadProblemDetailsAsync(response);

        Assert.Equal(status, response.StatusCode);

        if (requireProblemContentType)
            Assert.Equal("application/problem+json",
                response.Content.Headers.ContentType?.MediaType);

        Assert.Equal((int)status, problem.Status);
        Assert.Equal(title, problem.Title);
        Assert.Equal(detail, problem.Detail);
        Assert.False(string.IsNullOrWhiteSpace(problem.Instance));
        Assert.False(string.IsNullOrWhiteSpace(problem.TraceId));
    }

    private static async Task<ProblemDetailsBody> ReadProblemDetailsAsync(
        HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var body = document.RootElement;

        return new(
            body.GetProperty("status").GetInt32(),
            body.GetProperty("title").GetString(),
            body.GetProperty("detail").GetString(),
            body.GetProperty("instance").GetString(),
            body.GetProperty("traceId").GetString());
    }

    private static async Task AddFailureConstraintAsync(
        SqlServerApiWebApplicationFactory factory)
    {
        await using var context = factory.CreateDbContext();
        await context.Database.OpenConnectionAsync();

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE DetainedLicenses
                ADD CONSTRAINT CK_DetainedLicenses_IntegrationTest_ForceFailure
                CHECK (FineFees < 0)
                """);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task RemoveFailureConstraintAsync(
        SqlServerApiWebApplicationFactory factory)
    {
        await using var context = factory.CreateDbContext();
        await context.Database.OpenConnectionAsync();

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE DetainedLicenses
                DROP CONSTRAINT CK_DetainedLicenses_IntegrationTest_ForceFailure
                """);
        }
        catch { }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task<SeedData> SeedActiveLicenseScenarioAsync(
        SqlServerApiWebApplicationFactory factory)
    {
        await using var context = factory.CreateDbContext();
        await SeedLookupDataAsync(context);

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
            FirstName = "Detain",
            SecondName = "Integration",
            LastName = "Applicant",
            DateOfBirth = new(1990, 1, 1),
            Gender = Gender.Male,
            Address = "Detain Applicant Address",
            Phone = CreatePhone(),
            Email = $"detain-{Guid.NewGuid():N}@test.local",
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

        return new(user.UserId, license.LicenseID);
    }

    private static async Task SeedLookupDataAsync(DVLDDbContext context)
    {
        await context.Database.OpenConnectionAsync();

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT ApplicationTypes ON");

            await context.Database.ExecuteSqlRawAsync(
                """
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

    private sealed record ProblemDetailsBody(
        int Status,
        string? Title,
        string? Detail,
        string? Instance,
        string? TraceId);

    private sealed record SeedData(int UserId, int LicenseId);
}
