using API.IntegrationTests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using DVLD.Contracts.InternationalLicense;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Workflows;

public sealed class InternationalLicenseWorkflowTests
{
    private const int InternationalApplicationTypeId = 6;
    private const int OrdinaryLicenseClassId = 3;
    private const int AlternativeLicenseClassId = 2;

    [Fact]
    public async Task IssueInternationalLicense_WhenWorkflowIsValid_CreatesLicenseAndCompletesApplication()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var request =
            new IssueInternationalLicenseRequest(
                seed.LocalLicenseId);

        var response =
            await client.PostAsJsonAsync(
                "/api/InternationalLicenses",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<InternationalLicenseResponse>();

        Assert.NotNull(result);

        Assert.True(
            result.InternationalLicenseId > 0);

        Assert.True(
            result.ApplicationId > 0);

        Assert.Equal(
            seed.LocalLicenseId,
            result.IssuedUsingLocalLicenseId);

        Assert.Equal(
            seed.DriverId,
            result.DriverId);

        Assert.Equal(
            seed.PersonId,
            result.PersonId);

        Assert.Equal(
            seed.UserId,
            result.CreatedByUserId);

        Assert.Equal(
            seed.FullName,
            result.FullName);

        Assert.Equal(
            seed.NationalNo,
            result.NationalNo);

        Assert.Equal(
            "Male",
            result.Gender);

        Assert.True(
            result.IsActive);

        Assert.True(
            result.IssueDate >= seed.BeforeRequest);

        Assert.True(
            result.IssueDate <= DateTime.UtcNow);

        Assert.Equal(
            result.IssueDate.AddYears(1),
            result.ExpirationDate);

        Assert.Equal(
            50m,
            result.Fees);

        Assert.Equal(
            seed.Username,
            result.CreatedByUserName);

        await using var verificationContext =
            factory.CreateDbContext();

        var internationalLicense =
            await verificationContext
                .InternationalLicenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.InternationalLicenseID ==
                        result.InternationalLicenseId);

        Assert.NotNull(
            internationalLicense);

        Assert.Equal(
            result.ApplicationId,
            internationalLicense.ApplicationID);

        Assert.Equal(
            seed.DriverId,
            internationalLicense.DriverID);

        Assert.Equal(
            seed.LocalLicenseId,
            internationalLicense.IssuedUsingLocalLicenseID);

        Assert.Equal(
            seed.UserId,
            internationalLicense.CreatedByUserID);

        Assert.True(
            internationalLicense.IsActive);

        Assert.Equal(
            result.IssueDate,
            internationalLicense.IssueDate);

        Assert.Equal(
            result.ExpirationDate,
            internationalLicense.ExpirationDate);

