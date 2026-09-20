using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using DVLD.Contracts.LicenseReplacement;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.IntegrationTests.Controllers;

public sealed class LicenseReplacementControllerTests
{
    [Fact]
    public async Task Replace_WithoutAuthentication_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/LicenseReplacement",
            new ReplaceLicenseRequest(10, "Lost License"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        factory.LicenseReplacementServiceMock.Verify(
            x => x.ReplaceLicenseAsync(It.IsAny<int>(), It.IsAny<string>()),
            Times.Never);
    }

    [Theory]
    [InlineData(10, "Lost License", 100)]
    [InlineData(20, "Damaged License", 200)]
    public async Task Replace_WhenSuccessful_ReturnsLicenseId(
        int licenseId,
        string reason,
        int expectedLicenseId)
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseReplacementServiceMock
            .Setup(x => x.ReplaceLicenseAsync(licenseId, reason))
            .ReturnsAsync(Result<int>.Success(expectedLicenseId));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.PostAsJsonAsync(
            "/api/LicenseReplacement",
            new ReplaceLicenseRequest(licenseId, reason));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ReplaceLicenseResponse>();
        Assert.NotNull(result);
        Assert.Equal(expectedLicenseId, result.LicenseId);

        factory.LicenseReplacementServiceMock.Verify(
            x => x.ReplaceLicenseAsync(licenseId, reason),
            Times.Once);
    }

    [Theory]
    [InlineData(400, "Validation error", "Invalid replacement request.")]
    [InlineData(404, "Resource not found", "License not found.")]
    [InlineData(409, "Conflict", "License replacement conflict.")]
    [InlineData(403, "Forbidden", "Access denied.")]
    public async Task Replace_WhenResultFails_ReturnsProblemDetails(
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

        factory.LicenseReplacementServiceMock
            .Setup(x => x.ReplaceLicenseAsync(10, "Lost License"))
            .ReturnsAsync(result);

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.PostAsJsonAsync(
            "/api/LicenseReplacement",
            new ReplaceLicenseRequest(10, "Lost License"));

        await AssertProblemDetailsAsync(
            response,
            (HttpStatusCode)statusCode,
            title,
            detail);

        factory.LicenseReplacementServiceMock.Verify(
            x => x.ReplaceLicenseAsync(10, "Lost License"),
            Times.Once);
    }

    [Fact]
    public async Task Replace_WhenUnexpectedFailure_Returns500ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseReplacementServiceMock
            .Setup(x => x.ReplaceLicenseAsync(10, "Lost License"))
            .ReturnsAsync(Result<int>.FromFailure(
                "Unexpected replacement failure."));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.PostAsJsonAsync(
            "/api/LicenseReplacement",
            new ReplaceLicenseRequest(10, "Lost License"));

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");

        factory.LicenseReplacementServiceMock.Verify(
            x => x.ReplaceLicenseAsync(10, "Lost License"),
            Times.Once);
    }

    private static HttpClient CreateAuthenticatedClient(ApiWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", "1");
        client.DefaultRequestHeaders.Add("X-Test-Username", "testuser");
        client.DefaultRequestHeaders.Add("X-Test-FullName", "Test User");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Staff");
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

        Assert.Equal((int)expectedStatus, body.GetProperty("status").GetInt32());
        Assert.Equal(expectedTitle, body.GetProperty("title").GetString());
        Assert.Equal(expectedDetail, body.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            body.GetProperty("instance").GetString()));

        Assert.True(body.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}
