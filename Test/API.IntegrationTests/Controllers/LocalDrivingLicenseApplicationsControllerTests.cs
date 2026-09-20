using API.IntegrationTests.Infrastructure;
using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Domain.Enums;
using DVLD.Contracts.Application;
using DVLD.Contracts.Common;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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
        _factory.LocalDrivingLicenseApplicationServiceMock.Reset();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_Returns401()
    {
        var response = await _client.GetAsync(
            "/api/LocalDrivingLicenseApplications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedApplications()
    {
        var dto = CreateApplicationDto(10);
        var paged = new PagedResult<LocalDrivingLicenseApplicationListDto>
        {
            Items = [dto],
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetAllLocalDrivingLicenseApplicationsAsync(
                It.IsAny<PaginationRequest>()))
            .ReturnsAsync(Result<PagedResult<LocalDrivingLicenseApplicationListDto>>
                .Success(paged));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<LocalDrivingLicenseApplicationResponse>>();

        Assert.NotNull(result);
        var item = Assert.Single(result.Items);

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);

        AssertApplicationResponse(dto, item);

        _factory.LocalDrivingLicenseApplicationServiceMock.Verify(
            x => x.GetAllLocalDrivingLicenseApplicationsAsync(
                It.Is<PaginationRequest>(p =>
                    p.PageNumber == 1 && p.PageSize == 10)),
            Times.Once);
    }

    [Theory]
    [InlineData(400, "Validation error", "Invalid application data.")]
    public async Task GetAll_WhenResultFails_ReturnsProblemDetails(
        int statusCode,
        string title,
        string detail)
    {
        var result = Result<PagedResult<LocalDrivingLicenseApplicationListDto>>
            .FromValidationFailure(detail);

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetAllLocalDrivingLicenseApplicationsAsync(
                It.IsAny<PaginationRequest>()))
            .ReturnsAsync(result);

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications");

        await AssertProblemDetailsAsync(
            response,
            (HttpStatusCode)statusCode,
            title,
            detail);
    }

    [Fact]
    public async Task GetAll_WhenUnexpectedFailure_Returns500ProblemDetails()
    {
        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetAllLocalDrivingLicenseApplicationsAsync(
                It.IsAny<PaginationRequest>()))
            .ReturnsAsync(Result<PagedResult<LocalDrivingLicenseApplicationListDto>>
                .FromFailure("Database failure."));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedApplication()
    {
        var dto = CreateApplicationDto(20);

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetLocalDrivingLicenseApplicationByIdAsync(20))
            .ReturnsAsync(Result<LocalDrivingLicenseApplicationListDto>.Success(dto));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<LocalDrivingLicenseApplicationResponse>();

        Assert.NotNull(result);
        AssertApplicationResponse(dto, result);

        _factory.LocalDrivingLicenseApplicationServiceMock.Verify(
            x => x.GetLocalDrivingLicenseApplicationByIdAsync(20),
            Times.Once);
    }

    [Theory]
    [InlineData(20, 404, "Resource not found",
        "Local driving license application not found.")]
    [InlineData(0, 400, "Validation error",
        "Invalid local application ID.")]
    public async Task GetById_WhenResultFails_ReturnsProblemDetails(
        int id,
        int statusCode,
        string title,
        string detail)
    {
        var result = statusCode == 404
            ? Result<LocalDrivingLicenseApplicationListDto>.FromNotFound(detail)
            : Result<LocalDrivingLicenseApplicationListDto>.FromValidationFailure(detail);

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetLocalDrivingLicenseApplicationByIdAsync(id))
            .ReturnsAsync(result);

        var response = await GetAuthenticatedAsync(
            $"/api/LocalDrivingLicenseApplications/{id}");

        await AssertProblemDetailsAsync(
            response,
            (HttpStatusCode)statusCode,
            title,
            detail);
    }

    [Fact]
    public async Task GetApplicationBasicInfo_WhenSuccessful_ReturnsMappedInfo()
    {
        var dto = CreateApplicationBasicInfoDto();

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetApplicationBasicInfoAsync(30))
            .ReturnsAsync(Result<ApplicationBasicInfoDto>.Success(dto));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/30/application-basic-info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<ApplicationBasicInfoResponse>();

        Assert.NotNull(result);
        Assert.Equal(dto.ApplicantPersonID, result.ApplicantPersonId);
        Assert.Equal(dto.ApplicationID, result.ApplicationId);
        Assert.Equal(dto.ApplicationStatus.ToString(), result.ApplicationStatus);
        Assert.Equal(dto.StatusText, result.StatusText);
        Assert.Equal(dto.PaidFees, result.PaidFees);
        Assert.Equal(dto.ApplicationTypeName, result.ApplicationTypeName);
        Assert.Equal(dto.ApplicantFullName, result.ApplicantFullName);
        Assert.Equal(dto.ApplicationDate, result.ApplicationDate);
        Assert.Equal(dto.LastStatusDate, result.LastStatusDate);
        Assert.Equal(dto.CreatedByUserName, result.CreatedByUserName);

        _factory.LocalDrivingLicenseApplicationServiceMock.Verify(
            x => x.GetApplicationBasicInfoAsync(30), Times.Once);
    }

    [Fact]
    public async Task GetApplicationBasicInfo_WhenNotFound_Returns404ProblemDetails()
    {
        const string detail = "Application information not found.";

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetApplicationBasicInfoAsync(30))
            .ReturnsAsync(Result<ApplicationBasicInfoDto>.FromNotFound(detail));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/30/application-basic-info");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found",
            detail);
    }

    [Fact]
    public async Task GetByApplicationId_WhenSuccessful_ReturnsMappedApplications()
    {
        var dto = CreateApplicationDto(40);

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetLocalDrivingLicenseApplicationsByApplicationIdAsync(40))
            .ReturnsAsync(Result<List<LocalDrivingLicenseApplicationListDto>>
                .Success([dto]));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/application/40");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<List<LocalDrivingLicenseApplicationResponse>>();

        Assert.NotNull(result);
        AssertApplicationResponse(dto, Assert.Single(result));

        _factory.LocalDrivingLicenseApplicationServiceMock.Verify(
            x => x.GetLocalDrivingLicenseApplicationsByApplicationIdAsync(40),
            Times.Once);
    }

    [Fact]
    public async Task GetByApplicationId_WhenFailure_Returns409ProblemDetails()
    {
        const string detail = "Application conflict.";

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetLocalDrivingLicenseApplicationsByApplicationIdAsync(40))
            .ReturnsAsync(Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromConflict(detail));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/application/40");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict",
            detail);
    }

    [Fact]
    public async Task GetByLicenseClassId_WhenSuccessful_ReturnsMappedApplications()
    {
        var dto = CreateApplicationDto(50);

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(3))
            .ReturnsAsync(Result<List<LocalDrivingLicenseApplicationListDto>>
                .Success([dto]));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/license-class/3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<List<LocalDrivingLicenseApplicationResponse>>();

        Assert.NotNull(result);
        AssertApplicationResponse(dto, Assert.Single(result));
    }

    [Fact]
    public async Task GetByLicenseClassId_WhenValidationFailure_Returns400ProblemDetails()
    {
        const string detail = "Invalid license class ID.";

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(0))
            .ReturnsAsync(Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromValidationFailure(detail));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/license-class/0");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "Validation error",
            detail);
    }

    [Fact]
    public async Task GetByApplicantPersonId_WhenSuccessful_ReturnsMappedApplications()
    {
        var dto = CreateApplicationDto(60);

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(15))
            .ReturnsAsync(Result<List<LocalDrivingLicenseApplicationListDto>>
                .Success([dto]));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/person/15");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<List<LocalDrivingLicenseApplicationResponse>>();

        Assert.NotNull(result);
        AssertApplicationResponse(dto, Assert.Single(result));
    }

    [Fact]
    public async Task GetByApplicantPersonId_WhenNotFound_Returns404ProblemDetails()
    {
        const string detail = "Applications not found.";

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(15))
            .ReturnsAsync(Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromNotFound(detail));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/person/15");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found",
            detail);
    }

    [Fact]
    public async Task GetCreateInfo_WhenSuccessful_ReturnsApplicationFees()
    {
        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetNewLocalDrivingLicenseApplicationFeesAsync())
            .ReturnsAsync(Result<decimal>.Success(75m));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/create-info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<CreateLocalDrivingLicenseApplicationInfoResponse>();

        Assert.NotNull(result);
        Assert.Equal(75m, result.ApplicationFees);

        _factory.LocalDrivingLicenseApplicationServiceMock.Verify(
            x => x.GetNewLocalDrivingLicenseApplicationFeesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCreateInfo_WhenFailure_Returns500ProblemDetails()
    {
        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetNewLocalDrivingLicenseApplicationFeesAsync())
            .ReturnsAsync(Result<decimal>.FromFailure(
                "Application type lookup failed."));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/create-info");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetApplicationId_WhenSuccessful_ReturnsApplicationId()
    {
        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetApplicationIdByLocalIdAsync(70))
            .ReturnsAsync(Result<int>.Success(700));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/70/application-id");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<ApplicationIdResponse>();

        Assert.NotNull(result);
        Assert.Equal(700, result.ApplicationId);
    }

    [Fact]
    public async Task GetApplicationId_WhenNotFound_Returns404ProblemDetails()
    {
        const string detail =
            "Main application not found for this local application.";

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.GetApplicationIdByLocalIdAsync(70))
            .ReturnsAsync(Result<int>.FromNotFound(detail));

        var response = await GetAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/70/application-id");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found",
            detail);
    }

    [Fact]
    public async Task Create_WhenSuccessful_Returns201WithNewId()
    {
        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.CreateLocalDrivingLicenseApplicationAsync(15, 3))
            .ReturnsAsync(Result<int>.Success(100));

        var response = await PostAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications",
            new CreateLocalDrivingLicenseApplicationRequest
            {
                ApplicantPersonId = 15,
                LicenseClassId = 3
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<CreateLocalDrivingLicenseApplicationResponse>();

        Assert.NotNull(result);
        Assert.Equal(100, result.LocalDrivingLicenseApplicationId);
        Assert.NotNull(response.Headers.Location);

        _factory.LocalDrivingLicenseApplicationServiceMock.Verify(
            x => x.CreateLocalDrivingLicenseApplicationAsync(15, 3),
            Times.Once);
    }

    [Theory]
    [InlineData(400, "Validation error",
        "Invalid local driving license application.")]
    [InlineData(409, "Conflict", "Duplicate application.")]
    public async Task Create_WhenResultFails_ReturnsProblemDetails(
        int statusCode,
        string title,
        string detail)
    {
        var result = statusCode == 400
            ? Result<int>.FromValidationFailure(detail)
            : Result<int>.FromConflict(detail);

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.CreateLocalDrivingLicenseApplicationAsync(15, 3))
            .ReturnsAsync(result);

        var response = await PostAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications",
            new CreateLocalDrivingLicenseApplicationRequest
            {
                ApplicantPersonId = 15,
                LicenseClassId = 3
            });

        await AssertProblemDetailsAsync(
            response,
            (HttpStatusCode)statusCode,
            title,
            detail);
    }

    [Fact]
    public async Task Update_WhenSuccessful_Returns204()
    {
        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.UpdateLocalDrivingLicenseApplicationAsync(
                80,
                It.Is<UpdateLocalDrivingLicenseApplicationDto>(
                    dto => dto.LicenseClassID == 4)))
            .ReturnsAsync(Result.Success());

        var response = await PutAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/80",
            new UpdateLocalDrivingLicenseApplicationRequest
            {
                LicenseClassId = 4
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.LocalDrivingLicenseApplicationServiceMock.Verify(
            x => x.UpdateLocalDrivingLicenseApplicationAsync(
                80,
                It.Is<UpdateLocalDrivingLicenseApplicationDto>(
                    dto => dto.LicenseClassID == 4)),
            Times.Once);
    }

    [Fact]
    public async Task Update_WhenNotFound_Returns404ProblemDetails()
    {
        const string detail =
            "Local driving license application not found.";

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.UpdateLocalDrivingLicenseApplicationAsync(
                80,
                It.IsAny<UpdateLocalDrivingLicenseApplicationDto>()))
            .ReturnsAsync(Result.NotFound(detail));

        var response = await PutAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/80",
            new UpdateLocalDrivingLicenseApplicationRequest
            {
                LicenseClassId = 4
            });

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found",
            detail);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_Returns204()
    {
        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.DeleteLocalDrivingLicenseApplicationAsync(90))
            .ReturnsAsync(Result.Success());

        var response = await DeleteAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/90");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.LocalDrivingLicenseApplicationServiceMock.Verify(
            x => x.DeleteLocalDrivingLicenseApplicationAsync(90),
            Times.Once);
    }

    [Fact]
    public async Task Delete_WhenConflict_Returns409ProblemDetails()
    {
        const string detail = "Application cannot be deleted.";

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.DeleteLocalDrivingLicenseApplicationAsync(90))
            .ReturnsAsync(Result.Conflict(detail));

        var response = await DeleteAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/90");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict",
            detail);
    }

    [Fact]
    public async Task Cancel_WhenSuccessful_Returns204()
    {
        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.CancelLocalDrivingLicenseApplicationAsync(100))
            .ReturnsAsync(Result.Success());

        var response = await PostAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/100/cancel");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.LocalDrivingLicenseApplicationServiceMock.Verify(
            x => x.CancelLocalDrivingLicenseApplicationAsync(100),
            Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenNotFound_Returns404ProblemDetails()
    {
        const string detail =
            "Local driving license application not found.";

        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.CancelLocalDrivingLicenseApplicationAsync(100))
            .ReturnsAsync(Result.NotFound(detail));

        var response = await PostAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/100/cancel");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found",
            detail);
    }

    [Fact]
    public async Task Cancel_WhenForbidden_Returns403()
    {
        _factory.LocalDrivingLicenseApplicationServiceMock
            .Setup(x => x.CancelLocalDrivingLicenseApplicationAsync(100))
            .ReturnsAsync(Result.Forbidden("Access denied."));

        var response = await PostAuthenticatedAsync(
            "/api/LocalDrivingLicenseApplications/100/cancel");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<HttpResponseMessage> GetAuthenticatedAsync(string url)
    {
        return await _client.SendAsync(
            CreateAuthenticatedRequest(HttpMethod.Get, url));
    }

    private async Task<HttpResponseMessage> PostAuthenticatedAsync(
        string url,
        object? body = null)
    {
        var request = CreateAuthenticatedRequest(HttpMethod.Post, url);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PutAuthenticatedAsync(
        string url,
        object body)
    {
        var request = CreateAuthenticatedRequest(HttpMethod.Put, url);
        request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> DeleteAuthenticatedAsync(string url)
    {
        return await _client.SendAsync(
            CreateAuthenticatedRequest(HttpMethod.Delete, url));
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string url,
        string role = "Staff")
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Test-User-Id", "1");
        request.Headers.Add("X-Test-Username", "testuser");
        request.Headers.Add("X-Test-FullName", "Test User");
        request.Headers.Add("X-Test-Role", role);
        return request;
    }

    private static void AssertApplicationResponse(
        LocalDrivingLicenseApplicationListDto dto,
        LocalDrivingLicenseApplicationResponse response)
    {
        Assert.Equal(dto.LocalDrivingLicenseApplicationID,
            response.LocalDrivingLicenseApplicationId);
        Assert.Equal(dto.LicenseClassID, response.LicenseClassId);
        Assert.Equal(dto.LicenseClassName, response.LicenseClassName);
        Assert.Equal(dto.NationalNo, response.NationalNo);
        Assert.Equal(dto.FullName, response.FullName);
        Assert.Equal(dto.ApplicationDate, response.ApplicationDate);
        Assert.Equal(dto.PassedTest, response.PassedTest);
        Assert.Equal(dto.ApplicationStatus.ToString(), response.ApplicationStatus);
        Assert.Equal(dto.StatusText, response.StatusText);
        Assert.Equal(dto.ApplicationFees, response.ApplicationFees);
        Assert.Equal(dto.LicenseClassFees, response.LicenseClassFees);
        Assert.Equal(dto.HasLicense, response.HasLicense);
        Assert.Equal(dto.ApplicantPersonID, response.ApplicantPersonId);
    }

    private static LocalDrivingLicenseApplicationListDto CreateApplicationDto(int id) => new()
    {
        LocalDrivingLicenseApplicationID = id,
        LicenseClassID = 3,
        LicenseClassName = "Private",
        NationalNo = "123456789",
        FullName = "Test Person",
        ApplicationDate = new DateTime(2026, 1, 10),
        PassedTest = 3,
        ApplicationStatus = AppStatus.New,
        ApplicationFees = 50m,
        LicenseClassFees = 25m,
        HasLicense = false,
        ApplicantPersonID = 200
    };

    private static ApplicationBasicInfoDto CreateApplicationBasicInfoDto() => new()
    {
        ApplicantPersonID = 200,
        ApplicationID = 300,
        ApplicationStatus = AppStatus.New,
        PaidFees = 50m,
        ApplicationTypeName = "New Local Driving License",
        ApplicantFullName = "Test Person",
        ApplicationDate = new DateTime(2026, 1, 10),
        LastStatusDate = new DateTime(2026, 1, 10),
        CreatedByUserName = "admin"
    };

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedTitle,
        string expectedDetail)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var body = document.RootElement;

        Assert.Equal((int)expectedStatus,
            body.GetProperty("status").GetInt32());
        Assert.Equal(expectedTitle,
            body.GetProperty("title").GetString());
        Assert.Equal(expectedDetail,
            body.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            body.GetProperty("instance").GetString()));
        Assert.True(body.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    private sealed record ApplicationIdResponse(int ApplicationId);
}
