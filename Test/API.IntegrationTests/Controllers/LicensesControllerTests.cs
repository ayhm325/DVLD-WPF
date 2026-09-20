using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Domain.Enums;
using DVLD.Contracts.License;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.IntegrationTests.Controllers;

public sealed class LicensesControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public LicensesControllerTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.LicenseServiceMock.Reset();
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/Licenses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateLicenseDto();
        _factory.LicenseServiceMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<LicenseDto>>.Success([dto]));

        var response = await GetAsync("/api/Licenses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<LicenseResponse>>();
        Assert.NotNull(result);
        var item = Assert.Single(result);

        AssertLicense(item, dto);
    }

    [Theory]
    [InlineData(400, "Validation error", "Invalid license data.")]
    [InlineData(404, "Resource not found", "Licenses not found.")]
    [InlineData(409, "Conflict", "License conflict.")]
    [InlineData(403, "Forbidden", "Access denied.")]
    public async Task GetAll_WhenResultFails_ReturnsProblemDetails(
        int statusCode, string title, string detail)
    {
        var result = statusCode switch
        {
            400 => Result<List<LicenseDto>>.FromValidationFailure(detail),
            404 => Result<List<LicenseDto>>.FromNotFound(detail),
            409 => Result<List<LicenseDto>>.FromConflict(detail),
            403 => Result<List<LicenseDto>>.FromForbidden(detail),
            _ => throw new ArgumentOutOfRangeException(nameof(statusCode))
        };

        _factory.LicenseServiceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(result);

        var response = await GetAsync("/api/Licenses");

        await AssertProblemDetailsAsync(response, (HttpStatusCode)statusCode, title, detail);
    }

    [Fact]
    public async Task GetAll_WhenFailure_Returns500ProblemDetails()
    {
        _factory.LicenseServiceMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<LicenseDto>>.FromFailure("Database failure."));

        var response = await GetAsync("/api/Licenses");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetAll_WhenResultValueIsNull_Returns500ProblemDetails()
    {
        _factory.LicenseServiceMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<LicenseDto>>.Success(null!));

        var response = await GetAsync("/api/Licenses");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedLicense()
    {
        var dto = CreateLicenseDto(10);
        _factory.LicenseServiceMock.Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(Result<LicenseDto>.Success(dto));

        var response = await GetAsync("/api/Licenses/10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<LicenseResponse>();
        Assert.NotNull(result);

        AssertLicense(result, dto);
    }

    [Theory]
    [InlineData(10, 404, "Resource not found", "License not found.")]
    [InlineData(0, 400, "Validation error", "Invalid license ID.")]
    public async Task GetById_WhenResultFails_ReturnsProblemDetails(
        int id, int statusCode, string title, string detail)
    {
        var result = statusCode == 404
            ? Result<LicenseDto>.FromNotFound(detail)
            : Result<LicenseDto>.FromValidationFailure(detail);

        _factory.LicenseServiceMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(result);

        var response = await GetAsync($"/api/Licenses/{id}");

        await AssertProblemDetailsAsync(response, (HttpStatusCode)statusCode, title, detail);
    }

    [Fact]
    public async Task GetById_WhenResultValueIsNull_Returns500ProblemDetails()
    {
        _factory.LicenseServiceMock.Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(Result<LicenseDto>.Success(null!));

        var response = await GetAsync("/api/Licenses/10");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetByDriverId_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateLicenseDto(20);
        _factory.LicenseServiceMock.Setup(x => x.GetByDriverIdAsync(5))
            .ReturnsAsync(Result<List<LicenseDto>>.Success([dto]));

        var response = await GetAsync("/api/Licenses/driver/5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<LicenseResponse>>();
        Assert.NotNull(result);
        var item = Assert.Single(result);

        Assert.Equal(dto.LicenseID, item.LicenseId);
        Assert.Equal(dto.DriverID, item.DriverId);
    }

    [Fact]
    public async Task GetByDriverId_WhenServiceFails_ReturnsProblemDetails()
    {
        const string detail = "Driver licenses not found.";

        _factory.LicenseServiceMock.Setup(x => x.GetByDriverIdAsync(5))
            .ReturnsAsync(Result<List<LicenseDto>>.FromNotFound(detail));

        var response = await GetAsync("/api/Licenses/driver/5");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found",
            detail);
    }

    [Fact]
    public async Task GetByDriverId_WhenResultValueIsNull_Returns500ProblemDetails()
    {
        _factory.LicenseServiceMock.Setup(x => x.GetByDriverIdAsync(5))
            .ReturnsAsync(Result<List<LicenseDto>>.Success(null!));

        var response = await GetAsync("/api/Licenses/driver/5");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetByApplicationId_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateLicenseDto(30);
        _factory.LicenseServiceMock.Setup(x => x.GetByApplicationIdAsync(7))
            .ReturnsAsync(Result<List<LicenseDto>>.Success([dto]));

        var response = await GetAsync("/api/Licenses/application/7");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<LicenseResponse>>();
        Assert.NotNull(result);
        var item = Assert.Single(result);

        Assert.Equal(dto.LicenseID, item.LicenseId);
        Assert.Equal(dto.ApplicationID, item.ApplicationId);
    }

    [Fact]
    public async Task GetByApplicationId_WhenServiceFails_ReturnsProblemDetails()
    {
        const string detail = "Invalid application ID.";

        _factory.LicenseServiceMock.Setup(x => x.GetByApplicationIdAsync(7))
            .ReturnsAsync(Result<List<LicenseDto>>.FromValidationFailure(detail));

        var response = await GetAsync("/api/Licenses/application/7");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "Validation error",
            detail);
    }

    [Fact]
    public async Task GetByApplicationId_WhenResultValueIsNull_Returns500ProblemDetails()
    {
        _factory.LicenseServiceMock.Setup(x => x.GetByApplicationIdAsync(7))
            .ReturnsAsync(Result<List<LicenseDto>>.Success(null!));

        var response = await GetAsync("/api/Licenses/application/7");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetByLicenseClassId_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateLicenseDto(40);
        _factory.LicenseServiceMock.Setup(x => x.GetByLicenseClassIdAsync(2))
            .ReturnsAsync(Result<List<LicenseDto>>.Success([dto]));

        var response = await GetAsync("/api/Licenses/license-class/2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<LicenseResponse>>();
        Assert.NotNull(result);
        var item = Assert.Single(result);

        Assert.Equal(dto.LicenseID, item.LicenseId);
        Assert.Equal(dto.LicenseClassID, item.LicenseClassId);
    }

    [Fact]
    public async Task GetByLicenseClassId_WhenServiceFails_ReturnsProblemDetails()
    {
        const string detail = "License class conflict.";

        _factory.LicenseServiceMock.Setup(x => x.GetByLicenseClassIdAsync(2))
            .ReturnsAsync(Result<List<LicenseDto>>.FromConflict(detail));

        var response = await GetAsync("/api/Licenses/license-class/2");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict",
            detail);
    }

    [Fact]
    public async Task GetByLicenseClassId_WhenResultValueIsNull_Returns500ProblemDetails()
    {
        _factory.LicenseServiceMock.Setup(x => x.GetByLicenseClassIdAsync(2))
            .ReturnsAsync(Result<List<LicenseDto>>.Success(null!));

        var response = await GetAsync("/api/Licenses/license-class/2");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetByPersonId_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateLicenseDto(50);
        _factory.LicenseServiceMock.Setup(x => x.GetLicensesByPersonIdAsync(15))
            .ReturnsAsync(Result<List<LicenseDto>>.Success([dto]));

        var response = await GetAsync("/api/Licenses/person/15");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<LicenseResponse>>();
        Assert.NotNull(result);
        var item = Assert.Single(result);

        Assert.Equal(dto.LicenseID, item.LicenseId);
        Assert.Equal(dto.DriverID, item.DriverId);
    }

    [Fact]
    public async Task GetByPersonId_WhenServiceFails_ReturnsProblemDetails()
    {
        const string detail = "Access denied.";

        _factory.LicenseServiceMock.Setup(x => x.GetLicensesByPersonIdAsync(15))
            .ReturnsAsync(Result<List<LicenseDto>>.FromForbidden(detail));

        var response = await GetAsync("/api/Licenses/person/15");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Forbidden,
            "Forbidden",
            detail);
    }

    [Fact]
    public async Task GetByPersonId_WhenResultValueIsNull_Returns500ProblemDetails()
    {
        _factory.LicenseServiceMock.Setup(x => x.GetLicensesByPersonIdAsync(15))
            .ReturnsAsync(Result<List<LicenseDto>>.Success(null!));

        var response = await GetAsync("/api/Licenses/person/15");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetDetails_WhenSuccessful_ReturnsMappedDetails()
    {
        var dto = CreateDriverLicenseInfoDto();
        _factory.LicenseServiceMock.Setup(x => x.GetDetailsAsync(100))
            .ReturnsAsync(Result<DriverLicenseInfoDto>.Success(dto));

        var response = await GetAsync("/api/Licenses/local-application/100/details");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DriverLicenseInfoResponse>();
        Assert.NotNull(result);

        AssertDetails(result, dto);
    }

    [Fact]
    public async Task GetDetails_WhenNotFound_Returns404ProblemDetails()
    {
        const string detail = "License details not found.";

        _factory.LicenseServiceMock.Setup(x => x.GetDetailsAsync(100))
            .ReturnsAsync(Result<DriverLicenseInfoDto>.FromNotFound(detail));

        var response = await GetAsync("/api/Licenses/local-application/100/details");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found",
            detail);
    }

    [Fact]
    public async Task GetDetails_WhenResultValueIsNull_Returns500ProblemDetails()
    {
        _factory.LicenseServiceMock.Setup(x => x.GetDetailsAsync(100))
            .ReturnsAsync(Result<DriverLicenseInfoDto>.Success(null!));

        var response = await GetAsync("/api/Licenses/local-application/100/details");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetDetailsById_WhenSuccessful_ReturnsMappedDetails()
    {
        var dto = CreateDriverLicenseInfoDto();
        dto.LicenseId = 200;

        _factory.LicenseServiceMock.Setup(x => x.GetLicenseDetailsByIdAsync(200))
            .ReturnsAsync(Result<DriverLicenseInfoDto>.Success(dto));

        var response = await GetAsync("/api/Licenses/200/details");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DriverLicenseInfoResponse>();
        Assert.NotNull(result);

        AssertDetails(result, dto);
    }

    [Theory]
    [InlineData(200, 404, "Resource not found", "License not found.")]
    [InlineData(0, 400, "Validation error", "Invalid license ID.")]
    public async Task GetDetailsById_WhenResultFails_ReturnsProblemDetails(
        int id, int statusCode, string title, string detail)
    {
        var result = statusCode == 404
            ? Result<DriverLicenseInfoDto>.FromNotFound(detail)
            : Result<DriverLicenseInfoDto>.FromValidationFailure(detail);

        _factory.LicenseServiceMock
            .Setup(x => x.GetLicenseDetailsByIdAsync(id))
            .ReturnsAsync(result);

        var response = await GetAsync($"/api/Licenses/{id}/details");

        await AssertProblemDetailsAsync(
            response,
            (HttpStatusCode)statusCode,
            title,
            detail);
    }

    [Fact]
    public async Task GetDetailsById_WhenResultValueIsNull_Returns500ProblemDetails()
    {
        _factory.LicenseServiceMock.Setup(x => x.GetLicenseDetailsByIdAsync(200))
            .ReturnsAsync(Result<DriverLicenseInfoDto>.Success(null!));

        var response = await GetAsync("/api/Licenses/200/details");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    private async Task<HttpResponseMessage> GetAsync(string url)
    {
        using var request = CreateAuthenticatedRequest(url);
        return await _factory.CreateClient().SendAsync(request);
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        string url, string role = "Staff")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Test-User-Id", "1");
        request.Headers.Add("X-Test-Username", "testuser");
        request.Headers.Add("X-Test-FullName", "Test User");
        request.Headers.Add("X-Test-Role", role);
        return request;
    }

    private static void AssertLicense(LicenseResponse actual, LicenseDto expected)
    {
        Assert.Equal(expected.LicenseID, actual.LicenseId);
        Assert.Equal(expected.ApplicationID, actual.ApplicationId);
        Assert.Equal(expected.DriverID, actual.DriverId);
        Assert.Equal(expected.DriverName, actual.DriverName);
        Assert.Equal(expected.LicenseClassID, actual.LicenseClassId);
        Assert.Equal(expected.LicenseClassName, actual.LicenseClassName);
        Assert.Equal(expected.IssueDate, actual.IssueDate);
        Assert.Equal(expected.ExpirationDate, actual.ExpirationDate);
        Assert.Equal(expected.Notes, actual.Notes);
        Assert.Equal(expected.PaidFees, actual.PaidFees);
        Assert.Equal(expected.IsActive, actual.IsActive);
        Assert.Equal(expected.IssueReason, actual.IssueReason);
        Assert.Equal(expected.IssueReasonText, actual.IssueReasonText);
        Assert.Equal(expected.CreatedByUserID, actual.CreatedByUserId);
        Assert.Equal(expected.CreatedByUserName, actual.CreatedByUserName);
    }

    private static void AssertDetails(
        DriverLicenseInfoResponse actual,
        DriverLicenseInfoDto expected)
    {
        Assert.Equal(expected.LicenseId, actual.LicenseId);
        Assert.Equal(expected.LicenseClass, actual.LicenseClass);
        Assert.Equal(expected.IssueDate, actual.IssueDate);
        Assert.Equal(expected.ExpirationDate, actual.ExpirationDate);
        Assert.Equal(expected.IsActive, actual.IsActive);
        Assert.Equal(expected.IsDetained, actual.IsDetained);
        Assert.Equal(expected.IssueReason, actual.IssueReason);
        Assert.Equal(expected.Notes, actual.Notes);
        Assert.Equal(expected.LicenseClassFees, actual.LicenseClassFees);
        Assert.Equal(expected.DriverId, actual.DriverId);
        Assert.Equal(expected.PersonID, actual.PersonId);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.NationalNo, actual.NationalNo);
        Assert.Equal(expected.DateOfBirth, actual.DateOfBirth);
        Assert.Equal(expected.Gender, actual.Gender);
        Assert.Equal(expected.ImagePath, actual.ImagePath);
    }

    private static LicenseDto CreateLicenseDto(int licenseId = 1) => new()
    {
        LicenseID = licenseId,
        ApplicationID = 10,
        DriverID = 20,
        DriverName = "Test Driver",
        LicenseClassID = 3,
        LicenseClassName = "Private",
        IssueDate = new DateTime(2026, 1, 1),
        ExpirationDate = new DateTime(2031, 1, 1),
        Notes = "Test license",
        PaidFees = 50m,
        IsActive = true,
        IssueReason = (byte)IssueReason.FirstTime,
        IssueReasonText = IssueReason.FirstTime.ToString(),
        CreatedByUserID = 30,
        CreatedByUserName = "admin"
    };

    private static DriverLicenseInfoDto CreateDriverLicenseInfoDto() => new()
    {
        LicenseId = 100,
        LicenseClass = "Private",
        IssueDate = new DateTime(2026, 1, 1),
        ExpirationDate = new DateTime(2031, 1, 1),
        IsActive = true,
        IsDetained = false,
        IssueReason = "FirstTime",
        Notes = "Test notes",
        LicenseClassFees = 50m,
        DriverId = 20,
        PersonID = 30,
        FullName = "Test Person",
        NationalNo = "123456789",
        DateOfBirth = new DateTime(1990, 1, 1),
        Gender = "Male",
        ImagePath = "test.jpg"
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

        Assert.Equal((int)expectedStatus, body.GetProperty("status").GetInt32());
        Assert.Equal(expectedTitle, body.GetProperty("title").GetString());
        Assert.Equal(expectedDetail, body.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            body.GetProperty("instance").GetString()));
        Assert.True(body.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}
