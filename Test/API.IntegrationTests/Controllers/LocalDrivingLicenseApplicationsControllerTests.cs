using System.Net;
using System.Net.Http.Json;
using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Domain.Enums;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using DVLD.Contracts.Application;
using Moq;

namespace API.IntegrationTests.Controllers;

public sealed class LocalDrivingLicenseApplicationsControllerTests
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LocalDrivingLicenseApplicationsControllerTests(
        ApiWebApplicationFactory factory)
    {
        _factory = factory;

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Reset();

        _client = factory.CreateClient();
    }

    // =========================================================
    // AUTHENTICATION
    // =========================================================

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await _client.GetAsync(
                "/api/LocalDrivingLicenseApplications");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedApplications()
    {
        var dto =
            CreateApplicationDto(10);

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetAllLocalDrivingLicenseApplicationsAsync())
            .ReturnsAsync(
                Result<List<LocalDrivingLicenseApplicationListDto>>
                    .Success(
                        new List<LocalDrivingLicenseApplicationListDto>
                        {
                            dto
                        }));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    List<LocalDrivingLicenseApplicationResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        AssertApplicationResponse(
            dto,
            result[0]);

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Verify(
                x =>
                    x.GetAllLocalDrivingLicenseApplicationsAsync(),
                Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetAllLocalDrivingLicenseApplicationsAsync())
            .ReturnsAsync(
                Result<List<LocalDrivingLicenseApplicationListDto>>
                    .FromValidationFailure(
                        "Invalid application data."));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid application data.");
    }

    [Fact]
    public async Task GetAll_WhenUnexpectedFailure_ReturnsInternalServerError()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetAllLocalDrivingLicenseApplicationsAsync())
            .ReturnsAsync(
                Result<List<LocalDrivingLicenseApplicationListDto>>
                    .FromFailure(
                        "Database failure."));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Database failure.");
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedApplication()
    {
        var dto =
            CreateApplicationDto(20);

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetLocalDrivingLicenseApplicationByIdAsync(20))
            .ReturnsAsync(
                Result<LocalDrivingLicenseApplicationListDto>
                    .Success(dto));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/20");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    LocalDrivingLicenseApplicationResponse>();

        Assert.NotNull(result);

        AssertApplicationResponse(
            dto,
            result);

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Verify(
                x =>
                    x.GetLocalDrivingLicenseApplicationByIdAsync(20),
                Times.Once);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetLocalDrivingLicenseApplicationByIdAsync(20))
            .ReturnsAsync(
                Result<LocalDrivingLicenseApplicationListDto>
                    .FromNotFound(
                        "Local driving license application not found."));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/20");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Local driving license application not found.");
    }

    [Fact]
    public async Task GetById_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetLocalDrivingLicenseApplicationByIdAsync(0))
            .ReturnsAsync(
                Result<LocalDrivingLicenseApplicationListDto>
                    .FromValidationFailure(
                        "Invalid local application ID."));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/0");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid local application ID.");
    }

    // =========================================================
    // GET APPLICATION BASIC INFO
    // =========================================================

    [Fact]
    public async Task GetApplicationBasicInfo_WhenSuccessful_ReturnsMappedInfo()
    {
        var dto =
            CreateApplicationBasicInfoDto();

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetApplicationBasicInfoAsync(30))
            .ReturnsAsync(
                Result<ApplicationBasicInfoDto>
                    .Success(dto));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/30/application-basic-info");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<ApplicationBasicInfoResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            dto.ApplicantPersonID,
            result.ApplicantPersonId);

        Assert.Equal(
            dto.ApplicationID,
            result.ApplicationId);

        Assert.Equal(
            dto.ApplicationStatus.ToString(),
            result.ApplicationStatus);

        Assert.Equal(
            dto.StatusText,
            result.StatusText);

        Assert.Equal(
            dto.PaidFees,
            result.PaidFees);

        Assert.Equal(
            dto.ApplicationTypeName,
            result.ApplicationTypeName);

        Assert.Equal(
            dto.ApplicantFullName,
            result.ApplicantFullName);

        Assert.Equal(
            dto.ApplicationDate,
            result.ApplicationDate);

        Assert.Equal(
            dto.LastStatusDate,
            result.LastStatusDate);

        Assert.Equal(
            dto.CreatedByUserName,
            result.CreatedByUserName);

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Verify(
                x =>
                    x.GetApplicationBasicInfoAsync(30),
                Times.Once);
    }

    [Fact]
    public async Task GetApplicationBasicInfo_WhenNotFound_ReturnsNotFound()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetApplicationBasicInfoAsync(30))
            .ReturnsAsync(
                Result<ApplicationBasicInfoDto>
                    .FromNotFound(
                        "Application information not found."));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/30/application-basic-info");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Application information not found.");
    }

    // =========================================================
    // GET BY APPLICATION ID
    // =========================================================

    [Fact]
    public async Task GetByApplicationId_WhenSuccessful_ReturnsMappedApplications()
    {
        var dto =
            CreateApplicationDto(40);

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetLocalDrivingLicenseApplicationsByApplicationIdAsync(40))
            .ReturnsAsync(
                Result<List<LocalDrivingLicenseApplicationListDto>>
                    .Success(
                        new List<LocalDrivingLicenseApplicationListDto>
                        {
                            dto
                        }));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/application/40");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    List<LocalDrivingLicenseApplicationResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        AssertApplicationResponse(
            dto,
            result[0]);

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Verify(
                x =>
                    x.GetLocalDrivingLicenseApplicationsByApplicationIdAsync(
                        40),
                Times.Once);
    }

    [Fact]
    public async Task GetByApplicationId_WhenFailure_ReturnsMappedStatus()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetLocalDrivingLicenseApplicationsByApplicationIdAsync(40))
            .ReturnsAsync(
                Result<List<LocalDrivingLicenseApplicationListDto>>
                    .FromConflict(
                        "Application conflict."));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/application/40");

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Application conflict.");
    }

    // =========================================================
    // GET BY LICENSE CLASS
    // =========================================================

    [Fact]
    public async Task GetByLicenseClassId_WhenSuccessful_ReturnsMappedApplications()
    {
        var dto =
            CreateApplicationDto(50);

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(3))
            .ReturnsAsync(
                Result<List<LocalDrivingLicenseApplicationListDto>>
                    .Success(
                        new List<LocalDrivingLicenseApplicationListDto>
                        {
                            dto
                        }));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/license-class/3");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    List<LocalDrivingLicenseApplicationResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        AssertApplicationResponse(
            dto,
            result[0]);
    }

    [Fact]
    public async Task GetByLicenseClassId_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(0))
            .ReturnsAsync(
                Result<List<LocalDrivingLicenseApplicationListDto>>
                    .FromValidationFailure(
                        "Invalid license class ID."));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/license-class/0");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid license class ID.");
    }

    // =========================================================
    // GET BY PERSON
    // =========================================================

    [Fact]
    public async Task GetByApplicantPersonId_WhenSuccessful_ReturnsMappedApplications()
    {
        var dto =
            CreateApplicationDto(60);

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(
                    15))
            .ReturnsAsync(
                Result<List<LocalDrivingLicenseApplicationListDto>>
                    .Success(
                        new List<LocalDrivingLicenseApplicationListDto>
                        {
                            dto
                        }));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/person/15");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    List<LocalDrivingLicenseApplicationResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        AssertApplicationResponse(
            dto,
            result[0]);
    }

    [Fact]
    public async Task GetByApplicantPersonId_WhenNotFound_ReturnsNotFound()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(
                    15))
            .ReturnsAsync(
                Result<List<LocalDrivingLicenseApplicationListDto>>
                    .FromNotFound(
                        "Applications not found."));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/person/15");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Applications not found.");
    }

    // =========================================================
    // GET CREATE INFO
    // =========================================================

    [Fact]
    public async Task GetCreateInfo_WhenSuccessful_ReturnsApplicationFees()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetNewLocalDrivingLicenseApplicationFeesAsync())
            .ReturnsAsync(
                Result<decimal>.Success(75m));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/create-info");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    CreateLocalDrivingLicenseApplicationInfoResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            75m,
            result.ApplicationFees);

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Verify(
                x =>
                    x.GetNewLocalDrivingLicenseApplicationFeesAsync(),
                Times.Once);
    }

    [Fact]
    public async Task GetCreateInfo_WhenFailure_ReturnsInternalServerError()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetNewLocalDrivingLicenseApplicationFeesAsync())
            .ReturnsAsync(
                Result<decimal>
                    .FromFailure(
                        "Application type lookup failed."));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/create-info");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Application type lookup failed.");
    }

    // =========================================================
    // GET APPLICATION ID
    // =========================================================

    [Fact]
    public async Task GetApplicationId_WhenSuccessful_ReturnsApplicationId()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetApplicationIdByLocalIdAsync(70))
            .ReturnsAsync(
                Result<int>.Success(700));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/70/application-id");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<ApplicationIdResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            700,
            result.ApplicationId);
    }

    [Fact]
    public async Task GetApplicationId_WhenNotFound_ReturnsNotFound()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.GetApplicationIdByLocalIdAsync(70))
            .ReturnsAsync(
                Result<int>
                    .FromNotFound(
                        "Main application not found for this local application."));

        var response =
            await GetAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/70/application-id");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Main application not found for this local application.");
    }

    // =========================================================
    // CREATE
    // =========================================================

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedWithNewId()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.CreateLocalDrivingLicenseApplicationAsync(
                    15,
                    3))
            .ReturnsAsync(
                Result<int>.Success(100));

        var request =
            new CreateLocalDrivingLicenseApplicationRequest
            {
                ApplicantPersonId = 15,
                LicenseClassId = 3
            };

        var response =
            await PostAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    CreateLocalDrivingLicenseApplicationResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            100,
            result.LocalDrivingLicenseApplicationId);

        Assert.NotNull(
            response.Headers.Location);

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Verify(
                x =>
                    x.CreateLocalDrivingLicenseApplicationAsync(
                        15,
                        3),
                Times.Once);
    }

    [Fact]
    public async Task Create_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.CreateLocalDrivingLicenseApplicationAsync(
                    15,
                    3))
            .ReturnsAsync(
                Result<int>
                    .FromValidationFailure(
                        "Invalid local driving license application."));

        var request =
            new CreateLocalDrivingLicenseApplicationRequest
            {
                ApplicantPersonId = 15,
                LicenseClassId = 3
            };

        var response =
            await PostAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid local driving license application.");
    }

    [Fact]
    public async Task Create_WhenConflict_ReturnsConflict()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.CreateLocalDrivingLicenseApplicationAsync(
                    15,
                    3))
            .ReturnsAsync(
                Result<int>
                    .FromConflict(
                        "Duplicate application."));

        var request =
            new CreateLocalDrivingLicenseApplicationRequest
            {
                ApplicantPersonId = 15,
                LicenseClassId = 3
            };

        var response =
            await PostAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications",
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Duplicate application.");
    }

    // =========================================================
    // UPDATE
    // =========================================================

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.UpdateLocalDrivingLicenseApplicationAsync(
                    80,
                    It.Is<UpdateLocalDrivingLicenseApplicationDto>(
                        dto =>
                            dto.LicenseClassID == 4)))
            .ReturnsAsync(
                Result.Success());

        var request =
            new UpdateLocalDrivingLicenseApplicationRequest
            {
                LicenseClassId = 4
            };

        var response =
            await PutAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/80",
                request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Verify(
                x =>
                    x.UpdateLocalDrivingLicenseApplicationAsync(
                        80,
                        It.Is<UpdateLocalDrivingLicenseApplicationDto>(
                            dto =>
                                dto.LicenseClassID == 4)),
                Times.Once);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.UpdateLocalDrivingLicenseApplicationAsync(
                    80,
                    It.IsAny<UpdateLocalDrivingLicenseApplicationDto>()))
            .ReturnsAsync(
                Result.NotFound(
                    "Local driving license application not found."));

        var request =
            new UpdateLocalDrivingLicenseApplicationRequest
            {
                LicenseClassId = 4
            };

        var response =
            await PutAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/80",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Local driving license application not found.");
    }

    // =========================================================
    // DELETE
    // =========================================================

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.DeleteLocalDrivingLicenseApplicationAsync(90))
            .ReturnsAsync(
                Result.Success());

        var response =
            await DeleteAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/90");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Verify(
                x =>
                    x.DeleteLocalDrivingLicenseApplicationAsync(90),
                Times.Once);
    }

    [Fact]
    public async Task Delete_WhenConflict_ReturnsConflict()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.DeleteLocalDrivingLicenseApplicationAsync(90))
            .ReturnsAsync(
                Result.Conflict(
                    "Application cannot be deleted."));

        var response =
            await DeleteAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/90");

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Application cannot be deleted.");
    }

    // =========================================================
    // CANCEL
    // =========================================================

    [Fact]
    public async Task Cancel_WhenSuccessful_ReturnsNoContent()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.CancelLocalDrivingLicenseApplicationAsync(100))
            .ReturnsAsync(
                Result.Success());

        var response =
            await PostAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/100/cancel");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Verify(
                x =>
                    x.CancelLocalDrivingLicenseApplicationAsync(100),
                Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenNotFound_ReturnsNotFound()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.CancelLocalDrivingLicenseApplicationAsync(100))
            .ReturnsAsync(
                Result.NotFound(
                    "Local driving license application not found."));

        var response =
            await PostAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/100/cancel");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Local driving license application not found.");
    }

    [Fact]
    public async Task Cancel_WhenForbidden_ReturnsForbidden()
    {
        _factory
            .LocalDrivingLicenseApplicationServiceMock
            .Setup(x =>
                x.CancelLocalDrivingLicenseApplicationAsync(100))
            .ReturnsAsync(
                Result.Forbidden(
                    "Access denied."));

        var response =
            await PostAuthenticatedAsync(
                "/api/LocalDrivingLicenseApplications/100/cancel");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        // The controller uses Forbid(), therefore
        // there is intentionally no application error body.
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private async Task<HttpResponseMessage> GetAuthenticatedAsync(
        string url)
    {
        var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                url);

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostAuthenticatedAsync(
        string url,
        object? body = null)
    {
        var request =
            CreateAuthenticatedRequest(
                HttpMethod.Post,
                url);

        if (body is not null)
        {
            request.Content =
                JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PutAuthenticatedAsync(
        string url,
        object body)
    {
        var request =
            CreateAuthenticatedRequest(
                HttpMethod.Put,
                url);

        request.Content =
            JsonContent.Create(body);

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> DeleteAuthenticatedAsync(
        string url)
    {
        var request =
            CreateAuthenticatedRequest(
                HttpMethod.Delete,
                url);

        return await _client.SendAsync(request);
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string url,
        string role = "Staff")
    {
        var request =
            new HttpRequestMessage(
                method,
                url);

        request.Headers.Add(
            "X-Test-User-Id",
            "1");

        request.Headers.Add(
            "X-Test-Username",
            "testuser");

        request.Headers.Add(
            "X-Test-FullName",
            "Test User");

        request.Headers.Add(
            "X-Test-Role",
            role);

        return request;
    }

    // =========================================================
    // DTO FACTORIES
    // =========================================================

    private static LocalDrivingLicenseApplicationListDto
        CreateApplicationDto(
            int id)
    {
        return new LocalDrivingLicenseApplicationListDto
        {
            LocalDrivingLicenseApplicationID = id,

            LicenseClassID = 3,

            LicenseClassName =
                "Private",

            NationalNo =
                "123456789",

            FullName =
                "Test Person",

            ApplicationDate =
                new DateTime(2026, 1, 10),

            PassedTest =
                3,

            ApplicationStatus =
                AppStatus.New,

            ApplicationFees =
                50m,

            LicenseClassFees =
                25m,

            HasLicense =
                false,

            ApplicantPersonID =
                200
        };
    }

    private static ApplicationBasicInfoDto
        CreateApplicationBasicInfoDto()
    {
        return new ApplicationBasicInfoDto
        {
            ApplicantPersonID =
                200,

            ApplicationID =
                300,

            ApplicationStatus =
                AppStatus.New,

            PaidFees =
                50m,

            ApplicationTypeName =
                "New Local Driving License",

            ApplicantFullName =
                "Test Person",

            ApplicationDate =
                new DateTime(2026, 1, 10),

            LastStatusDate =
                new DateTime(2026, 1, 10),

            CreatedByUserName =
                "admin"
        };
    }

    // =========================================================
    // ASSERTIONS
    // =========================================================

    private static void AssertApplicationResponse(
        LocalDrivingLicenseApplicationListDto dto,
        LocalDrivingLicenseApplicationResponse response)
    {
        Assert.Equal(
            dto.LocalDrivingLicenseApplicationID,
            response.LocalDrivingLicenseApplicationId);

        Assert.Equal(
            dto.LicenseClassID,
            response.LicenseClassId);

        Assert.Equal(
            dto.LicenseClassName,
            response.LicenseClassName);

        Assert.Equal(
            dto.NationalNo,
            response.NationalNo);

        Assert.Equal(
            dto.FullName,
            response.FullName);

        Assert.Equal(
            dto.ApplicationDate,
            response.ApplicationDate);

        Assert.Equal(
            dto.PassedTest,
            response.PassedTest);

        Assert.Equal(
            dto.ApplicationStatus.ToString(),
            response.ApplicationStatus);

        Assert.Equal(
            dto.StatusText,
            response.StatusText);

        Assert.Equal(
            dto.ApplicationFees,
            response.ApplicationFees);

        Assert.Equal(
            dto.LicenseClassFees,
            response.LicenseClassFees);

        Assert.Equal(
            dto.HasLicense,
            response.HasLicense);

        Assert.Equal(
            dto.ApplicantPersonID,
            response.ApplicantPersonId);
    }

    private static async Task AssertErrorAsync(
        HttpResponseMessage response,
        string expectedError)
    {
        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            expectedError,
            body.Error);
    }

    private sealed record ErrorResponse(
        string Error);

    private sealed record ApplicationIdResponse(
        int ApplicationId);
}