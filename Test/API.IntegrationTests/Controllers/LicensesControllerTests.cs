using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Domain.Enums;
using DVLD.Contracts.License;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Controllers;

public sealed class LicensesControllerTests
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LicensesControllerTests(
        ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // =========================================================
    // AUTHORIZATION
    // =========================================================

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await _client.GetAsync("/api/Licenses");

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
        var dto = CreateLicenseDto();

        _factory.LicenseServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<LicenseDto>>.Success(
                    new List<LicenseDto>
                    {
                        dto
                    }));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<LicenseResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        var item = result[0];

        Assert.Equal(
            dto.LicenseID,
            item.LicenseId);

        Assert.Equal(
            dto.ApplicationID,
            item.ApplicationId);

        Assert.Equal(
            dto.DriverID,
            item.DriverId);

        Assert.Equal(
            dto.DriverName,
            item.DriverName);

        Assert.Equal(
            dto.LicenseClassID,
            item.LicenseClassId);

        Assert.Equal(
            dto.LicenseClassName,
            item.LicenseClassName);

        Assert.Equal(
            dto.IssueDate,
            item.IssueDate);

        Assert.Equal(
            dto.ExpirationDate,
            item.ExpirationDate);

        Assert.Equal(
            dto.Notes,
            item.Notes);

        Assert.Equal(
            dto.PaidFees,
            item.PaidFees);

        Assert.Equal(
            dto.IsActive,
            item.IsActive);

        Assert.Equal(
            dto.IssueReason,
            item.IssueReason);

        Assert.Equal(
            dto.IssueReasonText,
            item.IssueReasonText);

        Assert.Equal(
            dto.CreatedByUserID,
            item.CreatedByUserId);

        Assert.Equal(
            dto.CreatedByUserName,
            item.CreatedByUserName);
    }

    [Fact]
    public async Task GetAll_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<LicenseDto>>
                    .FromValidationFailure(
                        "Invalid license data."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses"));

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
        _factory.LicenseServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<LicenseDto>>
                    .FromNotFound(
                        "Licenses not found."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses"));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Licenses not found.");
    }

    [Fact]
    public async Task GetAll_WhenConflictFailure_ReturnsConflict()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<LicenseDto>>
                    .FromConflict(
                        "License conflict."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License conflict.");
    }

    [Fact]
    public async Task GetAll_WhenForbiddenFailure_ReturnsForbidden()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<LicenseDto>>
                    .FromForbidden(
                        "Access denied."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses"));

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
        _factory.LicenseServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<LicenseDto>>
                    .FromFailure(
                        "Database failure."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses"));

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Database failure.");
    }

    [Fact]
    public async Task GetAll_WhenResultValueIsNull_ReturnsInternalServerError()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<LicenseDto>>
                    .Success(null!));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses"));

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License service returned no data.");
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedLicense()
    {
        var dto = CreateLicenseDto(10);

        _factory.LicenseServiceMock
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(
                Result<LicenseDto>.Success(dto));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/10"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<LicenseResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            dto.LicenseID,
            result.LicenseId);

        Assert.Equal(
            dto.ApplicationID,
            result.ApplicationId);

        Assert.Equal(
            dto.DriverID,
            result.DriverId);

        Assert.Equal(
            dto.DriverName,
            result.DriverName);

        Assert.Equal(
            dto.LicenseClassID,
            result.LicenseClassId);

        Assert.Equal(
            dto.LicenseClassName,
            result.LicenseClassName);

        Assert.Equal(
            dto.IssueDate,
            result.IssueDate);

        Assert.Equal(
            dto.ExpirationDate,
            result.ExpirationDate);

        Assert.Equal(
            dto.Notes,
            result.Notes);

        Assert.Equal(
            dto.PaidFees,
            result.PaidFees);

        Assert.Equal(
            dto.IsActive,
            result.IsActive);

        Assert.Equal(
            dto.IssueReason,
            result.IssueReason);

        Assert.Equal(
            dto.IssueReasonText,
            result.IssueReasonText);

        Assert.Equal(
            dto.CreatedByUserID,
            result.CreatedByUserId);

        Assert.Equal(
            dto.CreatedByUserName,
            result.CreatedByUserName);
    }

    [Fact]
    public async Task GetById_WhenNotFoundFailure_ReturnsNotFound()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(
                Result<LicenseDto>
                    .FromNotFound(
                        "License not found."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/10"));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License not found.");
    }

    [Fact]
    public async Task GetById_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetByIdAsync(0))
            .ReturnsAsync(
                Result<LicenseDto>
                    .FromValidationFailure(
                        "Invalid license ID."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/0"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid license ID.");
    }

    [Fact]
    public async Task GetById_WhenResultValueIsNull_ReturnsInternalServerError()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(
                Result<LicenseDto>
                    .Success(null!));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/10"));

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License service returned no data.");
    }

    // =========================================================
    // GET BY DRIVER ID
    // =========================================================

    [Fact]
    public async Task GetByDriverId_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateLicenseDto(20);

        _factory.LicenseServiceMock
            .Setup(x => x.GetByDriverIdAsync(5))
            .ReturnsAsync(
                Result<List<LicenseDto>>.Success(
                    new List<LicenseDto>
                    {
                        dto
                    }));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/driver/5"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<LicenseResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        Assert.Equal(
            dto.LicenseID,
            result[0].LicenseId);

        Assert.Equal(
            dto.DriverID,
            result[0].DriverId);
    }

    [Fact]
    public async Task GetByDriverId_WhenServiceFails_ReturnsMappedFailure()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetByDriverIdAsync(5))
            .ReturnsAsync(
                Result<List<LicenseDto>>
                    .FromNotFound(
                        "Driver licenses not found."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/driver/5"));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Driver licenses not found.");
    }

    [Fact]
    public async Task GetByDriverId_WhenResultValueIsNull_ReturnsInternalServerError()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetByDriverIdAsync(5))
            .ReturnsAsync(
                Result<List<LicenseDto>>
                    .Success(null!));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/driver/5"));

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License service returned no data.");
    }

    // =========================================================
    // GET BY APPLICATION ID
    // =========================================================

    [Fact]
    public async Task GetByApplicationId_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateLicenseDto(30);

        _factory.LicenseServiceMock
            .Setup(x => x.GetByApplicationIdAsync(7))
            .ReturnsAsync(
                Result<List<LicenseDto>>.Success(
                    new List<LicenseDto>
                    {
                        dto
                    }));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/application/7"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<LicenseResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        Assert.Equal(
            dto.LicenseID,
            result[0].LicenseId);

        Assert.Equal(
            dto.ApplicationID,
            result[0].ApplicationId);
    }

    [Fact]
    public async Task GetByApplicationId_WhenServiceFails_ReturnsMappedFailure()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetByApplicationIdAsync(7))
            .ReturnsAsync(
                Result<List<LicenseDto>>
                    .FromValidationFailure(
                        "Invalid application ID."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/application/7"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid application ID.");
    }

    [Fact]
    public async Task GetByApplicationId_WhenResultValueIsNull_ReturnsInternalServerError()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetByApplicationIdAsync(7))
            .ReturnsAsync(
                Result<List<LicenseDto>>
                    .Success(null!));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/application/7"));

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License service returned no data.");
    }

    // =========================================================
    // GET BY LICENSE CLASS ID
    // =========================================================

    [Fact]
    public async Task GetByLicenseClassId_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateLicenseDto(40);

        _factory.LicenseServiceMock
            .Setup(x => x.GetByLicenseClassIdAsync(2))
            .ReturnsAsync(
                Result<List<LicenseDto>>.Success(
                    new List<LicenseDto>
                    {
                        dto
                    }));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/license-class/2"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<LicenseResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        Assert.Equal(
            dto.LicenseID,
            result[0].LicenseId);

        Assert.Equal(
            dto.LicenseClassID,
            result[0].LicenseClassId);
    }

    [Fact]
    public async Task GetByLicenseClassId_WhenServiceFails_ReturnsMappedFailure()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetByLicenseClassIdAsync(2))
            .ReturnsAsync(
                Result<List<LicenseDto>>
                    .FromConflict(
                        "License class conflict."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/license-class/2"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License class conflict.");
    }

    [Fact]
    public async Task GetByLicenseClassId_WhenResultValueIsNull_ReturnsInternalServerError()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetByLicenseClassIdAsync(2))
            .ReturnsAsync(
                Result<List<LicenseDto>>
                    .Success(null!));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/license-class/2"));

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License service returned no data.");
    }

    // =========================================================
    // GET BY PERSON ID
    // =========================================================

    [Fact]
    public async Task GetByPersonId_WhenSuccessful_ReturnsMappedLicenses()
    {
        var dto = CreateLicenseDto(50);

        _factory.LicenseServiceMock
            .Setup(x => x.GetLicensesByPersonIdAsync(15))
            .ReturnsAsync(
                Result<List<LicenseDto>>.Success(
                    new List<LicenseDto>
                    {
                        dto
                    }));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/person/15"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<LicenseResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        Assert.Equal(
            dto.LicenseID,
            result[0].LicenseId);

        Assert.Equal(
            dto.DriverID,
            result[0].DriverId);
    }

    [Fact]
    public async Task GetByPersonId_WhenServiceFails_ReturnsMappedFailure()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetLicensesByPersonIdAsync(15))
            .ReturnsAsync(
                Result<List<LicenseDto>>
                    .FromForbidden(
                        "Access denied."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/person/15"));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Access denied.");
    }

    [Fact]
    public async Task GetByPersonId_WhenResultValueIsNull_ReturnsInternalServerError()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetLicensesByPersonIdAsync(15))
            .ReturnsAsync(
                Result<List<LicenseDto>>
                    .Success(null!));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/person/15"));

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License service returned no data.");
    }

    // =========================================================
    // GET DETAILS BY LOCAL APPLICATION
    // =========================================================

    [Fact]
    public async Task GetDetails_WhenSuccessful_ReturnsMappedDetails()
    {
        var dto = CreateDriverLicenseInfoDto();

        _factory.LicenseServiceMock
            .Setup(x => x.GetDetailsAsync(100))
            .ReturnsAsync(
                Result<DriverLicenseInfoDto>
                    .Success(dto));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/local-application/100/details"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<DriverLicenseInfoResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            dto.LicenseId,
            result.LicenseId);

        Assert.Equal(
            dto.LicenseClass,
            result.LicenseClass);

        Assert.Equal(
            dto.IssueDate,
            result.IssueDate);

        Assert.Equal(
            dto.ExpirationDate,
            result.ExpirationDate);

        Assert.Equal(
            dto.IsActive,
            result.IsActive);

        Assert.Equal(
            dto.IsDetained,
            result.IsDetained);

        Assert.Equal(
            dto.IssueReason,
            result.IssueReason);

        Assert.Equal(
            dto.Notes,
            result.Notes);

        Assert.Equal(
            dto.LicenseClassFees,
            result.LicenseClassFees);

        Assert.Equal(
            dto.DriverId,
            result.DriverId);

        Assert.Equal(
            dto.PersonID,
            result.PersonId);

        Assert.Equal(
            dto.FullName,
            result.FullName);

        Assert.Equal(
            dto.NationalNo,
            result.NationalNo);

        Assert.Equal(
            dto.DateOfBirth,
            result.DateOfBirth);

        Assert.Equal(
            dto.Gender,
            result.Gender);

        Assert.Equal(
            dto.ImagePath,
            result.ImagePath);
    }

    [Fact]
    public async Task GetDetails_WhenNotFoundFailure_ReturnsNotFound()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetDetailsAsync(100))
            .ReturnsAsync(
                Result<DriverLicenseInfoDto>
                    .FromNotFound(
                        "License details not found."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/local-application/100/details"));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License details not found.");
    }

    [Fact]
    public async Task GetDetails_WhenResultValueIsNull_ReturnsInternalServerError()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetDetailsAsync(100))
            .ReturnsAsync(
                Result<DriverLicenseInfoDto>
                    .Success(null!));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/local-application/100/details"));

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License service returned no data.");
    }

    // =========================================================
    // GET DETAILS BY LICENSE ID
    // =========================================================

    [Fact]
    public async Task GetDetailsById_WhenSuccessful_ReturnsMappedDetails()
    {
        var dto = CreateDriverLicenseInfoDto();

        dto.LicenseId = 200;

        _factory.LicenseServiceMock
            .Setup(x => x.GetLicenseDetailsByIdAsync(200))
            .ReturnsAsync(
                Result<DriverLicenseInfoDto>
                    .Success(dto));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/200/details"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<DriverLicenseInfoResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            dto.LicenseId,
            result.LicenseId);

        Assert.Equal(
            dto.LicenseClass,
            result.LicenseClass);

        Assert.Equal(
            dto.IssueDate,
            result.IssueDate);

        Assert.Equal(
            dto.ExpirationDate,
            result.ExpirationDate);

        Assert.Equal(
            dto.IsActive,
            result.IsActive);

        Assert.Equal(
            dto.IsDetained,
            result.IsDetained);

        Assert.Equal(
            dto.IssueReason,
            result.IssueReason);

        Assert.Equal(
            dto.Notes,
            result.Notes);

        Assert.Equal(
            dto.LicenseClassFees,
            result.LicenseClassFees);

        Assert.Equal(
            dto.DriverId,
            result.DriverId);

        Assert.Equal(
            dto.PersonID,
            result.PersonId);

        Assert.Equal(
            dto.FullName,
            result.FullName);

        Assert.Equal(
            dto.NationalNo,
            result.NationalNo);

        Assert.Equal(
            dto.DateOfBirth,
            result.DateOfBirth);

        Assert.Equal(
            dto.Gender,
            result.Gender);

        Assert.Equal(
            dto.ImagePath,
            result.ImagePath);
    }

    [Fact]
    public async Task GetDetailsById_WhenNotFoundFailure_ReturnsNotFound()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetLicenseDetailsByIdAsync(200))
            .ReturnsAsync(
                Result<DriverLicenseInfoDto>
                    .FromNotFound(
                        "License not found."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/200/details"));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License not found.");
    }

    [Fact]
    public async Task GetDetailsById_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetLicenseDetailsByIdAsync(0))
            .ReturnsAsync(
                Result<DriverLicenseInfoDto>
                    .FromValidationFailure(
                        "Invalid license ID."));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/0/details"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid license ID.");
    }

    [Fact]
    public async Task GetDetailsById_WhenResultValueIsNull_ReturnsInternalServerError()
    {
        _factory.LicenseServiceMock
            .Setup(x => x.GetLicenseDetailsByIdAsync(200))
            .ReturnsAsync(
                Result<DriverLicenseInfoDto>
                    .Success(null!));

        var response =
            await _client.SendAsync(
                CreateAuthenticatedRequest(
                    "/api/Licenses/200/details"));

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License service returned no data.");
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static HttpRequestMessage CreateAuthenticatedRequest(
        string url,
        string role = "Staff")
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
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

    private static LicenseDto CreateLicenseDto(
        int licenseId = 1)
    {
        return new LicenseDto
        {
            LicenseID = licenseId,
            ApplicationID = 10,
            DriverID = 20,
            DriverName = "Test Driver",
            LicenseClassID = 3,
            LicenseClassName = "Private",
            IssueDate = new DateTime(
                2026,
                1,
                1),
            ExpirationDate = new DateTime(
                2031,
                1,
                1),
            Notes = "Test license",
            PaidFees = 50m,
            IsActive = true,
            IssueReason =
                (byte)IssueReason.FirstTime,
            IssueReasonText =
                IssueReason.FirstTime.ToString(),
            CreatedByUserID = 30,
            CreatedByUserName = "admin"
        };
    }

    private static DriverLicenseInfoDto
        CreateDriverLicenseInfoDto()
    {
        return new DriverLicenseInfoDto
        {
            LicenseId = 100,
            LicenseClass = "Private",
            IssueDate = new DateTime(
                2026,
                1,
                1),
            ExpirationDate = new DateTime(
                2031,
                1,
                1),
            IsActive = true,
            IsDetained = false,
            IssueReason = "FirstTime",
            Notes = "Test notes",
            LicenseClassFees = 50m,
            DriverId = 20,
            PersonID = 30,
            FullName = "Test Person",
            NationalNo = "123456789",
            DateOfBirth = new DateTime(
                1990,
                1,
                1),
            Gender = "Male",
            ImagePath = "test.jpg"
        };
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