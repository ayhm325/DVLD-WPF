using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.InternationalLicenseDTO;
using Application.DTOs.LicenseDTO;
using DVLD.Contracts.InternationalLicense;
using DVLD.Contracts.License;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.IntegrationTests.Controllers;

public sealed class InternationalLicensesControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public InternationalLicensesControllerTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.InternationalServiceMock.Reset();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/InternationalLicenses");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateInternationalDto();
        _factory.InternationalServiceMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<InternationalDto>>.Success([dto]));

        var response = await _client.SendAsync(CreateAuthenticatedRequest("/api/InternationalLicenses"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<InternationalLicenseListResponse>>();
        Assert.NotNull(result);
        AssertLicenseListResponse(dto, Assert.Single(result));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Validation error", "Invalid license data.")]
    [InlineData(HttpStatusCode.NotFound, "Resource not found", "International licenses not found.")]
    [InlineData(HttpStatusCode.Conflict, "Conflict", "International license conflict.")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden", "Access denied.")]
    public async Task GetAll_WhenServiceFails_ReturnsProblemDetails(
        HttpStatusCode status, string title, string detail)
    {
        await using var factory = new ApiWebApplicationFactory();

        var result = status switch
        {
            HttpStatusCode.BadRequest => Result<List<InternationalDto>>.FromValidationFailure(detail),
            HttpStatusCode.NotFound => Result<List<InternationalDto>>.FromNotFound(detail),
            HttpStatusCode.Conflict => Result<List<InternationalDto>>.FromConflict(detail),
            HttpStatusCode.Forbidden => Result<List<InternationalDto>>.FromForbidden(detail),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        factory.InternationalServiceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(result);

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync("/api/InternationalLicenses"),
            status, title, detail);
    }

    [Fact]
    public async Task GetAll_WhenFailure_ReturnsInternalServerError()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.InternationalServiceMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<InternationalDto>>.FromFailure("Database failure."));

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync("/api/InternationalLicenses"),
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedLicense()
    {
        var dto = CreateInternationalDto(internationalLicenseId: 10);
        _factory.InternationalServiceMock.Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(Result<InternationalDto>.Success(dto));

        var response = await _client.SendAsync(CreateAuthenticatedRequest("/api/InternationalLicenses/10"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<InternationalLicenseResponse>();
        Assert.NotNull(result);
        AssertLicenseResponse(dto, result);
    }

    [Theory]
    [InlineData(10, HttpStatusCode.NotFound, "Resource not found", "International license not found.")]
    [InlineData(0, HttpStatusCode.BadRequest, "Validation error", "Invalid international license ID.")]
    public async Task GetById_WhenServiceFails_ReturnsProblemDetails(
        int id, HttpStatusCode status, string title, string detail)
    {
        await using var factory = new ApiWebApplicationFactory();

        var result = status == HttpStatusCode.NotFound
            ? Result<InternationalDto>.FromNotFound(detail)
            : Result<InternationalDto>.FromValidationFailure(detail);

        factory.InternationalServiceMock.Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync(result);

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync($"/api/InternationalLicenses/{id}"),
            status, title, detail);
    }

    [Fact]
    public async Task GetById_WhenResultValueIsNull_ReturnsNotFound()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.InternationalServiceMock.Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(Result<InternationalDto>.Success(null!));

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync("/api/InternationalLicenses/10"),
            HttpStatusCode.NotFound,
            "Resource not found",
            "International license not found.");
    }

    [Fact]
    public async Task GetByDriverId_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateInternationalDto(internationalLicenseId: 20);
        _factory.InternationalServiceMock.Setup(x => x.GetByDriverIdAsync(5))
            .ReturnsAsync(Result<List<InternationalDto>>.Success([dto]));

        var response = await _client.SendAsync(
            CreateAuthenticatedRequest("/api/InternationalLicenses/driver/5"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<InternationalLicenseListResponse>>();
        Assert.NotNull(result);
        AssertLicenseListResponse(dto, Assert.Single(result));
    }

    [Fact]
    public async Task GetByDriverId_WhenServiceFails_ReturnsBadRequest()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.InternationalServiceMock.Setup(x => x.GetByDriverIdAsync(5))
            .ReturnsAsync(Result<List<InternationalDto>>.FromValidationFailure("Invalid driver ID."));

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync("/api/InternationalLicenses/driver/5"),
            HttpStatusCode.BadRequest,
            "Validation error",
            "Invalid driver ID.");
    }

    [Fact]
    public async Task GetByApplicationId_WhenSuccessful_ReturnsMappedLicense()
    {
        var dto = CreateInternationalDto(applicationId: 30);
        _factory.InternationalServiceMock.Setup(x => x.GetByApplicationIdAsync(30))
            .ReturnsAsync(Result<InternationalDto>.Success(dto));

        var response = await _client.SendAsync(
            CreateAuthenticatedRequest("/api/InternationalLicenses/application/30"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<InternationalLicenseResponse>();
        Assert.NotNull(result);
        AssertLicenseResponse(dto, result);
    }

    [Fact]
    public async Task GetByApplicationId_WhenNotFoundFailure_ReturnsNotFound()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.InternationalServiceMock.Setup(x => x.GetByApplicationIdAsync(30))
            .ReturnsAsync(Result<InternationalDto>.FromNotFound("International license not found."));

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync("/api/InternationalLicenses/application/30"),
            HttpStatusCode.NotFound,
            "Resource not found",
            "International license not found.");
    }

    [Fact]
    public async Task GetByApplicationId_WhenResultValueIsNull_ReturnsNotFound()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.InternationalServiceMock.Setup(x => x.GetByApplicationIdAsync(30))
            .ReturnsAsync(Result<InternationalDto>.Success(null!));

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync("/api/InternationalLicenses/application/30"),
            HttpStatusCode.NotFound,
            "Resource not found",
            "International license not found.");
    }

    [Fact]
    public async Task GetByLocalLicenseId_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateInternationalDto(issuedUsingLocalLicenseId: 40);
        _factory.InternationalServiceMock.Setup(x => x.GetByLocalLicenseIdAsync(40))
            .ReturnsAsync(Result<List<InternationalDto>>.Success([dto]));

        var response = await _client.SendAsync(
            CreateAuthenticatedRequest("/api/InternationalLicenses/license/40"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<InternationalLicenseListResponse>>();
        Assert.NotNull(result);
        AssertLicenseListResponse(dto, Assert.Single(result));
    }

    [Fact]
    public async Task GetByLocalLicenseId_WhenServiceFails_ReturnsConflict()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.InternationalServiceMock.Setup(x => x.GetByLocalLicenseIdAsync(40))
            .ReturnsAsync(Result<List<InternationalDto>>.FromConflict(
                "International license conflict."));

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync("/api/InternationalLicenses/license/40"),
            HttpStatusCode.Conflict,
            "Conflict",
            "International license conflict.");
    }

    [Fact]
    public async Task GetLocalLicenseInfo_WhenSuccessful_ReturnsMappedInfo()
    {
        var dto = CreateDriverLicenseInfoDto();

        _factory.InternationalServiceMock.Setup(x => x.GetLocalLicenseInfoAsync(50))
            .ReturnsAsync(Result<DriverLicenseInfoDto>.Success(dto));

        var response = await _client.SendAsync(
            CreateAuthenticatedRequest("/api/InternationalLicenses/license/50/info"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DriverLicenseInfoResponse>();
        Assert.NotNull(result);

        Assert.Equal(dto.LicenseId, result.LicenseId);
        Assert.Equal(dto.LicenseClass, result.LicenseClass);
        Assert.Equal(dto.IssueDate, result.IssueDate);
        Assert.Equal(dto.ExpirationDate, result.ExpirationDate);
        Assert.Equal(dto.IsActive, result.IsActive);
        Assert.Equal(dto.IsDetained, result.IsDetained);
        Assert.Equal(dto.IssueReason, result.IssueReason);
        Assert.Equal(dto.Notes, result.Notes);
        Assert.Equal(dto.LicenseClassFees, result.LicenseClassFees);
        Assert.Equal(dto.DriverId, result.DriverId);
        Assert.Equal(dto.PersonID, result.PersonId);
        Assert.Equal(dto.FullName, result.FullName);
        Assert.Equal(dto.NationalNo, result.NationalNo);
        Assert.Equal(dto.DateOfBirth, result.DateOfBirth);
        Assert.Equal(dto.Gender, result.Gender);
        Assert.Equal(dto.ImagePath, result.ImagePath);
    }

    [Fact]
    public async Task GetLocalLicenseInfo_WhenNotFoundFailure_ReturnsNotFound()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.InternationalServiceMock.Setup(x => x.GetLocalLicenseInfoAsync(50))
            .ReturnsAsync(Result<DriverLicenseInfoDto>.FromNotFound("License not found."));

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync("/api/InternationalLicenses/license/50/info"),
            HttpStatusCode.NotFound, "Resource not found", "License not found.");
    }

    [Fact]
    public async Task GetLocalLicenseInfo_WhenResultValueIsNull_ReturnsNotFound()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.InternationalServiceMock.Setup(x => x.GetLocalLicenseInfoAsync(50))
            .ReturnsAsync(Result<DriverLicenseInfoDto>.Success(null!));

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync("/api/InternationalLicenses/license/50/info"),
            HttpStatusCode.NotFound, "Resource not found", "License not found.");
    }

    [Fact]
    public async Task Issue_WhenSuccessful_ReturnsIssuedLicense()
    {
        var dto = CreateInternationalDto(100, issuedUsingLocalLicenseId: 25);

        _factory.InternationalServiceMock.Setup(x => x.IssueInternationalLicenseAsync(25))
            .ReturnsAsync(Result<int>.Success(100));
        _factory.InternationalServiceMock.Setup(x => x.GetByIdAsync(100))
            .ReturnsAsync(Result<InternationalDto>.Success(dto));

        var request = CreateAuthenticatedRequest("/api/InternationalLicenses", HttpMethod.Post);
        request.Content = JsonContent.Create(new IssueInternationalLicenseRequest(25));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<InternationalLicenseResponse>();
        Assert.NotNull(result);
        AssertLicenseResponse(dto, result);

        _factory.InternationalServiceMock.Verify(
            x => x.IssueInternationalLicenseAsync(25), Times.Once);
        _factory.InternationalServiceMock.Verify(
            x => x.GetByIdAsync(100), Times.Once);
    }

    [Fact]
    public async Task Issue_WhenIssuanceFails_ReturnsConflict()
    {
        await using var factory = new ApiWebApplicationFactory();

        _factory.InternationalServiceMock.Setup(x => x.IssueInternationalLicenseAsync(25))
            .ReturnsAsync(Result<int>.FromConflict("An international license already exists."));

        var request = CreateAuthenticatedRequest("/api/InternationalLicenses", HttpMethod.Post);
        request.Content = JsonContent.Create(new IssueInternationalLicenseRequest(25));

        await AssertProblemDetailsAsync(
            await _client.SendAsync(request),
            HttpStatusCode.Conflict,
            "Conflict",
            "An international license already exists.");

        _factory.InternationalServiceMock.Verify(
            x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Issue_WhenIssuedLicenseCannotBeRetrieved_ReturnsNotFound()
    {
        await using var factory = new ApiWebApplicationFactory();

        _factory.InternationalServiceMock.Setup(x => x.IssueInternationalLicenseAsync(25))
            .ReturnsAsync(Result<int>.Success(100));
        _factory.InternationalServiceMock.Setup(x => x.GetByIdAsync(100))
            .ReturnsAsync(Result<InternationalDto>.Success(null!));

        var request = CreateAuthenticatedRequest("/api/InternationalLicenses", HttpMethod.Post);
        request.Content = JsonContent.Create(new IssueInternationalLicenseRequest(25));

        await AssertProblemDetailsAsync(
            await _client.SendAsync(request),
            HttpStatusCode.NotFound,
            "Resource not found",
            "International license was issued but could not be retrieved.");
    }

    [Fact]
    public async Task Issue_WhenRetrievalFails_ReturnsInternalServerError()
    {
        await using var factory = new ApiWebApplicationFactory();

        _factory.InternationalServiceMock.Setup(x => x.IssueInternationalLicenseAsync(25))
            .ReturnsAsync(Result<int>.Success(100));
        _factory.InternationalServiceMock.Setup(x => x.GetByIdAsync(100))
            .ReturnsAsync(Result<InternationalDto>.FromFailure(
                "Failed to retrieve international license."));

        var request = CreateAuthenticatedRequest("/api/InternationalLicenses", HttpMethod.Post);
        request.Content = JsonContent.Create(new IssueInternationalLicenseRequest(25));

        await AssertProblemDetailsAsync(
            await _client.SendAsync(request),
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        string url, HttpMethod? method = null, string role = "Staff")
    {
        var request = new HttpRequestMessage(method ?? HttpMethod.Get, url);
        request.Headers.Add("X-Test-User-Id", "1");
        request.Headers.Add("X-Test-Username", "testuser");
        request.Headers.Add("X-Test-FullName", "Test User");
        request.Headers.Add("X-Test-Role", role);
        return request;
    }

    private static HttpClient Authenticated(ApiWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", "1");
        client.DefaultRequestHeaders.Add("X-Test-Username", "testuser");
        client.DefaultRequestHeaders.Add("X-Test-FullName", "Test User");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Staff");
        return client;
    }

    private static InternationalDto CreateInternationalDto(
        int internationalLicenseId = 1, int applicationId = 10,
        int driverId = 20, int issuedUsingLocalLicenseId = 30) => new()
        {
            InternationalLicenseID = internationalLicenseId,
            ApplicationID = applicationId,
            DriverID = driverId,
            IssuedUsingLocalLicenseID = issuedUsingLocalLicenseId,
            IssueDate = new DateTime(2026, 1, 1),
            ExpirationDate = new DateTime(2027, 1, 1),
            IsActive = true,
            CreatedByUserID = 40,
            PersonID = 50,
            FullName = "Test Person",
            DateOfBirth = new DateTime(1990, 1, 1),
            ImagePath = "test.jpg",
            NationalNo = "123456789",
            Gender = "Male",
            Fees = 50m,
            CreatedByUserName = "admin"
        };

    private static DriverLicenseInfoDto CreateDriverLicenseInfoDto() => new()
    {
        LicenseId = 50,
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

    private static void AssertLicenseListResponse(
        InternationalDto dto, InternationalLicenseListResponse response)
    {
        Assert.Equal(dto.InternationalLicenseID, response.InternationalLicenseId);
        Assert.Equal(dto.ApplicationID, response.ApplicationId);
        Assert.Equal(dto.DriverID, response.DriverId);
        Assert.Equal(dto.IssuedUsingLocalLicenseID, response.IssuedUsingLocalLicenseId);
        Assert.Equal(dto.PersonID, response.PersonId);
        Assert.Equal(dto.IssueDate, response.IssueDate);
        Assert.Equal(dto.ExpirationDate, response.ExpirationDate);
        Assert.Equal(dto.IsActive, response.IsActive);
    }

    private static void AssertLicenseResponse(
        InternationalDto dto, InternationalLicenseResponse response)
    {
        Assert.Equal(dto.InternationalLicenseID, response.InternationalLicenseId);
        Assert.Equal(dto.ApplicationID, response.ApplicationId);
        Assert.Equal(dto.DriverID, response.DriverId);
        Assert.Equal(dto.IssuedUsingLocalLicenseID, response.IssuedUsingLocalLicenseId);
        Assert.Equal(dto.IssueDate, response.IssueDate);
        Assert.Equal(dto.ExpirationDate, response.ExpirationDate);
        Assert.Equal(dto.IsActive, response.IsActive);
        Assert.Equal(dto.CreatedByUserID, response.CreatedByUserId);
        Assert.Equal(dto.PersonID, response.PersonId);
        Assert.Equal(dto.FullName, response.FullName);
        Assert.Equal(dto.DateOfBirth, response.DateOfBirth);
        Assert.Equal(dto.ImagePath, response.ImagePath);
        Assert.Equal(dto.NationalNo, response.NationalNo);
        Assert.Equal(dto.Gender, response.Gender);
        Assert.Equal(dto.Fees, response.Fees);
        Assert.Equal(dto.CreatedByUserName, response.CreatedByUserName);
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response, HttpStatusCode status,
        string title, string detail)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var body = document.RootElement;

        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.Equal(title, body.GetProperty("title").GetString());
        Assert.Equal(detail, body.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            body.GetProperty("instance").GetString()));
        Assert.True(body.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}