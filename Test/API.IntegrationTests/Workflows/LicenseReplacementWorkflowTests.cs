using API.IntegrationTests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using DVLD.Contracts.LicenseReplacement;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using static System.Net.Mime.MediaTypeNames;

namespace API.IntegrationTests.Workflows;

public sealed class LicenseReplacementWorkflowTests
{
    [Theory]
    [InlineData("Lost License", IssueReason.ReplacementForLost, 3)]
    [InlineData("Damaged License", IssueReason.ReplacementForDamaged, 4)]
    public async Task ReplaceLicense_WhenActiveLicenseIsValid_CreatesReplacementAndCompletesApplication(
        string reason,
        IssueReason expectedIssueReason,
        int expectedApplicationTypeId)
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedReplacementScenarioAsync(factory);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var request = new ReplaceLicenseRequest(
            seed.OldLicenseId,
            reason);

        var response = await client.PostAsJsonAsync(
            "/api/LicenseReplacement",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<ReplaceLicenseResponse>();

        Assert.NotNull(result);
        Assert.True(result.LicenseId > 0);
        Assert.NotEqual(seed.OldLicenseId, result.LicenseId);

        await using var context = factory.CreateDbContext();

        var licenses = await context.Licenses
            .AsNoTracking()
            .Where(x =>
                x.DriverID == seed.DriverId &&
                x.LicenseClass == seed.LicenseClassId)
            .ToListAsync();

        Assert.Equal(2, licenses.Count);

        var oldLicense = licenses.Single(
            x => x.LicenseID == seed.OldLicenseId);

        var newLicense = licenses.Single(
            x => x.LicenseID != seed.OldLicenseId);

        Assert.False(oldLicense.IsActive);
        Assert.True(newLicense.IsActive);

        Assert.Equal(result.LicenseId, newLicense.LicenseID);
        Assert.Equal(seed.DriverId, newLicense.DriverID);
        Assert.Equal(seed.LicenseClassId, newLicense.LicenseClass);
        Assert.Equal(seed.UserId, newLicense.CreatedByUserID);

        Assert.Equal(expectedIssueReason, newLicense.IssueReason);
        Assert.Equal(reason, newLicense.Notes);
        Assert.Equal(seed.ClassFees, newLicense.PaidFees);

        Assert.Equal(
            oldLicense.ExpirationDate,
            newLicense.ExpirationDate);

        Assert.Equal(
            seed.ApplicationId,
            oldLicense.ApplicationID);

        Assert.NotEqual(
            oldLicense.ApplicationID,
            newLicense.ApplicationID);

        var replacementApplication = await context.Applications
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ApplicationID == newLicense.ApplicationID);

        Assert.NotNull(replacementApplication);

        Assert.Equal(
            seed.PersonId,
            replacementApplication.ApplicantPersonID);

        Assert.Equal(
            expectedApplicationTypeId,
            replacementApplication.ApplicationTypeID);

        Assert.Equal(
            AppStatus.Completed,
            replacementApplication.ApplicationStatus);

        Assert.Equal(
            seed.UserId,
            replacementApplication.CreatedByUserID);

        Assert.Equal(
            seed.ReplacementApplicationFees,
            replacementApplication.PaidFees);

        Assert.True(
            newLicense.IssueDate >= seed.BeforeRequestUtc);

