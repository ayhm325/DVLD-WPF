using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs;
using DVLD.Contracts.LicenseClass;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.IntegrationTests.Controllers;

public sealed class LicenseClassesControllerTests
{
    [Fact]
    public async Task GetAll_WithoutAuthentication_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/LicenseClasses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        factory.LicenseClassServiceMock.Verify(x => x.GetAllLicenseClassesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_Returns200AndMappedResponse()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetAllLicenseClassesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<LicenseClassDto>>.Success(
            [
                new()
                {
                    LicenseClassID = 1,
                    LicenseClassName = "Small Motorcycle",
                    LicenseClassDescription = "License for small motorcycles.",
                    MinAllowedAge = 18,
                    DefaultValidityLength = 10,
                    LicenseClassFees = 15m
                },
                new()
                {
                    LicenseClassID = 2,
                    LicenseClassName = "Heavy Vehicle",
                    LicenseClassDescription = "License for heavy vehicles.",
                    MinAllowedAge = 21,
                    DefaultValidityLength = 5,
                    LicenseClassFees = 50m
                }
            ]));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync("/api/LicenseClasses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<List<LicenseClassResponse>>();

        Assert.NotNull(result);

        Assert.Collection(result,
            item =>
            {
                Assert.Equal(1, item.LicenseClassId);
                Assert.Equal("Small Motorcycle", item.LicenseClassName);
                Assert.Equal("License for small motorcycles.", item.LicenseClassDescription);
                Assert.Equal((byte)18, item.MinAllowedAge);
                Assert.Equal((byte)10, item.DefaultValidityLength);
                Assert.Equal(15m, item.LicenseClassFees);
            },
            item =>
            {
                Assert.Equal(2, item.LicenseClassId);
                Assert.Equal("Heavy Vehicle", item.LicenseClassName);
                Assert.Equal("License for heavy vehicles.", item.LicenseClassDescription);
                Assert.Equal((byte)21, item.MinAllowedAge);
                Assert.Equal((byte)5, item.DefaultValidityLength);
                Assert.Equal(50m, item.LicenseClassFees);
            });

        factory.LicenseClassServiceMock.Verify(
            x => x.GetAllLicenseClassesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_Returns500ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetAllLicenseClassesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<LicenseClassDto>>.FromFailure(
                "Failed to load license classes."));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync("/api/LicenseClasses");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetById_WhenSuccessful_Returns200AndMappedResponse()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LicenseClassDto>.Success(new()
            {
                LicenseClassID = 3,
                LicenseClassName = "Private Car",
                LicenseClassDescription = "License for private cars.",
                MinAllowedAge = 18,
                DefaultValidityLength = 10,
                LicenseClassFees = 25m
            }));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync("/api/LicenseClasses/3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<LicenseClassResponse>();

        Assert.NotNull(result);
        Assert.Equal(3, result.LicenseClassId);
        Assert.Equal("Private Car", result.LicenseClassName);
        Assert.Equal("License for private cars.", result.LicenseClassDescription);
        Assert.Equal((byte)18, result.MinAllowedAge);
        Assert.Equal((byte)10, result.DefaultValidityLength);
        Assert.Equal(25m, result.LicenseClassFees);

        factory.LicenseClassServiceMock.Verify(
            x => x.GetLicenseClassByIdAsync(3, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(
        99,
        "License class not found.",
        404,
        "Resource not found")]
    [InlineData(
        0,
        "Invalid license class ID.",
        400,
        "Validation error")]
    public async Task GetById_WhenRequestFails_ReturnsProblemDetails(
        int id,
        string detail,
        int statusCode,
        string title)
    {
        await using var factory = new ApiWebApplicationFactory();

        var result = id == 99
            ? Result<LicenseClassDto>.FromNotFound(detail)
            : Result<LicenseClassDto>.FromValidationFailure(detail);

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync($"/api/LicenseClasses/{id}");

        await AssertProblemDetailsAsync(
            response,
            (HttpStatusCode)statusCode,
            title,
            detail);
    }

    [Fact]
    public async Task GetById_WhenServiceFails_Returns500ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LicenseClassDto>.FromFailure(
                "Failed to load license class."));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync("/api/LicenseClasses/5");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetById_WhenValueIsNull_Returns500ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LicenseClassDto>.Success(null!));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync("/api/LicenseClasses/5");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");

