using API.IntegrationTests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Workflows;

public sealed class LocalDrivingLicenseApplicationWorkflowTests
{
    private const int ApplicationTypeId = 1;
    private const int OriginalLicenseClassId = 1;
    private const int NewLicenseClassId = 2;

    [Fact]
    public async Task Create_WhenWorkflowIsValid_CreatesApplicationAndLocalApplication()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedCreateScenarioAsync(factory);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var request = new CreateLocalDrivingLicenseApplicationRequest
        {
            ApplicantPersonId = seed.ApplicantPersonId,
            LicenseClassId = seed.LicenseClassId
        };

        var response = await client.PostAsJsonAsync(
            "/api/LocalDrivingLicenseApplications", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<CreateLocalDrivingLicenseApplicationResponse>();

        Assert.NotNull(result);
        Assert.True(result.LocalDrivingLicenseApplicationId > 0);

        await using var context = factory.CreateDbContext();

        var localApplication = await context.LocalDrivingLicenseApplications
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.LocalDrivingLicenseApplicationID ==
                result.LocalDrivingLicenseApplicationId);

        Assert.NotNull(localApplication);
        Assert.Equal(seed.LicenseClassId, localApplication.LicenseClassID);

        var application = await context.Applications
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.ApplicationID == localApplication.ApplicationID);

        Assert.NotNull(application);
        Assert.Equal(seed.ApplicantPersonId, application.ApplicantPersonID);
        Assert.Equal(ApplicationTypeId, application.ApplicationTypeID);
        Assert.Equal(AppStatus.New, application.ApplicationStatus);
        Assert.Equal(seed.ApplicationFees, application.PaidFees);
        Assert.Equal(seed.UserId, application.CreatedByUserID);
    }

    [Fact]
    public async Task Create_WhenLicenseClassIdIsInvalid_ReturnsBadRequestAndDoesNotCreateAnything()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedCreateScenarioAsync(factory);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var request = new CreateLocalDrivingLicenseApplicationRequest
        {
            ApplicantPersonId = seed.ApplicantPersonId,
            LicenseClassId = 0
        };

        var response = await client.PostAsJsonAsync(
            "/api/LocalDrivingLicenseApplications", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var context = factory.CreateDbContext();

        Assert.Equal(
            0,
            await context.LocalDrivingLicenseApplications.CountAsync());

        Assert.Equal(
            0,
            await context.Applications.CountAsync());
    }

    [Fact]
    public async Task Create_WhenUnauthenticated_ReturnsUnauthorized()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedCreateScenarioAsync(factory);

        using var client = factory.CreateClient();

        var request = new CreateLocalDrivingLicenseApplicationRequest
        {
            ApplicantPersonId = seed.ApplicantPersonId,
            LicenseClassId = seed.LicenseClassId
        };

        var response = await client.PostAsJsonAsync(
            "/api/LocalDrivingLicenseApplications", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenDuplicateApplicationExists_ReturnsConflictAndDoesNotCreateAnotherApplication()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();

        var seed = await SeedCreateScenarioAsync(
            factory,
            createExistingLocalApplication: true);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var request = new CreateLocalDrivingLicenseApplicationRequest
        {
            ApplicantPersonId = seed.ApplicantPersonId,
            LicenseClassId = seed.LicenseClassId
        };

        var response = await client.PostAsJsonAsync(
            "/api/LocalDrivingLicenseApplications", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var context = factory.CreateDbContext();

        var localApplications = await context.LocalDrivingLicenseApplications
            .AsNoTracking()
            .Where(x => x.ApplicationID == seed.ExistingApplicationId)
            .ToListAsync();

        Assert.Single(localApplications);

        var applications = await context.Applications
            .AsNoTracking()
            .Where(x =>
                x.ApplicantPersonID == seed.ApplicantPersonId &&
                x.ApplicationTypeID == ApplicationTypeId)
            .ToListAsync();

        Assert.Single(applications);
    }

    [Fact]
    public async Task Create_WhenLocalApplicationCreationFails_RollsBackMainApplication()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedCreateScenarioAsync(factory);

        await CreateLocalApplicationInsertFailureConstraintAsync(factory);

        try
        {
            using var client = factory.CreateClient();
            ConfigureAuthenticatedClient(client, seed.UserId);

            var request = new CreateLocalDrivingLicenseApplicationRequest
            {
                ApplicantPersonId = seed.ApplicantPersonId,
                LicenseClassId = seed.LicenseClassId
            };

            var response = await client.PostAsJsonAsync(
                "/api/LocalDrivingLicenseApplications", request);

            Assert.Equal(
                HttpStatusCode.InternalServerError,
                response.StatusCode);

            await using var context = factory.CreateDbContext();

            Assert.Empty(
                await context.LocalDrivingLicenseApplications
                    .AsNoTracking()
                    .ToListAsync());

            Assert.Empty(
                await context.Applications
                    .AsNoTracking()
                    .ToListAsync());
        }
        finally
        {
            await RemoveLocalApplicationInsertFailureConstraintAsync(factory);
        }
    }

    [Fact]
    public async Task Create_WhenTwoRequestsRunConcurrently_AllowsOnlyOneApplication()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedCreateScenarioAsync(factory);

        using var client1 = factory.CreateClient();
        using var client2 = factory.CreateClient();

        ConfigureAuthenticatedClient(client1, seed.UserId);
        ConfigureAuthenticatedClient(client2, seed.UserId);

        var request = new CreateLocalDrivingLicenseApplicationRequest
        {
            ApplicantPersonId = seed.ApplicantPersonId,
            LicenseClassId = seed.LicenseClassId
        };

        var gate = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> SendAsync(HttpClient client)
        {
            await gate.Task;

            return await client.PostAsJsonAsync(
                "/api/LocalDrivingLicenseApplications", request);
        }

        var request1 = SendAsync(client1);
        var request2 = SendAsync(client2);

        gate.SetResult();

        var responses = await Task.WhenAll(request1, request2);

        var statusCodes = responses
            .Select(x => x.StatusCode)
            .ToArray();

        Assert.Equal(2, statusCodes.Length);
        Assert.Contains(HttpStatusCode.Created, statusCodes);
        Assert.Contains(HttpStatusCode.Conflict, statusCodes);

        Assert.Equal(
            1,
            statusCodes.Count(x => x == HttpStatusCode.Created));

        Assert.Equal(
            1,
            statusCodes.Count(x => x == HttpStatusCode.Conflict));

        await using var context = factory.CreateDbContext();

        var applications = await context.Applications
            .AsNoTracking()
            .Where(x =>
                x.ApplicantPersonID == seed.ApplicantPersonId &&
                x.ApplicationTypeID == ApplicationTypeId)
            .ToListAsync();

        Assert.Single(applications);

        var localApplications = await context.LocalDrivingLicenseApplications
            .AsNoTracking()
            .Where(x => x.ApplicationID == applications[0].ApplicationID)
            .ToListAsync();

        Assert.Single(localApplications);
    }

    [Fact]
    public async Task Update_WhenWorkflowIsValid_ChangesLicenseClass()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedUpdateScenarioAsync(factory);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var request = new UpdateLocalDrivingLicenseApplicationRequest
        {
            LicenseClassId = seed.NewLicenseClassId
        };

        var response = await client.PutAsJsonAsync(
            $"/api/LocalDrivingLicenseApplications/{seed.LocalApplicationId}",
            request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var context = factory.CreateDbContext();

        var localApplication = await context.LocalDrivingLicenseApplications
            .AsNoTracking()
            .SingleAsync(x =>
                x.LocalDrivingLicenseApplicationID ==
                seed.LocalApplicationId);

        Assert.Equal(
            seed.NewLicenseClassId,
            localApplication.LicenseClassID);
    }

    [Fact]
    public async Task Update_WhenApplicationIsCompleted_ReturnsConflictAndDoesNotChangeLicenseClass()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();

        var seed = await SeedUpdateScenarioAsync(
            factory,
            AppStatus.Completed);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var request = new UpdateLocalDrivingLicenseApplicationRequest
        {
            LicenseClassId = seed.NewLicenseClassId
        };

        var response = await client.PutAsJsonAsync(
            $"/api/LocalDrivingLicenseApplications/{seed.LocalApplicationId}",
            request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var context = factory.CreateDbContext();

        var localApplication = await context.LocalDrivingLicenseApplications
            .AsNoTracking()
            .SingleAsync(x =>
                x.LocalDrivingLicenseApplicationID ==
                seed.LocalApplicationId);

        Assert.Equal(
            seed.OriginalLicenseClassId,
            localApplication.LicenseClassID);
    }

    [Fact]
    public async Task Update_WhenAnotherApplicationAlreadyUsesTargetClass_ReturnsConflictAndDoesNotChangeLicenseClass()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();

        var seed = await SeedUpdateDuplicateScenarioAsync(factory);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var request = new UpdateLocalDrivingLicenseApplicationRequest
        {
            LicenseClassId = seed.TargetLicenseClassId
        };

        var response = await client.PutAsJsonAsync(
            $"/api/LocalDrivingLicenseApplications/{seed.LocalApplicationId}",
            request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var context = factory.CreateDbContext();

        var localApplication = await context.LocalDrivingLicenseApplications
            .AsNoTracking()
            .SingleAsync(x =>
                x.LocalDrivingLicenseApplicationID ==
                seed.LocalApplicationId);

        Assert.Equal(
            seed.OriginalLicenseClassId,
            localApplication.LicenseClassID);
    }

    [Fact]
    public async Task Update_WhenTargetClassIsSame_ReturnsNoContentAndKeepsExistingValue()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedUpdateScenarioAsync(factory);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var request = new UpdateLocalDrivingLicenseApplicationRequest
        {
            LicenseClassId = seed.OriginalLicenseClassId
        };

        var response = await client.PutAsJsonAsync(
            $"/api/LocalDrivingLicenseApplications/{seed.LocalApplicationId}",
            request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var context = factory.CreateDbContext();

        var localApplication = await context.LocalDrivingLicenseApplications
            .AsNoTracking()
            .SingleAsync(x =>
                x.LocalDrivingLicenseApplicationID ==
                seed.LocalApplicationId);

        Assert.Equal(
            seed.OriginalLicenseClassId,
            localApplication.LicenseClassID);
    }

    [Fact]
    public async Task Delete_WhenApplicationIsNew_DeletesLocalApplication()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();

        var seed = await SeedCreateScenarioAsync(
            factory,
            createExistingLocalApplication: true);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var response = await client.DeleteAsync(
            $"/api/LocalDrivingLicenseApplications/{seed.LocalApplicationId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var context = factory.CreateDbContext();

        Assert.False(
            await context.LocalDrivingLicenseApplications
                .AsNoTracking()
                .AnyAsync(x =>
                    x.LocalDrivingLicenseApplicationID ==
                    seed.LocalApplicationId));

        Assert.True(
            await context.Applications
                .AsNoTracking()
                .AnyAsync(x =>
                    x.ApplicationID == seed.ExistingApplicationId));
    }

    [Fact]
    public async Task Delete_WhenApplicationIsCompleted_ReturnsConflictAndDoesNotDelete()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();

        var seed = await SeedCreateScenarioAsync(
            factory,
            createExistingLocalApplication: true,
            applicationStatus: AppStatus.Completed);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var response = await client.DeleteAsync(
            $"/api/LocalDrivingLicenseApplications/{seed.LocalApplicationId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var context = factory.CreateDbContext();

        Assert.True(
            await context.LocalDrivingLicenseApplications
                .AsNoTracking()
                .AnyAsync(x =>
                    x.LocalDrivingLicenseApplicationID ==
                    seed.LocalApplicationId));
    }

    [Fact]
    public async Task Cancel_WhenApplicationIsNew_CancelsMainApplication()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();

        var seed = await SeedCreateScenarioAsync(
            factory,
            createExistingLocalApplication: true);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var response = await client.PostAsync(
            $"/api/LocalDrivingLicenseApplications/{seed.LocalApplicationId}/cancel",
            null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var context = factory.CreateDbContext();

        var application = await context.Applications
            .AsNoTracking()
            .SingleAsync(x =>
                x.ApplicationID == seed.ExistingApplicationId);

        Assert.Equal(AppStatus.Cancelled, application.ApplicationStatus);
    }

    [Fact]
    public async Task Cancel_WhenLocalApplicationDoesNotExist_ReturnsNotFound()
    {
        await using var factory = new SqlServerApiWebApplicationFactory();
        var seed = await SeedCreateScenarioAsync(factory);

        using var client = factory.CreateClient();
        ConfigureAuthenticatedClient(client, seed.UserId);

        var response = await client.PostAsync(
            "/api/LocalDrivingLicenseApplications/999999/cancel",
            null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static void ConfigureAuthenticatedClient(
        HttpClient client,
        int userId)
    {
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Username", "integration.test");
        client.DefaultRequestHeaders.Add("X-Test-FullName", "Integration Test User");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Staff");
    }

    private static async Task<SeedData> SeedCreateScenarioAsync(
        SqlServerApiWebApplicationFactory factory,
        bool createExistingLocalApplication = false,
        AppStatus applicationStatus = AppStatus.New)
    {
        await using var context = factory.CreateDbContext();

        await SeedLookupDataAsync(context);

        var country = new Country
        {
            CountryName = $"Integration Country {Guid.NewGuid():N}"
        };

        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var userPerson = CreatePerson(
            CreateNationalNumber(),
            "Integration",
            "Test",
            "User",
            country.CountryId,
            "Integration Test Address",
            "user");

        var applicantPerson = CreatePerson(
            CreateNationalNumber(),
            "Applicant",
            "Integration",
            "Person",
            country.CountryId,
            "Applicant Test Address",
            "applicant");

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

        if (!createExistingLocalApplication)
        {
            return new SeedData(
                user.UserId,
                applicantPerson.PersonId,
                0,
                0,
                OriginalLicenseClassId,
                20m);
        }

        var application = new ApplicationD
        {
            ApplicantPersonID = applicantPerson.PersonId,
            ApplicationDate = DateTime.UtcNow,
            ApplicationTypeID = ApplicationTypeId,
            ApplicationStatus = applicationStatus,
            LastStatusDate = DateTime.UtcNow,
            PaidFees = 20m,
            CreatedByUserID = user.UserId
        };

        context.Applications.Add(application);
        await context.SaveChangesAsync();

        var localApplication = new LocalDrivingLicenseApplication
        {
            ApplicationID = application.ApplicationID,
            LicenseClassID = OriginalLicenseClassId
        };

        context.LocalDrivingLicenseApplications.Add(localApplication);
        await context.SaveChangesAsync();

        return new SeedData(
            user.UserId,
            applicantPerson.PersonId,
            application.ApplicationID,
            localApplication.LocalDrivingLicenseApplicationID,
            OriginalLicenseClassId,
            20m);
    }

    private static async Task<UpdateSeedData> SeedUpdateScenarioAsync(
        SqlServerApiWebApplicationFactory factory,
        AppStatus applicationStatus = AppStatus.New)
    {
        await using var context = factory.CreateDbContext();

        await SeedLookupDataAsync(
            context,
            includeSecondLicenseClass: true);

        var country = await CreateCountryAsync(context);

        var userPerson = CreatePerson(
            CreateNationalNumber(),
            "Update",
            "Integration",
            "User",
            country.CountryId,
            "Update Address",
            "update-user");

        var applicantPerson = CreatePerson(
            CreateNationalNumber(),
            "Update",
            "Integration",
            "Applicant",
            country.CountryId,
            "Update Applicant Address",
            "update-applicant");

        context.People.AddRange(userPerson, applicantPerson);
        await context.SaveChangesAsync();

        var user = await CreateUserAsync(context, userPerson.PersonId, "update-user");

        var application = new ApplicationD
        {
            ApplicantPersonID = applicantPerson.PersonId,
            ApplicationDate = DateTime.UtcNow,
            ApplicationTypeID = ApplicationTypeId,
            ApplicationStatus = applicationStatus,
            LastStatusDate = DateTime.UtcNow,
            PaidFees = 20m,
            CreatedByUserID = user.UserId
        };

        context.Applications.Add(application);
        await context.SaveChangesAsync();

        var localApplication = new LocalDrivingLicenseApplication
        {
            ApplicationID = application.ApplicationID,
            LicenseClassID = OriginalLicenseClassId
        };

        context.LocalDrivingLicenseApplications.Add(localApplication);
        await context.SaveChangesAsync();

        return new UpdateSeedData(
            user.UserId,
            localApplication.LocalDrivingLicenseApplicationID,
            OriginalLicenseClassId,
            NewLicenseClassId);
    }

    private static async Task<UpdateDuplicateSeedData>
        SeedUpdateDuplicateScenarioAsync(
            SqlServerApiWebApplicationFactory factory)
    {
        await using var context = factory.CreateDbContext();

        await SeedLookupDataAsync(
            context,
            includeSecondLicenseClass: true);

        var country = await CreateCountryAsync(context);

        var userPerson = CreatePerson(
            CreateNationalNumber(),
            "Duplicate",
            "Integration",
            "User",
            country.CountryId,
            "Duplicate User Address",
            "duplicate-user");

        var applicantPerson = CreatePerson(
            CreateNationalNumber(),
            "Duplicate",
            "Integration",
            "Applicant",
            country.CountryId,
            "Duplicate Applicant Address",
            "duplicate-applicant");

        context.People.AddRange(userPerson, applicantPerson);
        await context.SaveChangesAsync();

        var user = await CreateUserAsync(
            context,
            userPerson.PersonId,
            "duplicate-user");

        var firstApplication = CreateApplication(
            applicantPerson.PersonId,
            user.UserId);

        var secondApplication = CreateApplication(
            applicantPerson.PersonId,
            user.UserId);

        context.Applications.AddRange(
            firstApplication,
            secondApplication);

        await context.SaveChangesAsync();

        var firstLocalApplication = new LocalDrivingLicenseApplication
        {
            ApplicationID = firstApplication.ApplicationID,
            LicenseClassID = OriginalLicenseClassId
        };

        var secondLocalApplication = new LocalDrivingLicenseApplication
        {
            ApplicationID = secondApplication.ApplicationID,
            LicenseClassID = NewLicenseClassId
        };

        context.LocalDrivingLicenseApplications.AddRange(
            firstLocalApplication,
            secondLocalApplication);

        await context.SaveChangesAsync();

        return new UpdateDuplicateSeedData(
            user.UserId,
            firstLocalApplication.LocalDrivingLicenseApplicationID,
            OriginalLicenseClassId,
            NewLicenseClassId);
    }

    private static async Task<Country> CreateCountryAsync(
        DVLDDbContext context)
    {
        var country = new Country
        {
            CountryName = $"Integration Country {Guid.NewGuid():N}"
        };

        context.Countries.Add(country);
        await context.SaveChangesAsync();

        return country;
    }

    private static Person CreatePerson(
        string nationalNumber,
        string firstName,
        string secondName,
        string lastName,
        int countryId,
        string address,
        string emailPrefix)
    {
        return new Person
        {
            NationalNo = nationalNumber,
            FirstName = firstName,
            SecondName = secondName,
            LastName = lastName,
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = Gender.Male,
            Address = address,
            Phone = CreatePhone(),
            Email = $"{emailPrefix}-{Guid.NewGuid():N}@test.local",
            NationalityCountryID = countryId
        };
    }

    private static async Task<User> CreateUserAsync(
        DVLDDbContext context,
        int personId,
        string usernamePrefix)
    {
        var user = new User
        {
            PersonId = personId,
            UserName = $"{usernamePrefix}-{Guid.NewGuid():N}",
            Password = "TestPassword",
            IsActive = true,
            Role = UserRole.Staff
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    private static ApplicationD CreateApplication(
        int applicantPersonId,
        int userId,
        AppStatus status = AppStatus.New)
    {
        return new ApplicationD
        {
            ApplicantPersonID = applicantPersonId,
            ApplicationDate = DateTime.UtcNow,
            ApplicationTypeID = ApplicationTypeId,
            ApplicationStatus = status,
            LastStatusDate = DateTime.UtcNow,
            PaidFees = 20m,
            CreatedByUserID = userId
        };
    }

    private static async Task SeedLookupDataAsync(
        DVLDDbContext context,
        bool includeSecondLicenseClass = false)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            SET IDENTITY_INSERT ApplicationTypes ON;

            INSERT INTO ApplicationTypes
                (ApplicationTypeId, ApplicationTypeTitle, ApplicationFees)
            VALUES
                (1, N'New Local Driving License', 20);

            SET IDENTITY_INSERT ApplicationTypes OFF;
            """);

        if (!includeSecondLicenseClass)
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                SET IDENTITY_INSERT LicenseClasses ON;

                INSERT INTO LicenseClasses
                    (LicenseClassID, ClassName, ClassDescription,
                     MinimumAllowedAge, DefaultValidityLength, ClassFees)
                VALUES
                    (1, N'Integration Class 1',
                     N'Integration test license class 1',
                     18, 5, 100);

                SET IDENTITY_INSERT LicenseClasses OFF;
                """);

            return;
        }

        await context.Database.ExecuteSqlRawAsync(
            """
            SET IDENTITY_INSERT LicenseClasses ON;

            INSERT INTO LicenseClasses
                (LicenseClassID, ClassName, ClassDescription,
                 MinimumAllowedAge, DefaultValidityLength, ClassFees)
            VALUES
                (1, N'Integration Class 1',
                 N'Integration test license class 1',
                 18, 5, 100),
                (2, N'Integration Class 2',
                 N'Integration test license class 2',
                 18, 5, 150);

            SET IDENTITY_INSERT LicenseClasses OFF;
            """);
    }

    private static async Task
        CreateLocalApplicationInsertFailureConstraintAsync(
            SqlServerApiWebApplicationFactory factory)
    {
        await using var context = factory.CreateDbContext();

        await context.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE [LocalDrivingLicenseApplications]
            ADD CONSTRAINT
                [CK_Test_LocalDrivingLicenseApplications_ForceFailure]
            CHECK ([LicenseClassID] <> 1);
            """);
    }

    private static async Task
        RemoveLocalApplicationInsertFailureConstraintAsync(
            SqlServerApiWebApplicationFactory factory)
    {
        await using var context = factory.CreateDbContext();

        await context.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE [LocalDrivingLicenseApplications]
            DROP CONSTRAINT
                [CK_Test_LocalDrivingLicenseApplications_ForceFailure];
            """);
    }

    private static string CreateNationalNumber() =>
        Guid.NewGuid().ToString("N")[..18];

    private static string CreatePhone() =>
        $"07{Random.Shared.NextInt64(100000000, 999999999)}";

    private sealed record SeedData(
        int UserId,
        int ApplicantPersonId,
        int ExistingApplicationId,
        int LocalApplicationId,
        int LicenseClassId,
        decimal ApplicationFees);

    private sealed record UpdateSeedData(
        int UserId,
        int LocalApplicationId,
        int OriginalLicenseClassId,
        int NewLicenseClassId);

    private sealed record UpdateDuplicateSeedData(
        int UserId,
        int LocalApplicationId,
        int OriginalLicenseClassId,
        int TargetLicenseClassId);
}