        Assert.True(
            newLicense.IssueDate < newLicense.ExpirationDate);
    }

    [Fact]
    public async Task ReplaceLicense_WhenNewLicenseInsertFails_RollsBackEntireTransaction()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedReplacementScenarioAsync(factory);

        const string constraintName =
            "CK_Licenses_IntegrationTest_ForceFailure";

        await AddFailureConstraintAsync(
            factory,
            constraintName);

        try
        {
            using var client = factory.CreateClient();
            ConfigureAuthenticatedClient(client, seed.UserId);

            var request = new ReplaceLicenseRequest(
                seed.OldLicenseId,
                "Lost License");

            var response = await client.PostAsJsonAsync(
                "/api/LicenseReplacement",
                request);

            Assert.Equal(
                HttpStatusCode.InternalServerError,
                response.StatusCode);

            await using var context = factory.CreateDbContext();

            var oldLicense = await context.Licenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.LicenseID == seed.OldLicenseId);

            Assert.NotNull(oldLicense);
            Assert.True(oldLicense.IsActive);

            var licenses = await context.Licenses
                .AsNoTracking()
                .Where(x =>
                    x.DriverID == seed.DriverId &&
                    x.LicenseClass == seed.LicenseClassId)
                .ToListAsync();

            Assert.Single(licenses);

            Assert.Equal(
                seed.OldLicenseId,
                licenses[0].LicenseID);

            var replacementApplications = await context.Applications
                .AsNoTracking()
                .Where(x =>
                    x.ApplicantPersonID == seed.PersonId &&
                    (x.ApplicationTypeID == 3 ||
                     x.ApplicationTypeID == 4))
                .ToListAsync();

            Assert.Empty(replacementApplications);
        }
        finally
        {
            await RemoveFailureConstraintAsync(
                factory,
                constraintName);
        }
    }

    [Fact]
    public async Task ReplaceLicense_WhenTwoRequestsAreSentConcurrently_AllowsOnlyOneReplacement()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedReplacementScenarioAsync(factory);

        using var clientA = factory.CreateClient();
        using var clientB = factory.CreateClient();

        ConfigureAuthenticatedClient(clientA, seed.UserId);
        ConfigureAuthenticatedClient(clientB, seed.UserId);

        var request = new ReplaceLicenseRequest(
            seed.OldLicenseId,
            "Lost License");

        var gate = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> SendAsync(HttpClient client)
        {
            await gate.Task;

            return await client.PostAsJsonAsync(
                "/api/LicenseReplacement",
                request);
        }

        var taskA = SendAsync(clientA);
        var taskB = SendAsync(clientB);

        gate.SetResult();

        var responses = await Task.WhenAll(
            taskA,
            taskB);

        Assert.Equal(
            1,
            responses.Count(x =>
                x.StatusCode == HttpStatusCode.OK));

        Assert.Equal(
            1,
            responses.Count(x =>
                x.StatusCode == HttpStatusCode.Conflict));

        var successfulResponse = responses.Single(
            x => x.StatusCode == HttpStatusCode.OK);

        var result = await successfulResponse.Content
            .ReadFromJsonAsync<ReplaceLicenseResponse>();

        Assert.NotNull(result);
        Assert.True(result.LicenseId > 0);
        Assert.NotEqual(
            seed.OldLicenseId,
            result.LicenseId);

        await using var context = factory.CreateDbContext();

        var licenses = await context.Licenses
            .AsNoTracking()
            .Where(x =>
                x.DriverID == seed.DriverId &&
                x.LicenseClass == seed.LicenseClassId)
            .ToListAsync();

        Assert.Equal(2, licenses.Count);

        var oldLicense = licenses.Single(
            x => x.LicenseID == seed.OldLicenseId);

        var newLicense = licenses.Single(
            x => x.LicenseID != seed.OldLicenseId);

        Assert.False(oldLicense.IsActive);
        Assert.True(newLicense.IsActive);

        Assert.Equal(
            result.LicenseId,
            newLicense.LicenseID);

        Assert.Equal(
            IssueReason.ReplacementForLost,
            newLicense.IssueReason);

        Assert.Equal(
            "Lost License",
            newLicense.Notes);

        var replacementApplications = await context.Applications
            .AsNoTracking()
            .Where(x =>
                x.ApplicantPersonID == seed.PersonId &&
                x.ApplicationTypeID == 3)
            .ToListAsync();

        Assert.Single(replacementApplications);

        Assert.Equal(
            AppStatus.Completed,
            replacementApplications[0].ApplicationStatus);
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

    private static async Task AddFailureConstraintAsync(
        SqlServerApiWebApplicationFactory factory,
        string constraintName)
    {
        await using var context = factory.CreateDbContext();

        await context.Database.OpenConnectionAsync();

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                $"""
                ALTER TABLE Licenses
                ADD CONSTRAINT {constraintName}
                CHECK (Notes <> 'Lost License')
                """);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task RemoveFailureConstraintAsync(
        SqlServerApiWebApplicationFactory factory,
        string constraintName)
    {
        await using var context = factory.CreateDbContext();

        await context.Database.OpenConnectionAsync();

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                $"""
                ALTER TABLE Licenses
                DROP CONSTRAINT {constraintName}
                """);
        }
        catch
        {
            // The constraint may already have been removed.
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task<SeedData> SeedReplacementScenarioAsync(
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
            FirstName = "Replacement",
            SecondName = "Integration",
            LastName = "Applicant",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = Gender.Male,
            Address = "Replacement Applicant Address",
            Phone = CreatePhone(),
            Email = $"replacement-{Guid.NewGuid():N}@test.local",
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
            ApplicationDate = DateTime.UtcNow.AddYears(-10),
            ApplicationTypeID = 1,
            ApplicationStatus = AppStatus.Completed,
            LastStatusDate = DateTime.UtcNow.AddYears(-10),
            PaidFees = 20m,
            CreatedByUserID = user.UserId
        };

        context.Applications.Add(application);
        await context.SaveChangesAsync();

        var beforeRequestUtc = DateTime.UtcNow;

        var oldLicense = new License
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

        context.Licenses.Add(oldLicense);
        await context.SaveChangesAsync();

        return new SeedData(
            UserId: user.UserId,
            PersonId: applicantPerson.PersonId,
            DriverId: driver.DriverID,
            ApplicationId: application.ApplicationID,
            OldLicenseId: oldLicense.LicenseID,
            LicenseClassId: 1,
            ClassFees: 100m,
            ReplacementApplicationFees: 10m,
            BeforeRequestUtc: beforeRequestUtc);
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
                    ),
                    (
                        3,
                        N'Replacement for Lost License',
                        10
                    ),
                    (
                        4,
                        N'Replacement for Damaged License',
                        10
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

    private sealed record SeedData(
        int UserId,
        int PersonId,
        int DriverId,
        int ApplicationId,
        int OldLicenseId,
        int LicenseClassId,
        decimal ClassFees,
        decimal ReplacementApplicationFees,
        DateTime BeforeRequestUtc);
}
