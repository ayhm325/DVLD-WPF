using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.DetainedLicenseDTO;
using DVLD.Contracts.DetainedLicense;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.IntegrationTests.Controllers;

public sealed class DetainedLicensesControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DetainedLicensesControllerTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.DetainedLicenseServiceMock.Reset();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized() =>
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.GetAsync("/api/DetainedLicenses")).StatusCode);

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedResponses()
    {
        var dto = new DetainedLicenseDto
        {
            DetainID = 10,
            LicenseID = 20,
            PersonID = 30,
            NationalNo = "N100",
            FullName = "Test Person",
            DetainDate = new(2026, 1, 10),
            FineFees = 150.50m,
            CreatedByUserID = 40,
            CreatedByUserName = "admin",
            IsReleased = true,
            ReleaseDate = new(2026, 2, 10),
            ReleasedByUserID = 50,
            ReleaseApplicationID = 60
        };

        SetupGetAll(Result<List<DetainedLicenseDto>>.Success([dto]));

        var response = await SendAsync(HttpMethod.Get, "/api/DetainedLicenses");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var item = Assert.Single(
            await response.Content.ReadFromJsonAsync<List<DetainedLicenseResponse>>() ?? []);

        Assert.Equal(10, item.DetainId);
        Assert.Equal(20, item.LicenseId);
        Assert.Equal(30, item.PersonId);
        Assert.Equal("N100", item.NationalNo);
        Assert.Equal("Test Person", item.FullName);
        Assert.Equal(new DateTime(2026, 1, 10), item.DetainDate);
        Assert.Equal(150.50m, item.FineFees);
        Assert.Equal(40, item.CreatedByUserId);
        Assert.Equal("admin", item.CreatedByUserName);
        Assert.True(item.IsReleased);
        Assert.Equal(new DateTime(2026, 2, 10), item.ReleaseDate);
        Assert.Equal(50, item.ReleasedByUserId);
        Assert.Equal(60, item.ReleaseApplicationId);

        _factory.DetainedLicenseServiceMock.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenServiceReturnsNullValue_ReturnsInternalServerError()
    {
        SetupGetAll(Result<List<DetainedLicenseDto>>.Success(null!));

        await AssertProblemDetailsAsync(
            await SendAsync(HttpMethod.Get, "/api/DetainedLicenses"),
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Validation error", "validation error")]
    [InlineData(HttpStatusCode.NotFound, "Resource not found", "not found")]
    [InlineData(HttpStatusCode.Conflict, "Conflict", "conflict")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden", "forbidden")]
    public async Task GetAll_WhenServiceReturnsFailure_ReturnsExpectedProblemDetails(
        HttpStatusCode status, string title, string detail)
    {
        var result = status switch
        {
            HttpStatusCode.BadRequest => Result<List<DetainedLicenseDto>>.FromValidationFailure(detail),
            HttpStatusCode.NotFound => Result<List<DetainedLicenseDto>>.FromNotFound(detail),
            HttpStatusCode.Conflict => Result<List<DetainedLicenseDto>>.FromConflict(detail),
            HttpStatusCode.Forbidden => Result<List<DetainedLicenseDto>>.FromForbidden(detail),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        SetupGetAll(result);

        await AssertProblemDetailsAsync(
            await SendAsync(HttpMethod.Get, "/api/DetainedLicenses"),
            status, title, detail);
    }

    [Fact]
    public async Task GetAll_WhenFailureOccurs_ReturnsInternalServerError()
    {
        SetupGetAll(Result<List<DetainedLicenseDto>>.FromFailure("failure"));

        await AssertProblemDetailsAsync(
            await SendAsync(HttpMethod.Get, "/api/DetainedLicenses"),
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
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
            DetainDate = new(2026, 3, 1),
            FineFees = 100m,
            CreatedByUserID = 4,
            CreatedByUserName = "admin",
            IsReleased = false
        };

        _factory.DetainedLicenseServiceMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(Result<DetainedLicenseDto>.Success(dto));

        var response = await SendAsync(HttpMethod.Get, "/api/DetainedLicenses/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DetainedLicenseResponse>();

        Assert.NotNull(result);
        Assert.Equal(1, result.DetainId);
        Assert.Equal(2, result.LicenseId);
        Assert.Equal(3, result.PersonId);
        Assert.Equal("N1", result.NationalNo);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal(100m, result.FineFees);
        Assert.False(result.IsReleased);

        _factory.DetainedLicenseServiceMock.Verify(x => x.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenServiceReturnsNullValue_ReturnsInternalServerError()
    {
        _factory.DetainedLicenseServiceMock.Setup(x => x.GetByIdAsync(7))
            .ReturnsAsync(Result<DetainedLicenseDto>.Success(null!));

        await AssertProblemDetailsAsync(
            await SendAsync(HttpMethod.Get, "/api/DetainedLicenses/7"),
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _factory.DetainedLicenseServiceMock.Setup(x => x.GetByIdAsync(7))
            .ReturnsAsync(Result<DetainedLicenseDto>.FromNotFound("not found"));

        await AssertProblemDetailsAsync(
            await SendAsync(HttpMethod.Get, "/api/DetainedLicenses/7"),
            HttpStatusCode.NotFound, "Resource not found", "not found");
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
            DetainDate = new(2026, 4, 1),
            FineFees = 200m,
            CreatedByUserID = 40,
            CreatedByUserName = "staff",
            IsReleased = false
        };

        _factory.DetainedLicenseServiceMock
            .Setup(x => x.GetActiveDetainByLicenseIdAsync(20))
            .ReturnsAsync(Result<DetainedLicenseDto>.Success(dto));

        var response = await SendAsync(
            HttpMethod.Get, "/api/DetainedLicenses/license/20/active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DetainedLicenseResponse>();

        Assert.NotNull(result);
        Assert.Equal(10, result.DetainId);
        Assert.Equal(20, result.LicenseId);
        Assert.Equal("Active Driver", result.FullName);
        Assert.False(result.IsReleased);

        _factory.DetainedLicenseServiceMock.Verify(
            x => x.GetActiveDetainByLicenseIdAsync(20), Times.Once);
    }

    [Fact]
    public async Task GetActiveByLicenseId_WhenNotFound_ReturnsNotFound()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.GetActiveDetainByLicenseIdAsync(20))
            .ReturnsAsync(Result<DetainedLicenseDto>.FromNotFound("not found"));

        await AssertProblemDetailsAsync(
            await SendAsync(HttpMethod.Get, "/api/DetainedLicenses/license/20/active"),
            HttpStatusCode.NotFound, "Resource not found", "not found");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsDetained_ReturnsExpectedResult(bool detained)
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.IsLicenseDetainedAsync(20)).ReturnsAsync(detained);

        var response = await SendAsync(
            HttpMethod.Get, "/api/DetainedLicenses/license/20/detained");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DetainedStatusResponse>();

        Assert.NotNull(result);
        Assert.Equal(detained, result.Detained);
        _factory.DetainedLicenseServiceMock.Verify(
            x => x.IsLicenseDetainedAsync(20), Times.Once);
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
            DetainDate = new(2026, 5, 1),
            FineFees = 250m,
            CreatedByUserID = 400,
            CreatedByUserName = "admin",
            IsReleased = false
        };

        _factory.DetainedLicenseServiceMock
            .Setup(x => x.AddAsync(It.Is<CreateDetainedLicenseDto>(
                d => d.LicenseID == 200 && d.FineFees == 250m)))
            .ReturnsAsync(Result<DetainedLicenseDto>.Success(dto));

        var response = await SendAsync(
            HttpMethod.Post, "/api/DetainedLicenses",
            new CreateDetainedLicenseRequest { LicenseId = 200, FineFees = 250m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DetainedLicenseResponse>();

        Assert.NotNull(result);
        Assert.Equal(100, result.DetainId);
        Assert.Equal(200, result.LicenseId);
        Assert.Equal(250m, result.FineFees);

        _factory.DetainedLicenseServiceMock.Verify(
            x => x.AddAsync(It.Is<CreateDetainedLicenseDto>(
                d => d.LicenseID == 200 && d.FineFees == 250m)), Times.Once);
    }

    [Fact]
    public async Task Detain_WhenValidationFails_ReturnsBadRequest()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.AddAsync(It.IsAny<CreateDetainedLicenseDto>()))
            .ReturnsAsync(Result<DetainedLicenseDto>.FromValidationFailure("validation error"));

        await AssertProblemDetailsAsync(
            await SendAsync(HttpMethod.Post, "/api/DetainedLicenses",
                new CreateDetainedLicenseRequest { LicenseId = 1, FineFees = 10m }),
            HttpStatusCode.BadRequest, "Validation error", "validation error");
    }

    [Fact]
    public async Task Detain_WhenServiceReturnsNullValue_ReturnsInternalServerError()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.AddAsync(It.IsAny<CreateDetainedLicenseDto>()))
            .ReturnsAsync(Result<DetainedLicenseDto>.Success(null!));

        await AssertProblemDetailsAsync(
            await SendAsync(HttpMethod.Post, "/api/DetainedLicenses",
                new CreateDetainedLicenseRequest { LicenseId = 1, FineFees = 10m }),
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task Release_WhenSuccessful_ReturnsNoContent()
    {
        _factory.DetainedLicenseServiceMock
            .Setup(x => x.ReleaseAsync(It.Is<ReleaseDetainedLicenseDto>(
                d => d.DetainID == 500)))
            .ReturnsAsync(Result.Success());

        var response = await SendAsync(
            HttpMethod.Post, "/api/DetainedLicenses/release",
            new ReleaseDetainedLicenseRequest { DetainId = 500 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.DetainedLicenseServiceMock.Verify(
            x => x.ReleaseAsync(It.Is<ReleaseDetainedLicenseDto>(
                d => d.DetainID == 500)), Times.Once);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "Resource not found", "not found")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden", "forbidden")]
    public async Task Release_WhenServiceFails_ReturnsExpectedProblemDetails(
        HttpStatusCode status, string title, string detail)
    {
        var result = status == HttpStatusCode.NotFound
            ? Result.NotFound(detail)
            : Result.Forbidden(detail);

        _factory.DetainedLicenseServiceMock
            .Setup(x => x.ReleaseAsync(It.IsAny<ReleaseDetainedLicenseDto>()))
            .ReturnsAsync(result);

        await AssertProblemDetailsAsync(
            await SendAsync(HttpMethod.Post, "/api/DetainedLicenses/release",
                new ReleaseDetainedLicenseRequest { DetainId = 500 }),
            status, title, detail);
    }

    private void SetupGetAll(Result<List<DetainedLicenseDto>> result) =>
        _factory.DetainedLicenseServiceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(result);

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, object? content = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Test-User-Id", "1");
        request.Headers.Add("X-Test-Username", "testuser");
        request.Headers.Add("X-Test-FullName", "Test User");
        request.Headers.Add("X-Test-Role", "Staff");
        if (content is not null) request.Content = JsonContent.Create(content);
        return await _client.SendAsync(request);
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response, HttpStatusCode status, string title, string detail)
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
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("instance").GetString()));
        Assert.True(body.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    private sealed record DetainedStatusResponse(bool Detained);
}