        var application =
            await verificationContext
                .Applications
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.ApplicationID ==
                        result.ApplicationId);

        Assert.NotNull(
            application);

        Assert.Equal(
            seed.PersonId,
            application.ApplicantPersonID);

        Assert.Equal(
            InternationalApplicationTypeId,
            application.ApplicationTypeID);

        Assert.Equal(
            AppStatus.Completed,
            application.ApplicationStatus);

        Assert.Equal(
            50m,
            application.PaidFees);

        Assert.Equal(
            seed.UserId,
            application.CreatedByUserID);

        var internationalLicensesForLocalLicense =
            await verificationContext
                .InternationalLicenses
                .AsNoTracking()
                .Where(
                    x =>
                        x.IssuedUsingLocalLicenseID ==
                        seed.LocalLicenseId)
                .ToListAsync();

        Assert.Single(
            internationalLicensesForLocalLicense);
    }

    [Fact]
    public async Task IssueInternationalLicense_WhenLocalLicenseDoesNotExist_ReturnsNotFoundAndCreatesNothing()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var response =
            await client.PostAsJsonAsync(
                "/api/InternationalLicenses",
                new IssueInternationalLicenseRequest(
                    999999));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            "License not found.",
            error.Error);

        await using var verificationContext =
            factory.CreateDbContext();

        var internationalLicenses =
            await verificationContext
                .InternationalLicenses
                .AsNoTracking()
                .ToListAsync();

        Assert.Empty(
            internationalLicenses);

        var internationalApplications =
            await verificationContext
                .Applications
                .AsNoTracking()
                .Where(
                    x =>
                        x.ApplicationTypeID ==
                        InternationalApplicationTypeId &&
                        x.ApplicantPersonID ==
                        seed.PersonId)
                .ToListAsync();

        Assert.Empty(
            internationalApplications);
    }

    [Fact]
    public async Task IssueInternationalLicense_WhenLicenseClassIsNotOrdinary_ReturnsConflictAndCreatesNothing()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidScenarioAsync(factory);

        await using (
            var context =
                factory.CreateDbContext())
        {
            var affectedRows =
                await context
                    .Licenses
                    .Where(
                        x =>
                            x.LicenseID ==
                            seed.LocalLicenseId)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                x =>
                                    x.LicenseClass,
                                AlternativeLicenseClassId));

            Assert.Equal(
                1,
                affectedRows);
        }

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var response =
            await client.PostAsJsonAsync(
                "/api/InternationalLicenses",
                new IssueInternationalLicenseRequest(
                    seed.LocalLicenseId));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            "Only class 3 licenses can be issued internationally.",
            error.Error);

        await using var verificationContext =
            factory.CreateDbContext();

        Assert.Empty(
            await verificationContext
                .InternationalLicenses
                .AsNoTracking()
                .ToListAsync());

        var internationalApplications =
            await verificationContext
                .Applications
                .AsNoTracking()
                .Where(
                    x =>
                        x.ApplicationTypeID ==
                        InternationalApplicationTypeId &&
                        x.ApplicantPersonID ==
                        seed.PersonId)
                .ToListAsync();

        Assert.Empty(
            internationalApplications);
    }

    [Fact]
    public async Task IssueInternationalLicense_WhenLocalLicenseIsInactive_ReturnsConflictAndCreatesNothing()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidScenarioAsync(factory);

        await using (
            var context =
                factory.CreateDbContext())
        {
            var affectedRows =
                await context
                    .Licenses
                    .Where(
                        x =>
                            x.LicenseID ==
                            seed.LocalLicenseId)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                x =>
                                    x.IsActive,
                                false));

            Assert.Equal(
                1,
                affectedRows);
        }

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var response =
            await client.PostAsJsonAsync(
                "/api/InternationalLicenses",
                new IssueInternationalLicenseRequest(
                    seed.LocalLicenseId));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            "The local license is not active.",
            error.Error);

        await using var verificationContext =
            factory.CreateDbContext();

        Assert.Empty(
            await verificationContext
                .InternationalLicenses
                .AsNoTracking()
                .ToListAsync());

        var internationalApplications =
            await verificationContext
                .Applications
                .AsNoTracking()
                .Where(
                    x =>
                        x.ApplicationTypeID ==
                        InternationalApplicationTypeId &&
                        x.ApplicantPersonID ==
                        seed.PersonId)
                .ToListAsync();

        Assert.Empty(
            internationalApplications);
    }

    [Fact]
    public async Task IssueInternationalLicense_WhenLocalLicenseIsExpired_ReturnsConflictAndCreatesNothing()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidScenarioAsync(factory);

        await using (
            var context =
                factory.CreateDbContext())
        {
            var affectedRows =
                await context
                    .Licenses
                    .Where(
                        x =>
                            x.LicenseID ==
                            seed.LocalLicenseId)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                x =>
                                    x.ExpirationDate,
                                DateTime.UtcNow.AddDays(-1)));

            Assert.Equal(
                1,
                affectedRows);
        }

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var response =
            await client.PostAsJsonAsync(
                "/api/InternationalLicenses",
                new IssueInternationalLicenseRequest(
                    seed.LocalLicenseId));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            "The local license is expired.",
            error.Error);

        await using var verificationContext =
            factory.CreateDbContext();

        Assert.Empty(
            await verificationContext
                .InternationalLicenses
                .AsNoTracking()
                .ToListAsync());

        var internationalApplications =
            await verificationContext
                .Applications
                .AsNoTracking()
                .Where(
                    x =>
                        x.ApplicationTypeID ==
                        InternationalApplicationTypeId &&
                        x.ApplicantPersonID ==
                        seed.PersonId)
                .ToListAsync();

        Assert.Empty(
            internationalApplications);
    }

    [Fact]
    public async Task IssueInternationalLicense_WhenInternationalLicenseAlreadyExistsForLocalLicense_ReturnsConflictAndDoesNotCreateSecondApplication()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client,
            seed.UserId);

        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/InternationalLicenses",
                new IssueInternationalLicenseRequest(
                    seed.LocalLicenseId));

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var firstResult =
            await firstResponse.Content
                .ReadFromJsonAsync<InternationalLicenseResponse>();

        Assert.NotNull(
            firstResult);

        var secondResponse =
            await client.PostAsJsonAsync(
                "/api/InternationalLicenses",
                new IssueInternationalLicenseRequest(
                    seed.LocalLicenseId));

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);

        var error =
            await secondResponse.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            "An international license already exists for this local license.",
            error.Error);

        await using var verificationContext =
            factory.CreateDbContext();

        var licenses =
            await verificationContext
                .InternationalLicenses
                .AsNoTracking()
                .Where(
                    x =>
                        x.IssuedUsingLocalLicenseID ==
                        seed.LocalLicenseId)
                .ToListAsync();

        Assert.Single(
            licenses);

        Assert.Equal(
            firstResult.InternationalLicenseId,
            licenses[0].InternationalLicenseID);

        var applications =
            await verificationContext
                .Applications
                .AsNoTracking()
                .Where(
                    x =>
                        x.ApplicationTypeID ==
                        InternationalApplicationTypeId &&
                        x.ApplicantPersonID ==
                        seed.PersonId)
                .ToListAsync();

        Assert.Single(
            applications);

        Assert.Equal(
            firstResult.ApplicationId,
            applications[0].ApplicationID);

        Assert.Equal(
            AppStatus.Completed,
            applications[0].ApplicationStatus);
    }

    [Fact]
    public async Task IssueInternationalLicense_WhenSavingInternationalLicenseFails_RollsBackCreatedApplication()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidScenarioAsync(factory);

        await CreateAlwaysFailingInternationalLicenseConstraintAsync(
            factory);

        try
        {
            using var client =
                factory.CreateClient();

            ConfigureAuthenticatedClient(
                client,
                seed.UserId);

            var response =
                await client.PostAsJsonAsync(
                    "/api/InternationalLicenses",
                    new IssueInternationalLicenseRequest(
                        seed.LocalLicenseId));

            Assert.Equal(
                HttpStatusCode.InternalServerError,
                response.StatusCode);

            await using var verificationContext =
                factory.CreateDbContext();

            var internationalLicenses =
                await verificationContext
                    .InternationalLicenses
                    .AsNoTracking()
                    .ToListAsync();

            Assert.Empty(
                internationalLicenses);

            var internationalApplications =
                await verificationContext
                    .Applications
                    .AsNoTracking()
                    .Where(
                        x =>
                            x.ApplicationTypeID ==
                            InternationalApplicationTypeId &&
                            x.ApplicantPersonID ==
                            seed.PersonId)
                    .ToListAsync();

            Assert.Empty(
                internationalApplications);
        }
        finally
        {
            await RemoveAlwaysFailingInternationalLicenseConstraintAsync(
                factory);
        }
    }

    [Fact]
    public async Task IssueInternationalLicense_WhenTwoRequestsRunConcurrently_AllowsOnlyOneInternationalLicense()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidScenarioAsync(factory);

        using var client1 =
            factory.CreateClient();

        using var client2 =
            factory.CreateClient();

        ConfigureAuthenticatedClient(
            client1,
            seed.UserId);

        ConfigureAuthenticatedClient(
            client2,
            seed.UserId);

        var request =
            new IssueInternationalLicenseRequest(
                seed.LocalLicenseId);

        var startGate =
            new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> SendAsync(
            HttpClient client)
        {
            await startGate.Task;

            return await client.PostAsJsonAsync(
                "/api/InternationalLicenses",
                request);
        }

        var request1 =
            SendAsync(client1);

        var request2 =
            SendAsync(client2);

        startGate.SetResult();

        var responses =
            await Task.WhenAll(
                request1,
                request2);

        var statusCodes =
            responses
                .Select(x => x.StatusCode)
                .OrderBy(x => x)
                .ToArray();

        Assert.Equal(
            2,
            statusCodes.Length);

        Assert.Equal(
            1,
            statusCodes.Count(
                x =>
                    x == HttpStatusCode.OK));

        Assert.Equal(
            1,
            statusCodes.Count(
                x =>
                    x == HttpStatusCode.Conflict));

        await using var verificationContext =
            factory.CreateDbContext();

        var internationalLicenses =
            await verificationContext
                .InternationalLicenses
                .AsNoTracking()
                .Where(
                    x =>
                        x.IssuedUsingLocalLicenseID ==
                        seed.LocalLicenseId)
                .ToListAsync();

        Assert.Single(
            internationalLicenses);

        var internationalApplications =
            await verificationContext
                .Applications
                .AsNoTracking()
                .Where(
                    x =>
                        x.ApplicationTypeID ==
                        InternationalApplicationTypeId &&
                        x.ApplicantPersonID ==
                        seed.PersonId)
                .ToListAsync();

        Assert.Single(
            internationalApplications);

        Assert.Equal(
            AppStatus.Completed,
            internationalApplications[0].ApplicationStatus);
    }

    [Fact]
    public async Task IssueInternationalLicense_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory =
            new SqlServerApiWebApplicationFactory();

        var seed =
            await SeedValidScenarioAsync(factory);

        using var client =
            factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/InternationalLicenses",
                new IssueInternationalLicenseRequest(
                    seed.LocalLicenseId));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        await using var verificationContext =
            factory.CreateDbContext();

        Assert.Empty(
            await verificationContext
                .InternationalLicenses
                .AsNoTracking()
                .ToListAsync());

        var internationalApplications =
            await verificationContext
                .Applications
                .AsNoTracking()
                .Where(
                    x =>
                        x.ApplicationTypeID ==
                        InternationalApplicationTypeId &&
                        x.ApplicantPersonID ==
                        seed.PersonId)
                .ToListAsync();

        Assert.Empty(
            internationalApplications);
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

    private static async Task<SeedData> SeedValidScenarioAsync(
        SqlServerApiWebApplicationFactory factory)
    {
        await using var context =
            factory.CreateDbContext();

        await SeedLookupDataAsync(
            context);

        var country =
            new Country
            {
                CountryName =
                    $"International Test Country {Guid.NewGuid():N}"
            };

        context.Countries.Add(
            country);

        await context.SaveChangesAsync();

        var userPerson =
            new Person
            {
                NationalNo =
                    CreateNationalNumber(),

                FirstName =
                    "Integration",

                SecondName =
                    "International",

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
                    "International",

                SecondName =
                    "License",

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
                    "International Applicant Address",

                Phone =
                    CreatePhone(),

                Email =
                    $"applicant-{Guid.NewGuid():N}@test.local",

                NationalityCountryID =
                    country.CountryId
            };

        context.People.AddRange(
            userPerson,
            applicantPerson);

        await context.SaveChangesAsync();

        var username =
            $"integration-{Guid.NewGuid():N}";

        var user =
            new User
            {
                PersonId =
                    userPerson.PersonId,

                UserName =
                    username,

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

        var application =
            new ApplicationD
            {
                ApplicantPersonID =
                    applicantPerson.PersonId,

                ApplicationDate =
                    DateTime.UtcNow.AddDays(-30),

                ApplicationTypeID =
                    1,

                ApplicationStatus =
                    AppStatus.Completed,

                LastStatusDate =
                    DateTime.UtcNow.AddDays(-30),

                PaidFees =
                    100m,

                CreatedByUserID =
                    user.UserId
            };

        context.Applications.Add(
            application);

        await context.SaveChangesAsync();

        var driver =
            new Driver
            {
                PersonID =
                    applicantPerson.PersonId,

                CreatedByUserID =
                    user.UserId,

                CreatedDate =
                    DateTime.UtcNow.AddDays(-20)
            };

        context.Drivers.Add(
            driver);

        await context.SaveChangesAsync();

        var localLicense =
            new License
            {
                ApplicationID =
                    application.ApplicationID,

                DriverID =
                    driver.DriverID,

                LicenseClass =
                    OrdinaryLicenseClassId,

                IssueDate =
                    DateTime.UtcNow.AddDays(-10),

                ExpirationDate =
                    DateTime.UtcNow.AddYears(2),

                Notes =
                    "International workflow test license",

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
            localLicense);

        await context.SaveChangesAsync();

        return new SeedData(
            UserId:
                user.UserId,

            Username:
                username,

            PersonId:
                applicantPerson.PersonId,

            DriverId:
                driver.DriverID,

            LocalLicenseId:
                localLicense.LicenseID,

            NationalNo:
                applicantPerson.NationalNo,

            FullName:
                applicantPerson.FullName,

            BeforeRequest:
                DateTime.UtcNow);
    }

    private static async Task SeedLookupDataAsync(
        DVLDDbContext context)
    {
        await InsertApplicationTypeAsync(
            context,
            1,
            "Local Driving License",
            100m);

        await InsertApplicationTypeAsync(
            context,
            InternationalApplicationTypeId,
            "International License",
            50m);

        await InsertLicenseClassAsync(
            context,
            AlternativeLicenseClassId,
            "Alternative Class",
            "International workflow alternative class",
            18,
            5,
            75m);

        await InsertLicenseClassAsync(
            context,
            OrdinaryLicenseClassId,
            "Ordinary Class",
            "International workflow ordinary class",
            18,
            5,
            100m);
    }

    private static async Task InsertApplicationTypeAsync(
        DVLDDbContext context,
        int id,
        string title,
        decimal fees)
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            SET IDENTITY_INSERT [ApplicationTypes] ON;

            INSERT INTO [ApplicationTypes]
            (
                [ApplicationTypeId],
                [ApplicationTypeTitle],
                [ApplicationFees]
            )
            VALUES
            (
                {id},
                {title},
                {fees}
            );

            SET IDENTITY_INSERT [ApplicationTypes] OFF;
            """);
    }

    private static async Task InsertLicenseClassAsync(
        DVLDDbContext context,
        int id,
        string name,
        string description,
        byte minimumAge,
        byte validityYears,
        decimal fees)
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            SET IDENTITY_INSERT [LicenseClasses] ON;

            INSERT INTO [LicenseClasses]
            (
                [LicenseClassID],
                [ClassName],
                [ClassDescription],
                [MinimumAllowedAge],
                [DefaultValidityLength],
                [ClassFees]
            )
            VALUES
            (
                {id},
                {name},
                {description},
                {minimumAge},
                {validityYears},
                {fees}
            );

            SET IDENTITY_INSERT [LicenseClasses] OFF;
            """);
    }

    private static async Task CreateAlwaysFailingInternationalLicenseConstraintAsync(
        SqlServerApiWebApplicationFactory factory)
    {
#pragma warning disable EF1002
        await using var context =
            factory.CreateDbContext();

        await context.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE [InternationalLicenses]
            ADD CONSTRAINT [CK_Test_InternationalLicenses_ForceFailure]
            CHECK (1 = 0);
            """);
#pragma warning restore EF1002
    }

    private static async Task RemoveAlwaysFailingInternationalLicenseConstraintAsync(
        SqlServerApiWebApplicationFactory factory)
    {
#pragma warning disable EF1002
        await using var context =
            factory.CreateDbContext();

        await context.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE [InternationalLicenses]
            DROP CONSTRAINT [CK_Test_InternationalLicenses_ForceFailure];
            """);
#pragma warning restore EF1002
    }

    private static string CreateNationalNumber() =>
        $"{Random.Shared.Next(10000000, 99999999)}";

    private static string CreatePhone() =>
        $"079{Random.Shared.Next(1000000, 9999999)}";

    private sealed record SeedData(
        int UserId,
        string Username,
        int PersonId,
        int DriverId,
        int LocalLicenseId,
        string NationalNo,
        string FullName,
        DateTime BeforeRequest);

    private sealed record ErrorResponse(
        string Error);
}