using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.DetainedLicenseDTO;
using Application.Interfaces;
using DVLD.Contracts.DetainedLicense;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Controllers;

public sealed class DetainedLicensesControllerTests
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DetainedLicensesControllerTests(
        ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.DetainedLicenseServiceMock.Reset();

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await _client.GetAsync(
                "/api/DetainedLicenses");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedResponses()
    {
        var detainDate =
            new DateTime(2026, 1, 10);

        var releaseDate =
            new DateTime(2026, 2, 10);

        var dto = new DetainedLicenseDto
        {
            DetainID = 10,
            LicenseID = 20,
            PersonID = 30,
            NationalNo = "N100",
            FullName = "Test Person",
            DetainDate = detainDate,
            FineFees = 150.50m,
            CreatedByUserID = 40,
            CreatedByUserName = "admin",
            IsReleased = true,
            ReleaseDate = releaseDate,
            ReleasedByUserID = 50,
            ReleaseApplicationID = 60
        };

        _factory.DetainedLicenseServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<DetainedLicenseDto>>.Success(
                    new List<DetainedLicenseDto>
                    {
                        dto
                    }));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/DetainedLicenses");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    List<DetainedLicenseResponse>>();

        Assert.NotNull(result);

        var item = Assert.Single(result);

        Assert.Equal(10, item.DetainId);
        Assert.Equal(20, item.LicenseId);
        Assert.Equal(30, item.PersonId);
        Assert.Equal("N100", item.NationalNo);
        Assert.Equal("Test Person", item.FullName);
        Assert.Equal(detainDate, item.DetainDate);
        Assert.Equal(150.50m, item.FineFees);
        Assert.Equal(40, item.CreatedByUserId);
        Assert.Equal("admin", item.CreatedByUserName);
        Assert.True(item.IsReleased);
        Assert.Equal(releaseDate, item.ReleaseDate);
        Assert.Equal(50, item.ReleasedByUserId);
        Assert.Equal(60, item.ReleaseApplicationId);

        _factory.DetainedLicenseServiceMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenServiceReturnsNullValue_ReturnsInternalServerError()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<DetainedLicenseDto>>.Success(
                    null!));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/DetainedLicenses");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "Detained license service returned no data.",
            error);
    }

    [Fact]
    public async Task GetAll_WhenValidationFails_ReturnsBadRequest()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<DetainedLicenseDto>>
                    .FromValidationFailure(
                        "validation error"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/DetainedLicenses");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "validation error",
            error);
    }

    [Fact]
    public async Task GetAll_WhenNotFound_ReturnsNotFound()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<DetainedLicenseDto>>
                    .FromNotFound(
                        "not found"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/DetainedLicenses");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "not found",
            error);
    }

    [Fact]
    public async Task GetAll_WhenConflict_ReturnsConflict()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<DetainedLicenseDto>>
                    .FromConflict(
                        "conflict"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/DetainedLicenses");

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "conflict",
            error);
    }

    [Fact]
    public async Task GetAll_WhenForbidden_ReturnsForbidden()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<DetainedLicenseDto>>
                    .FromForbidden(
                        "forbidden"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/DetainedLicenses");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "forbidden",
            error);
    }

    [Fact]
    public async Task GetAll_WhenFailureOccurs_ReturnsInternalServerError()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<DetainedLicenseDto>>
                    .FromFailure(
                        "failure"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/DetainedLicenses");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "failure",
            error);
    }

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedResponse()
    {
        var dto = new DetainedLicenseDto
        {
            DetainID = 1,
            LicenseID = 2,
            PersonID = 3,
            NationalNo = "N1",
            FullName = "John Doe",
            DetainDate = new DateTime(2026, 3, 1),
            FineFees = 100m,
            CreatedByUserID = 4,
            CreatedByUserName = "admin",
            IsReleased = false
        };

        _factory.DetainedLicenseServiceMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(
                Result<DetainedLicenseDto>.Success(dto));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/DetainedLicenses/1");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    DetainedLicenseResponse>();

        Assert.NotNull(result);

        Assert.Equal(1, result.DetainId);
        Assert.Equal(2, result.LicenseId);
        Assert.Equal(3, result.PersonId);
        Assert.Equal("N1", result.NationalNo);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal(100m, result.FineFees);
        Assert.False(result.IsReleased);

        _factory.DetainedLicenseServiceMock.Verify(
            x => x.GetByIdAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task GetById_WhenServiceReturnsNullValue_ReturnsInternalServerError()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.GetByIdAsync(7))
            .ReturnsAsync(
                Result<DetainedLicenseDto>.Success(
                    null!));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/DetainedLicenses/7");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "Detained license service returned no data.",
            error);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.GetByIdAsync(7))
            .ReturnsAsync(
                Result<DetainedLicenseDto>
                    .FromNotFound(
                        "not found"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/DetainedLicenses/7");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "not found",
            error);
    }

    [Fact]
    public async Task GetActiveByLicenseId_WhenSuccessful_ReturnsMappedResponse()
    {
        var dto = new DetainedLicenseDto
        {
            DetainID = 10,
            LicenseID = 20,
            PersonID = 30,
            NationalNo = "N20",
            FullName = "Active Driver",
            DetainDate = new DateTime(2026, 4, 1),
            FineFees = 200m,
            CreatedByUserID = 40,
            CreatedByUserName = "staff",
            IsReleased = false
        };

        _factory.DetainedLicenseServiceMock
            .Setup(x =>
                x.GetActiveDetainByLicenseIdAsync(20))
            .ReturnsAsync(
                Result<DetainedLicenseDto>.Success(dto));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/DetainedLicenses/license/20/active");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    DetainedLicenseResponse>();

        Assert.NotNull(result);

        Assert.Equal(10, result.DetainId);
        Assert.Equal(20, result.LicenseId);
        Assert.Equal("Active Driver", result.FullName);
        Assert.False(result.IsReleased);

        _factory.DetainedLicenseServiceMock.Verify(
            x =>
                x.GetActiveDetainByLicenseIdAsync(20),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveByLicenseId_WhenNotFound_ReturnsNotFound()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x =>
                x.GetActiveDetainByLicenseIdAsync(20))
            .ReturnsAsync(
                Result<DetainedLicenseDto>
                    .FromNotFound(
                        "not found"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/DetainedLicenses/license/20/active");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "not found",
            error);
    }

    [Fact]
    public async Task IsDetained_WhenServiceReturnsTrue_ReturnsTrue()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.IsLicenseDetainedAsync(20))
            .ReturnsAsync(true);

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/DetainedLicenses/license/20/detained");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    DetainedStatusResponse>();

        Assert.NotNull(result);
        Assert.True(result.Detained);

        _factory.DetainedLicenseServiceMock.Verify(
            x => x.IsLicenseDetainedAsync(20),
            Times.Once);
    }

    [Fact]
    public async Task IsDetained_WhenServiceReturnsFalse_ReturnsFalse()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.IsLicenseDetainedAsync(20))
            .ReturnsAsync(false);

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/DetainedLicenses/license/20/detained");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    DetainedStatusResponse>();

        Assert.NotNull(result);
        Assert.False(result.Detained);
    }

    [Fact]
    public async Task Detain_WhenSuccessful_MapsRequestAndReturnsResponse()
    {
        var dto = new DetainedLicenseDto
        {
            DetainID = 100,
            LicenseID = 200,
            PersonID = 300,
            NationalNo = "N300",
            FullName = "Detained Person",
            DetainDate = new DateTime(2026, 5, 1),
            FineFees = 250m,
            CreatedByUserID = 400,
            CreatedByUserName = "admin",
            IsReleased = false
        };

        _factory.DetainedLicenseServiceMock
            .Setup(x =>
                x.AddAsync(
                    It.Is<CreateDetainedLicenseDto>(
                        d =>
                            d.LicenseID == 200 &&
                            d.FineFees == 250m)))
            .ReturnsAsync(
                Result<DetainedLicenseDto>.Success(dto));

        var request =
            new CreateDetainedLicenseRequest
            {
                LicenseId = 200,
                FineFees = 250m
            };

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/DetainedLicenses",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    DetainedLicenseResponse>();

        Assert.NotNull(result);

        Assert.Equal(100, result.DetainId);
        Assert.Equal(200, result.LicenseId);
        Assert.Equal(250m, result.FineFees);

        _factory.DetainedLicenseServiceMock.Verify(
            x =>
                x.AddAsync(
                    It.Is<CreateDetainedLicenseDto>(
                        d =>
                            d.LicenseID == 200 &&
                            d.FineFees == 250m)),
            Times.Once);
    }

    [Fact]
    public async Task Detain_WhenValidationFails_ReturnsBadRequest()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<CreateDetainedLicenseDto>()))
            .ReturnsAsync(
                Result<DetainedLicenseDto>
                    .FromValidationFailure(
                        "validation error"));

        var request =
            new CreateDetainedLicenseRequest
            {
                LicenseId = 1,
                FineFees = 10m
            };

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/DetainedLicenses",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "validation error",
            error);
    }

    [Fact]
    public async Task Detain_WhenServiceReturnsNullValue_ReturnsInternalServerError()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<CreateDetainedLicenseDto>()))
            .ReturnsAsync(
                Result<DetainedLicenseDto>.Success(
                    null!));

        var request =
            new CreateDetainedLicenseRequest
            {
                LicenseId = 1,
                FineFees = 10m
            };

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/DetainedLicenses",
                request);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "Detained license service returned no data.",
            error);
    }

    [Fact]
    public async Task Release_WhenSuccessful_ReturnsNoContent()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x =>
                x.ReleaseAsync(
                    It.Is<ReleaseDetainedLicenseDto>(
                        d => d.DetainID == 500)))
            .ReturnsAsync(
                Result.Success());

        var request =
            new ReleaseDetainedLicenseRequest
            {
                DetainId = 500
            };

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/DetainedLicenses/release",
                request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        _factory.DetainedLicenseServiceMock.Verify(
            x =>
                x.ReleaseAsync(
                    It.Is<ReleaseDetainedLicenseDto>(
                        d => d.DetainID == 500)),
            Times.Once);
    }

    [Fact]
    public async Task Release_WhenNotFound_ReturnsNotFound()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x =>
                x.ReleaseAsync(
                    It.IsAny<ReleaseDetainedLicenseDto>()))
            .ReturnsAsync(
                Result.NotFound(
                    "not found"));

        var request =
            new ReleaseDetainedLicenseRequest
            {
                DetainId = 500
            };

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/DetainedLicenses/release",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "not found",
            error);
    }

    [Fact]
    public async Task Release_WhenForbidden_ReturnsForbidden()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x =>
                x.ReleaseAsync(
                    It.IsAny<ReleaseDetainedLicenseDto>()))
            .ReturnsAsync(
                Result.Forbidden(
                    "forbidden"));

        var request =
            new ReleaseDetainedLicenseRequest
            {
                DetainId = 500
            };

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/DetainedLicenses/release",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "forbidden",
            error);
    }

    private async Task<HttpResponseMessage>
        SendAuthenticatedAsync(
            HttpMethod method,
            string url,
            object? content = null)
    {
        using var request =
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
            "Staff");

        if (content is not null)
        {
            request.Content =
                JsonContent.Create(content);
        }

        return await _client.SendAsync(request);
    }

    private static async Task<string?>
        ReadErrorAsync(
            HttpResponseMessage response)
    {
        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        return body?.Error;
    }

    private sealed record ErrorResponse(
        string? Error);

    private sealed record DetainedStatusResponse(
        bool Detained);
}