        factory.LicenseClassServiceMock.Verify(
            x => x.GetLicenseClassByIdAsync(5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetById_WhenConflictFailure_Returns409ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LicenseClassDto>.FromConflict(
                "License class conflict."));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync("/api/LicenseClasses/5");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict",
            "License class conflict.");
    }

    [Fact]
    public async Task GetById_WhenForbiddenFailure_Returns403ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LicenseClassDto>.FromForbidden(
                "Access denied."));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync("/api/LicenseClasses/5");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Forbidden,
            "Forbidden",
            "Access denied.");
    }

    [Fact]
    public async Task Update_WhenAnonymous_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/api/LicenseClasses/1",
            ValidUpdateRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        factory.LicenseClassServiceMock.Verify(
            x => x.UpdateLicenseClassAsync(It.IsAny<int>(), It.IsAny<LicenseClassDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_WhenStaff_Returns403()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory, role: "Staff");

        var response = await client.PutAsJsonAsync(
            "/api/LicenseClasses/1",
            ValidUpdateRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        factory.LicenseClassServiceMock.Verify(
            x => x.UpdateLicenseClassAsync(It.IsAny<int>(), It.IsAny<LicenseClassDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_WhenSuccessful_Returns204()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.UpdateLicenseClassAsync(
                1,
                It.Is<LicenseClassDto>(d =>
                    d.LicenseClassID == 1 &&
                    d.LicenseClassName == "Class 1 - Small Motorcycle" &&
                    d.LicenseClassDescription == "Motorcycle up to 125cc" &&
                    d.MinAllowedAge == 18 &&
                    d.DefaultValidityLength == 10 &&
                    d.LicenseClassFees == 20m),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        using var client = CreateAuthenticatedClient(factory, role: "Admin");

        var response = await client.PutAsJsonAsync(
            "/api/LicenseClasses/1",
            ValidUpdateRequest());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        factory.LicenseClassServiceMock.Verify(
            x => x.UpdateLicenseClassAsync(
                1,
                It.Is<LicenseClassDto>(d =>
                    d.LicenseClassID == 1 &&
                    d.LicenseClassName == "Class 1 - Small Motorcycle" &&
                    d.LicenseClassDescription == "Motorcycle up to 125cc" &&
                    d.MinAllowedAge == 18 &&
                    d.DefaultValidityLength == 10 &&
                    d.LicenseClassFees == 20m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Update_WhenNotFound_Returns404ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.UpdateLicenseClassAsync(99, It.IsAny<LicenseClassDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.NotFound("License class not found."));

        using var client = CreateAuthenticatedClient(factory, role: "Admin");

        var response = await client.PutAsJsonAsync(
            "/api/LicenseClasses/99",
            ValidUpdateRequest());

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found",
            "License class not found.");
    }

    [Fact]
    public async Task Update_WhenValidationFails_Returns400ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.UpdateLicenseClassAsync(1, It.IsAny<LicenseClassDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.ValidationFailure("License class name is required."));

        using var client = CreateAuthenticatedClient(factory, role: "Admin");

        var response = await client.PutAsJsonAsync(
            "/api/LicenseClasses/1",
            ValidUpdateRequest());

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "Validation error",
            "License class name is required.");
    }

    [Fact]
    public async Task Update_WhenServiceFails_Returns500ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.UpdateLicenseClassAsync(1, It.IsAny<LicenseClassDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Database write failed."));

        using var client = CreateAuthenticatedClient(factory, role: "Admin");

        var response = await client.PutAsJsonAsync(
            "/api/LicenseClasses/1",
            ValidUpdateRequest());

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    private static UpdateLicenseClassRequest ValidUpdateRequest() => new()
    {
        LicenseClassId = 1,
        LicenseClassName = "Class 1 - Small Motorcycle",
        LicenseClassDescription = "Motorcycle up to 125cc",
        MinAllowedAge = 18,
        DefaultValidityLength = 10,
        LicenseClassFees = 20m
    };

    private static HttpClient CreateAuthenticatedClient(
        ApiWebApplicationFactory factory,
        string role = "Staff")
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", "1");
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }

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

        Assert.Equal(
            (int)expectedStatus,
            body.GetProperty("status").GetInt32());

        Assert.Equal(
            expectedTitle,
            body.GetProperty("title").GetString());

        Assert.Equal(
            expectedDetail,
            body.GetProperty("detail").GetString());

        Assert.False(
            string.IsNullOrWhiteSpace(
                body.GetProperty("instance").GetString()));

        Assert.True(
            body.TryGetProperty("traceId", out var traceId));

        Assert.False(
            string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}
