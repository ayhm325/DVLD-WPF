using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using DVLD.Contracts.LicenseRenewal;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.IntegrationTests.Controllers;

public sealed class LicenseRenewalControllerTests
{
    [Fact]
    public async Task Renew_WithoutAuthentication_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/LicenseRenewal",
            new RenewLicenseRequest(10, "Renewal"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        factory.LicenseRenewalServiceMock.Verify(
            x => x.RenewLicenseAsync(It.IsAny<int>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task Renew_WhenSuccessful_Returns200AndLicenseId()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseRenewalServiceMock
            .Setup(x => x.RenewLicenseAsync(10, "Renewal notes"))
            .ReturnsAsync(Result<int>.Success(300));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.PostAsJsonAsync(
            "/api/LicenseRenewal",
            new RenewLicenseRequest(10, "Renewal notes"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RenewLicenseResponse>();
        Assert.NotNull(result);
        Assert.Equal(300, result.LicenseId);

        factory.LicenseRenewalServiceMock.Verify(
            x => x.RenewLicenseAsync(10, "Renewal notes"),
            Times.Once);
    }

    [Fact]
    public async Task Renew_WhenNotesAreNull_PassesNullToService()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseRenewalServiceMock
            .Setup(x => x.RenewLicenseAsync(10, null))
            .ReturnsAsync(Result<int>.Success(301));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.PostAsJsonAsync(
            "/api/LicenseRenewal",
            new RenewLicenseRequest(10, null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RenewLicenseResponse>();
        Assert.NotNull(result);
        Assert.Equal(301, result.LicenseId);

        factory.LicenseRenewalServiceMock.Verify(
            x => x.RenewLicenseAsync(10, null),
            Times.Once);
    }

    [Theory]
    [InlineData(0, null, 400, "Validation error", "Invalid license ID.")]
    [InlineData(999, null, 404, "Resource not found", "License not found.")]
    [InlineData(20, null, 409, "Conflict", "License cannot be renewed.")]
    [InlineData(20, null, 403, "Forbidden", "Authenticated user is required.")]
    public async Task Renew_WhenResultFails_ReturnsProblemDetails(
        int licenseId,
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

        factory.LicenseRenewalServiceMock
            .Setup(x => x.RenewLicenseAsync(licenseId, notes))
            .ReturnsAsync(result);

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.PostAsJsonAsync(
            "/api/LicenseRenewal",
            new RenewLicenseRequest(licenseId, notes));

        await AssertProblemDetailsAsync(
            response,
            (HttpStatusCode)statusCode,
            title,
            detail);
    }

    [Fact]
    public async Task Renew_WhenServiceFails_Returns500ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseRenewalServiceMock
            .Setup(x => x.RenewLicenseAsync(20, null))
            .ReturnsAsync(Result<int>.FromFailure(
                "Failed to renew license."));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.PostAsJsonAsync(
            "/api/LicenseRenewal",
            new RenewLicenseRequest(20, null));

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
