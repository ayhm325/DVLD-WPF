using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.InternationalLicenseDTO;
using Application.DTOs.LicenseDTO;
using DVLD.Contracts.InternationalLicense;
using DVLD.Contracts.License;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Controllers;

public sealed class InternationalLicensesControllerTests
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public InternationalLicensesControllerTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.InternationalServiceMock.Reset();

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
                "/api/InternationalLicenses");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateInternationalDto();

        _factory.InternationalServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<InternationalDto>>.Success(
                    new List<InternationalDto> { dto }));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<InternationalLicenseResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        AssertLicenseResponse(dto, result[0]);
    }

    [Fact]
    public async Task GetAll_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory.InternationalServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<InternationalDto>>
                    .FromValidationFailure(
                        "Invalid license data."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid license data.");
    }

    [Fact]
    public async Task GetAll_WhenNotFoundFailure_ReturnsNotFound()
    {
        _factory.InternationalServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<InternationalDto>>
                    .FromNotFound(
                        "International licenses not found."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses"));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "International licenses not found.");
    }

    [Fact]
    public async Task GetAll_WhenConflictFailure_ReturnsConflict()
    {
        _factory.InternationalServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<InternationalDto>>
                    .FromConflict(
                        "International license conflict."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "International license conflict.");
    }

    [Fact]
    public async Task GetAll_WhenForbiddenFailure_ReturnsForbidden()
    {
        _factory.InternationalServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<InternationalDto>>
                    .FromForbidden(
                        "Access denied."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses"));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Access denied.");
    }

    [Fact]
    public async Task GetAll_WhenFailure_ReturnsInternalServerError()
    {
        _factory.InternationalServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<InternationalDto>>
                    .FromFailure(
                        "Database failure."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses"));

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
    public async Task GetById_WhenSuccessful_ReturnsMappedLicense()
    {
        var dto = CreateInternationalDto(
            internationalLicenseId: 10);

        _factory.InternationalServiceMock
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(
                Result<InternationalDto>.Success(dto));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses/10"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<InternationalLicenseResponse>();

        Assert.NotNull(result);

        AssertLicenseResponse(dto, result);
    }

    [Fact]
    public async Task GetById_WhenNotFoundFailure_ReturnsNotFound()
    {
        _factory.InternationalServiceMock
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(
                Result<InternationalDto>
                    .FromNotFound(
                        "International license not found."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses/10"));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "International license not found.");
    }

    [Fact]
    public async Task GetById_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory.InternationalServiceMock
            .Setup(x => x.GetByIdAsync(0))
            .ReturnsAsync(
                Result<InternationalDto>
                    .FromValidationFailure(
                        "Invalid international license ID."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses/0"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid international license ID.");
    }

    [Fact]
    public async Task GetById_WhenResultValueIsNull_ReturnsNotFound()
    {
        _factory.InternationalServiceMock
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(
                Result<InternationalDto>.Success(null!));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses/10"));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "International license not found.");
    }

    // =========================================================
    // GET BY DRIVER
    // =========================================================

    [Fact]
    public async Task GetByDriverId_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateInternationalDto(
            internationalLicenseId: 20);

        _factory.InternationalServiceMock
            .Setup(x => x.GetByDriverIdAsync(5))
            .ReturnsAsync(
                Result<List<InternationalDto>>.Success(
                    new List<InternationalDto> { dto }));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses/driver/5"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<InternationalLicenseResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        AssertLicenseResponse(
            dto,
            result[0]);
    }

    [Fact]
    public async Task GetByDriverId_WhenServiceFails_ReturnsMappedFailure()
    {
        _factory.InternationalServiceMock
            .Setup(x => x.GetByDriverIdAsync(5))
            .ReturnsAsync(
                Result<List<InternationalDto>>
                    .FromValidationFailure(
                        "Invalid driver ID."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses/driver/5"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid driver ID.");
    }

    // =========================================================
    // GET BY APPLICATION
    // =========================================================

    [Fact]
    public async Task GetByApplicationId_WhenSuccessful_ReturnsMappedLicense()
    {
        var dto = CreateInternationalDto(
            applicationId: 30);

        _factory.InternationalServiceMock
            .Setup(x => x.GetByApplicationIdAsync(30))
            .ReturnsAsync(
                Result<InternationalDto>.Success(dto));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses/application/30"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<InternationalLicenseResponse>();

        Assert.NotNull(result);

        AssertLicenseResponse(
            dto,
            result);
    }

    [Fact]
    public async Task GetByApplicationId_WhenNotFoundFailure_ReturnsNotFound()
    {
        _factory.InternationalServiceMock
            .Setup(x => x.GetByApplicationIdAsync(30))
            .ReturnsAsync(
                Result<InternationalDto>
                    .FromNotFound(
                        "International license not found."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses/application/30"));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "International license not found.");
    }

    [Fact]
    public async Task GetByApplicationId_WhenResultValueIsNull_ReturnsNotFound()
    {
        _factory.InternationalServiceMock
            .Setup(x => x.GetByApplicationIdAsync(30))
            .ReturnsAsync(
                Result<InternationalDto>.Success(null!));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses/application/30"));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "International license not found.");
    }

    // =========================================================
    // GET BY LOCAL LICENSE
    // =========================================================

    [Fact]
    public async Task GetByLocalLicenseId_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateInternationalDto(
            issuedUsingLocalLicenseId: 40);

        _factory.InternationalServiceMock
            .Setup(x => x.GetByLocalLicenseIdAsync(40))
            .ReturnsAsync(
                Result<List<InternationalDto>>.Success(
                    new List<InternationalDto> { dto }));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses/license/40"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<InternationalLicenseResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        AssertLicenseResponse(
            dto,
            result[0]);
    }

    [Fact]
    public async Task GetByLocalLicenseId_WhenServiceFails_ReturnsMappedFailure()
    {
        _factory.InternationalServiceMock
            .Setup(x => x.GetByLocalLicenseIdAsync(40))
            .ReturnsAsync(
                Result<List<InternationalDto>>
                    .FromConflict(
                        "International license conflict."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses/license/40"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "International license conflict.");
    }

    // =========================================================
    // GET LOCAL LICENSE INFO
    // =========================================================

    [Fact]
    public async Task GetLocalLicenseInfo_WhenSuccessful_ReturnsMappedInfo()
    {
        var dto = CreateDriverLicenseInfoDto();

        _factory.InternationalServiceMock
            .Setup(x => x.GetLocalLicenseInfoAsync(50))
            .ReturnsAsync(
                Result<DriverLicenseInfoDto>.Success(dto));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses/license/50/info"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<DriverLicenseInfoResponse>();

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
        _factory.InternationalServiceMock
            .Setup(x => x.GetLocalLicenseInfoAsync(50))
            .ReturnsAsync(
                Result<DriverLicenseInfoDto>
                    .FromNotFound(
                        "License not found."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses/license/50/info"));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License not found.");
    }

    [Fact]
    public async Task GetLocalLicenseInfo_WhenResultValueIsNull_ReturnsNotFound()
    {
        _factory.InternationalServiceMock
            .Setup(x => x.GetLocalLicenseInfoAsync(50))
            .ReturnsAsync(
                Result<DriverLicenseInfoDto>.Success(null!));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/InternationalLicenses/license/50/info"));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License not found.");
    }

    // =========================================================
    // ISSUE INTERNATIONAL LICENSE
    // =========================================================

    [Fact]
    public async Task Issue_WhenSuccessful_ReturnsIssuedLicense()
    {
        var dto = CreateInternationalDto(
            internationalLicenseId: 100,
            issuedUsingLocalLicenseId: 25);

        _factory.InternationalServiceMock
            .Setup(x =>
                x.IssueInternationalLicenseAsync(25))
            .ReturnsAsync(
                Result<int>.Success(100));

        _factory.InternationalServiceMock
            .Setup(x =>
                x.GetByIdAsync(100))
            .ReturnsAsync(
                Result<InternationalDto>.Success(dto));

        var request =
            CreateAuthenticatedRequest(
                "/api/InternationalLicenses",
                HttpMethod.Post);

        request.Content =
            JsonContent.Create(
                new IssueInternationalLicenseRequest(25));

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<InternationalLicenseResponse>();

        Assert.NotNull(result);

        AssertLicenseResponse(
            dto,
            result);

        _factory.InternationalServiceMock.Verify(
            x => x.IssueInternationalLicenseAsync(25),
            Times.Once);

        _factory.InternationalServiceMock.Verify(
            x => x.GetByIdAsync(100),
            Times.Once);
    }

    [Fact]
    public async Task Issue_WhenIssuanceFails_ReturnsMappedFailure()
    {
        _factory.InternationalServiceMock
            .Setup(x =>
                x.IssueInternationalLicenseAsync(25))
            .ReturnsAsync(
                Result<int>
                    .FromConflict(
                        "An international license already exists."));

        var request =
            CreateAuthenticatedRequest(
                "/api/InternationalLicenses",
                HttpMethod.Post);

        request.Content =
            JsonContent.Create(
                new IssueInternationalLicenseRequest(25));

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "An international license already exists.");

        _factory.InternationalServiceMock.Verify(
            x => x.GetByIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task Issue_WhenIssuedLicenseCannotBeRetrieved_ReturnsNotFound()
    {
        _factory.InternationalServiceMock
            .Setup(x =>
                x.IssueInternationalLicenseAsync(25))
            .ReturnsAsync(
                Result<int>.Success(100));

        _factory.InternationalServiceMock
            .Setup(x =>
                x.GetByIdAsync(100))
            .ReturnsAsync(
                Result<InternationalDto>.Success(null!));

        var request =
            CreateAuthenticatedRequest(
                "/api/InternationalLicenses",
                HttpMethod.Post);

        request.Content =
            JsonContent.Create(
                new IssueInternationalLicenseRequest(25));

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "International license was issued but could not be retrieved.");
    }

    [Fact]
    public async Task Issue_WhenRetrievalFails_ReturnsMappedFailure()
    {
        _factory.InternationalServiceMock
            .Setup(x =>
                x.IssueInternationalLicenseAsync(25))
            .ReturnsAsync(
                Result<int>.Success(100));

        _factory.InternationalServiceMock
            .Setup(x =>
                x.GetByIdAsync(100))
            .ReturnsAsync(
                Result<InternationalDto>
                    .FromFailure(
                        "Failed to retrieve international license."));

        var request =
            CreateAuthenticatedRequest(
                "/api/InternationalLicenses",
                HttpMethod.Post);

        request.Content =
            JsonContent.Create(
                new IssueInternationalLicenseRequest(25));

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Failed to retrieve international license.");
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static HttpRequestMessage CreateAuthenticatedRequest(
        string url,
        HttpMethod? method = null,
        string role = "Staff")
    {
        var request =
            new HttpRequestMessage(
                method ?? HttpMethod.Get,
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

    private static InternationalDto CreateInternationalDto(
        int internationalLicenseId = 1,
        int applicationId = 10,
        int driverId = 20,
        int issuedUsingLocalLicenseId = 30)
    {
        return new InternationalDto
        {
            InternationalLicenseID =
                internationalLicenseId,

            ApplicationID =
                applicationId,

            DriverID =
                driverId,

            IssuedUsingLocalLicenseID =
                issuedUsingLocalLicenseId,

            IssueDate =
                new DateTime(2026, 1, 1),

            ExpirationDate =
                new DateTime(2027, 1, 1),

            IsActive =
                true,

            CreatedByUserID =
                40,

            PersonID =
                50,

            FullName =
                "Test Person",

            DateOfBirth =
                new DateTime(1990, 1, 1),

            ImagePath =
                "test.jpg",

            NationalNo =
                "123456789",

            Gender =
                "Male",

            Fees =
                50m,

            CreatedByUserName =
                "admin"
        };
    }

    private static DriverLicenseInfoDto
        CreateDriverLicenseInfoDto()
    {
        return new DriverLicenseInfoDto
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
    }

    private static void AssertLicenseResponse(
        InternationalDto dto,
        InternationalLicenseResponse response)
    {
        Assert.Equal(
            dto.InternationalLicenseID,
            response.InternationalLicenseId);

        Assert.Equal(
            dto.ApplicationID,
            response.ApplicationId);

        Assert.Equal(
            dto.DriverID,
            response.DriverId);

        Assert.Equal(
            dto.IssuedUsingLocalLicenseID,
            response.IssuedUsingLocalLicenseId);

        Assert.Equal(
            dto.IssueDate,
            response.IssueDate);

        Assert.Equal(
            dto.ExpirationDate,
            response.ExpirationDate);

        Assert.Equal(
            dto.IsActive,
            response.IsActive);

        Assert.Equal(
            dto.CreatedByUserID,
            response.CreatedByUserId);

        Assert.Equal(
            dto.PersonID,
            response.PersonId);

        Assert.Equal(
            dto.FullName,
            response.FullName);

        Assert.Equal(
            dto.DateOfBirth,
            response.DateOfBirth);

        Assert.Equal(
            dto.ImagePath,
            response.ImagePath);

        Assert.Equal(
            dto.NationalNo,
            response.NationalNo);

        Assert.Equal(
            dto.Gender,
            response.Gender);

        Assert.Equal(
            dto.Fees,
            response.Fees);

        Assert.Equal(
            dto.CreatedByUserName,
            response.CreatedByUserName);
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
}