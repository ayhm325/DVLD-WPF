using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using DVLD.Contracts.LicenseIssuance;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;


namespace API.IntegrationTests.Controllers;

public sealed class LicenseIssuanceControllerTests
{
    [Fact]
    public async Task IssueFirstLicense_WithoutAuthentication_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new IssueFirstLicenseRequest(10, "First license");
        var response = await client.PostAsJsonAsync("/api/LicenseIssuance/first-license", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        factory.LicenseIssuanceServiceMock.Verify(
            x => x.IssueFirstLicenseAsync(It.IsAny<int>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueFirstLicense_WhenSuccessful_Returns200AndLicenseId()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseIssuanceServiceMock
            .Setup(x => x.IssueFirstLicenseAsync(10, "First license notes"))
            .ReturnsAsync(Result<int>.Success(250));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.PostAsJsonAsync(
            "/api/LicenseIssuance/first-license",
            new IssueFirstLicenseRequest(10, "First license notes"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<IssueFirstLicenseResponse>();
        Assert.NotNull(result);
        Assert.Equal(250, result.LicenseId);

        factory.LicenseIssuanceServiceMock.Verify(
            x => x.IssueFirstLicenseAsync(10, "First license notes"),
            Times.Once);
    }

    [Fact]
    public async Task IssueFirstLicense_WhenNotesAreNull_PassesNullToService()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseIssuanceServiceMock
            .Setup(x => x.IssueFirstLicenseAsync(10, null))
            .ReturnsAsync(Result<int>.Success(251));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.PostAsJsonAsync(
            "/api/LicenseIssuance/first-license",
            new IssueFirstLicenseRequest(10, null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<IssueFirstLicenseResponse>();
        Assert.NotNull(result);
        Assert.Equal(251, result.LicenseId);

        factory.LicenseIssuanceServiceMock.Verify(
            x => x.IssueFirstLicenseAsync(10, null),
            Times.Once);
    }

    [Theory]
    [InlineData(
        0,
        "Invalid application",
        400,
        "Validation error",
        "Invalid local application ID.")]
    [InlineData(
        999,
        null,
        404,
        "Resource not found",
        "Local driving license application not found.")]
    [InlineData(
        20,
        null,
        409,
        "Conflict",
        "License has already been issued.")]
    [InlineData(
        20,
        null,
        403,
        "Forbidden",
        "Authenticated user is required.")]
    public async Task IssueFirstLicense_WhenResultFails_ReturnsProblemDetails(
        int applicationId,
        string? notes,
        int statusCode,
        string title,
        string detail)
    {
        await using var factory = new ApiWebApplicationFactory();

        var result = statusCode switch
        {
            400 => Result<int>.FromValidationFailure(detail),
            404 => Result<int>.FromNotFound(detail),
            409 => Result<int>.FromConflict(detail),
            403 => Result<int>.FromForbidden(detail),
            _ => throw new ArgumentOutOfRangeException(nameof(statusCode))
        };

        factory.LicenseIssuanceServiceMock
            .Setup(x => x.IssueFirstLicenseAsync(applicationId, notes))
            .ReturnsAsync(result);

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.PostAsJsonAsync(
            "/api/LicenseIssuance/first-license",
            new IssueFirstLicenseRequest(applicationId, notes));

        await AssertProblemDetailsAsync(
            response,
            (HttpStatusCode)statusCode,
            title,
            detail);
    }

    [Fact]
    public async Task IssueFirstLicense_WhenServiceFails_Returns500ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseIssuanceServiceMock
            .Setup(x => x.IssueFirstLicenseAsync(20, null))
            .ReturnsAsync(Result<int>.FromFailure(
                "Failed to issue driving license."));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.PostAsJsonAsync(
            "/api/LicenseIssuance/first-license",
            new IssueFirstLicenseRequest(20, null));

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    private static HttpClient CreateAuthenticatedClient(ApiWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", "1");
        return client;
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedTitle,
        string expectedDetail)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = document.RootElement;

        Assert.Equal((int)expectedStatus, body.GetProperty("status").GetInt32());
        Assert.Equal(expectedTitle, body.GetProperty("title").GetString());
        Assert.Equal(expectedDetail, body.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("instance").GetString()));
        Assert.True(body.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}
