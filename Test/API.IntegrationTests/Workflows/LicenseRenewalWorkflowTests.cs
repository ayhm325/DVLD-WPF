using API.IntegrationTests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using DVLD.Contracts.LicenseRenewal;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Workflows;

public sealed class LicenseRenewalWorkflowTests
{
    [Fact]
    public async Task RenewLicense_WhenExpiredLicenseIsValid_CreatesNewLicenseAndCompletesApplication()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedExpiredLicenseScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var request =
            new RenewLicenseRequest(
                OldLicenseId:
                    seed.OldLicenseId,
                Notes:
                    "Integration test renewal");

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseRenewal",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<RenewLicenseResponse>();

        Assert.NotNull(result);

        Assert.True(
            result.LicenseId > 0);

        Assert.NotEqual(
            seed.OldLicenseId,
            result.LicenseId);

        await using var verificationContext =
            factory.CreateDbContext();

        var oldLicense =
            await verificationContext
                .Licenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.LicenseID ==
                        seed.OldLicenseId);

        Assert.NotNull(oldLicense);

        Assert.False(
            oldLicense.IsActive);

        var newLicense =
            await verificationContext
                .Licenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.LicenseID ==
                        result.LicenseId);

        Assert.NotNull(newLicense);

        Assert.True(
            newLicense.IsActive);

        Assert.Equal(
            seed.DriverId,
            newLicense.DriverID);

        Assert.Equal(
            seed.LicenseClassId,
            newLicense.LicenseClass);

        Assert.Equal(
            IssueReason.Renew,
            newLicense.IssueReason);

        Assert.Equal(
            seed.UserId,
            newLicense.CreatedByUserID);

        Assert.Equal(
            "Integration test renewal",
            newLicense.Notes);

        Assert.Equal(
            seed.ClassFees,
            newLicense.PaidFees);

        Assert.Equal(
            seed.ApplicationId,
            oldLicense.ApplicationID);

        Assert.NotEqual(
            oldLicense.ApplicationID,
            newLicense.ApplicationID);

        var renewalApplication =
            await verificationContext
                .Applications
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.ApplicationID ==
                        newLicense.ApplicationID);

        Assert.NotNull(
            renewalApplication);

        Assert.Equal(
            seed.PersonId,
            renewalApplication.ApplicantPersonID);

        Assert.Equal(
            2,
            renewalApplication.ApplicationTypeID);

        Assert.Equal(
            AppStatus.Completed,
            renewalApplication.ApplicationStatus);

        Assert.Equal(
            seed.UserId,
            renewalApplication.CreatedByUserID);

        Assert.Equal(
            seed.RenewalApplicationFees,
            renewalApplication.PaidFees);

        Assert.True(
            newLicense.IssueDate >=
            seed.BeforeRequestUtc);

        Assert.True(
            newLicense.ExpirationDate >
            newLicense.IssueDate);

        Assert.Equal(
            seed.ValidityYears,
            newLicense.ExpirationDate.Year -
            newLicense.IssueDate.Year);
    }

    [Fact]
    public async Task RenewLicense_WhenNewLicenseInsertFails_RollsBackEntireTransaction()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedExpiredLicenseScenarioAsync(factory);

        const string constraintName =
            "CK_Licenses_IntegrationTest_ForceFailure";

        await using var setupContext =
            factory.CreateDbContext();

        await setupContext.Database.OpenConnectionAsync();

        try
        {
            await setupContext.Database.ExecuteSqlRawAsync(
                $"""
            ALTER TABLE Licenses
            ADD CONSTRAINT {constraintName}
            CHECK (Notes <> 'FORCE_FAILURE')
            """);
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
                new RenewLicenseRequest(
                    OldLicenseId:
                        seed.OldLicenseId,
                    Notes:
                        "FORCE_FAILURE");

            var response =
                await client.PostAsJsonAsync(
                    "/api/LicenseRenewal",
                    request);

            Assert.Equal(
                HttpStatusCode.InternalServerError,
                response.StatusCode);

            await using var verificationContext =
                factory.CreateDbContext();

            var oldLicense =
                await verificationContext.Licenses
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.LicenseID == seed.OldLicenseId);

            Assert.NotNull(oldLicense);

            Assert.True(
                oldLicense.IsActive);

            var licenses =
                await verificationContext.Licenses
                    .AsNoTracking()
                    .Where(
                        x => x.DriverID == seed.DriverId
                              && x.LicenseClass == seed.LicenseClassId)
                    .ToListAsync();

            Assert.Single(licenses);

            Assert.Equal(
                seed.OldLicenseId,
                licenses[0].LicenseID);

            var renewalApplications =
                await verificationContext.Applications
                    .AsNoTracking()
                    .Where(
                        x => x.ApplicantPersonID == seed.PersonId
                              && x.ApplicationTypeID == 2)
                    .ToListAsync();

            Assert.Empty(
                renewalApplications);
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
                ALTER TABLE Licenses
                DROP CONSTRAINT {constraintName}
                """);
            }
            catch
            {
                // The constraint may already have been removed
                // if the test environment/database cleanup handled it.
            }
            finally
            {
                await cleanupContext.Database.CloseConnectionAsync();
            }
        }
    }

    [Fact]
    public async Task RenewLicense_WhenTwoRequestsAreSentConcurrently_AllowsOnlyOneRenewal()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedExpiredLicenseScenarioAsync(factory);

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
            new RenewLicenseRequest(
                OldLicenseId:
                    seed.OldLicenseId,
                Notes:
                    "Concurrent renewal test");

        var requestA =
            clientA.PostAsJsonAsync(
                "/api/LicenseRenewal",
                request);

        var requestB =
            clientB.PostAsJsonAsync(
                "/api/LicenseRenewal",
                request);

        var responses =
            await Task.WhenAll(
                requestA,
                requestB);

        var successfulResponses =
            responses
                .Where(
                    x => x.StatusCode == HttpStatusCode.OK)
                .ToList();

        var conflictResponses =
            responses
                .Where(
                    x => x.StatusCode == HttpStatusCode.Conflict)
                .ToList();

        Assert.Single(
            successfulResponses);

        Assert.Single(
            conflictResponses);

        var successfulResponse =
            successfulResponses[0];

        var result =
            await successfulResponse.Content
                .ReadFromJsonAsync<RenewLicenseResponse>();

        Assert.NotNull(result);

        Assert.True(
            result.LicenseId > 0);

        Assert.NotEqual(
            seed.OldLicenseId,
            result.LicenseId);

        await using var verificationContext =
            factory.CreateDbContext();

        var licenses =
            await verificationContext.Licenses
                .AsNoTracking()
                .Where(
                    x => x.DriverID == seed.DriverId
                          && x.LicenseClass == seed.LicenseClassId)
                .OrderBy(
                    x => x.LicenseID)
                .ToListAsync();

        Assert.Equal(
            2,
            licenses.Count);

        var oldLicense =
            licenses.Single(
                x => x.LicenseID == seed.OldLicenseId);

        var newLicenses =
            licenses
                .Where(
                    x => x.LicenseID != seed.OldLicenseId)
                .ToList();

        Assert.Single(
            newLicenses);

        var newLicense =
            newLicenses[0];

        Assert.False(
            oldLicense.IsActive);

        Assert.True(
            newLicense.IsActive);

        Assert.Equal(
            result.LicenseId,
            newLicense.LicenseID);

        Assert.Equal(
            IssueReason.Renew,
            newLicense.IssueReason);

        var renewalApplications =
            await verificationContext.Applications
                .AsNoTracking()
                .Where(
                    x => x.ApplicantPersonID == seed.PersonId
                          && x.ApplicationTypeID == 2)
                .ToListAsync();

        Assert.Single(
            renewalApplications);

        Assert.Equal(
            AppStatus.Completed,
            renewalApplications[0].ApplicationStatus);
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
        SeedExpiredLicenseScenarioAsync(
            SqlServerApiWebApplicationFactory factory)
    {
        await using var context =
            factory.CreateDbContext();

        /*
         * ---------------------------------------------------------
         * Lookup data
         * ---------------------------------------------------------
         */

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
                    2,
                    N'Renew Driving License',
                    5
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

        /*
         * ---------------------------------------------------------
         * Country
         * ---------------------------------------------------------
         */

        var country =
            new Country
            {
                CountryName =
                    $"Integration Country {Guid.NewGuid():N}"
            };

        context.Countries.Add(
            country);

        await context.SaveChangesAsync();

        /*
         * ---------------------------------------------------------
         * People
         * ---------------------------------------------------------
         */

        var userPerson =
            new Person
            {
                NationalNo =
                    CreateNationalNumber(),

                FirstName =
                    "Integration",

                SecondName =
                    "Test",

                ThirdName =
                    null,

                LastName =
                    "User",

                DateOfBirth =
                    new DateTime(
                        1990,
                        1,
                        1),

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
                    "Renewal",

                SecondName =
                    "Integration",

                ThirdName =
                    null,

                LastName =
                    "Applicant",

                DateOfBirth =
                    new DateTime(
                        1990,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Renewal Applicant Address",

                Phone =
                    CreatePhone(),

                Email =
                    $"renewal-{Guid.NewGuid():N}@test.local",

                NationalityCountryID =
                    country.CountryId
            };

        context.People.AddRange(
            userPerson,
            applicantPerson);

        await context.SaveChangesAsync();

        /*
         * ---------------------------------------------------------
         * User
         * ---------------------------------------------------------
         */

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

        context.Users.Add(
            user);

        await context.SaveChangesAsync();

        /*
         * ---------------------------------------------------------
         * Driver
         * ---------------------------------------------------------
         */

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

        context.Drivers.Add(
            driver);

        await context.SaveChangesAsync();

        /*
         * ---------------------------------------------------------
         * Original Application
         * ---------------------------------------------------------
         */

        var application =
            new ApplicationD
            {
                ApplicantPersonID =
                    applicantPerson.PersonId,

                ApplicationDate =
                    DateTime.UtcNow.AddYears(-10),

                ApplicationTypeID =
                    1,

                ApplicationStatus =
                    AppStatus.Completed,

                LastStatusDate =
                    DateTime.UtcNow.AddYears(-10),

                PaidFees =
                    20m,

                CreatedByUserID =
                    user.UserId
            };

        context.Applications.Add(
            application);

        await context.SaveChangesAsync();

        /*
         * ---------------------------------------------------------
         * Expired License
         * ---------------------------------------------------------
         */

        var issueDate =
            DateTime.UtcNow.AddYears(-10);

        var expirationDate =
            DateTime.UtcNow.AddDays(-1);

        var oldLicense =
            new License
            {
                ApplicationID =
                    application.ApplicationID,

                DriverID =
                    driver.DriverID,

                LicenseClass =
                    1,

                IssueDate =
                    issueDate,

                ExpirationDate =
                    expirationDate,

                Notes =
                    "Expired integration-test license",

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
            oldLicense);

        await context.SaveChangesAsync();

        return new SeedData(
            UserId:
                user.UserId,

            PersonId:
                applicantPerson.PersonId,

            DriverId:
                driver.DriverID,

            ApplicationId:
                application.ApplicationID,

            OldLicenseId:
                oldLicense.LicenseID,

            LicenseClassId:
                1,

            ClassFees:
                100m,

            RenewalApplicationFees:
                5m,

            ValidityYears:
                5,

            BeforeRequestUtc:
                DateTime.UtcNow);
    }


    private static string CreateNationalNumber()
    {
        return
            Guid.NewGuid()
                .ToString("N")[..18];
    }


    private static string CreatePhone()
    {
        return
            $"07{Random.Shared.NextInt64(
                100000000,
                999999999)}";
    }


    private sealed record SeedData(
        int UserId,
        int PersonId,
        int DriverId,
        int ApplicationId,
        int OldLicenseId,
        int LicenseClassId,
        decimal ClassFees,
        decimal RenewalApplicationFees,
        int ValidityYears,
        DateTime BeforeRequestUtc);
}