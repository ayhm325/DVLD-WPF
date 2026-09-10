using System.Net;
using System.Net.Http.Json;
using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.Interfaces;
using DVLD.Contracts.LicenseReplacement;
using Moq;

namespace API.IntegrationTests.Controllers;

public sealed class LicenseReplacementControllerTests
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LicenseReplacementControllerTests(
        ApiWebApplicationFactory factory)
    {
        _factory = factory;

        _factory.LicenseReplacementServiceMock.Reset();

        _client = factory.CreateClient();
    }

    // =========================================================
    // AUTHENTICATION
    // =========================================================

    [Fact]
    public async Task Replace_WithoutAuthentication_ReturnsUnauthorized()
    {
        var request =
            new ReplaceLicenseRequest(
                OldLicenseId: 10,
                ReplacementReason: "Lost License");

        var response =
            await _client.PostAsJsonAsync(
                "/api/LicenseReplacement",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // =========================================================
    // SUCCESS - LOST LICENSE
    // =========================================================

    [Fact]
    public async Task Replace_WhenSuccessful_ReturnsNewLicenseId()
    {
        _factory.LicenseReplacementServiceMock
            .Setup(x =>
                x.ReplaceLicenseAsync(
                    10,
                    "Lost License"))
            .ReturnsAsync(
                Result<int>.Success(100));

        var request =
            new ReplaceLicenseRequest(
                OldLicenseId: 10,
                ReplacementReason: "Lost License");

        var response =
            await PostAuthenticatedAsync(
                "/api/LicenseReplacement",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<ReplaceLicenseResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            100,
            result.LicenseId);

        _factory.LicenseReplacementServiceMock.Verify(
            x =>
                x.ReplaceLicenseAsync(
                    10,
                    "Lost License"),
            Times.Once);
    }

    // =========================================================
    // VALIDATION FAILURE
    // =========================================================

    [Fact]
    public async Task Replace_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory.LicenseReplacementServiceMock
            .Setup(x =>
                x.ReplaceLicenseAsync(
                    10,
                    "Lost License"))
            .ReturnsAsync(
                Result<int>
                    .FromValidationFailure(
                        "Invalid replacement request."));

        var request =
            new ReplaceLicenseRequest(
                OldLicenseId: 10,
                ReplacementReason: "Lost License");

        var response =
            await PostAuthenticatedAsync(
                "/api/LicenseReplacement",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid replacement request.");

        _factory.LicenseReplacementServiceMock.Verify(
            x =>
                x.ReplaceLicenseAsync(
                    10,
                    "Lost License"),
            Times.Once);
    }

    // =========================================================
    // NOT FOUND
    // =========================================================

    [Fact]
    public async Task Replace_WhenLicenseNotFound_ReturnsNotFound()
    {
        _factory.LicenseReplacementServiceMock
            .Setup(x =>
                x.ReplaceLicenseAsync(
                    10,
                    "Lost License"))
            .ReturnsAsync(
                Result<int>
                    .FromNotFound(
                        "License not found."));

        var request =
            new ReplaceLicenseRequest(
                OldLicenseId: 10,
                ReplacementReason: "Lost License");

        var response =
            await PostAuthenticatedAsync(
                "/api/LicenseReplacement",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License not found.");

        _factory.LicenseReplacementServiceMock.Verify(
            x =>
                x.ReplaceLicenseAsync(
                    10,
                    "Lost License"),
            Times.Once);
    }

    // =========================================================
    // CONFLICT
    // =========================================================

    [Fact]
    public async Task Replace_WhenConflict_ReturnsConflict()
    {
        _factory.LicenseReplacementServiceMock
            .Setup(x =>
                x.ReplaceLicenseAsync(
                    10,
                    "Lost License"))
            .ReturnsAsync(
                Result<int>
                    .FromConflict(
                        "License replacement conflict."));

        var request =
            new ReplaceLicenseRequest(
                OldLicenseId: 10,
                ReplacementReason: "Lost License");

        var response =
            await PostAuthenticatedAsync(
                "/api/LicenseReplacement",
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "License replacement conflict.");

        _factory.LicenseReplacementServiceMock.Verify(
            x =>
                x.ReplaceLicenseAsync(
                    10,
                    "Lost License"),
            Times.Once);
    }

    // =========================================================
    // FORBIDDEN
    // =========================================================

    [Fact]
    public async Task Replace_WhenForbidden_ReturnsForbidden()
    {
        _factory.LicenseReplacementServiceMock
            .Setup(x =>
                x.ReplaceLicenseAsync(
                    10,
                    "Lost License"))
            .ReturnsAsync(
                Result<int>
                    .FromForbidden(
                        "Access denied."));

        var request =
            new ReplaceLicenseRequest(
                OldLicenseId: 10,
                ReplacementReason: "Lost License");

        var response =
            await PostAuthenticatedAsync(
                "/api/LicenseReplacement",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Access denied.");

        _factory.LicenseReplacementServiceMock.Verify(
            x =>
                x.ReplaceLicenseAsync(
                    10,
                    "Lost License"),
            Times.Once);
    }

    // =========================================================
    // UNEXPECTED FAILURE
    // =========================================================

    [Fact]
    public async Task Replace_WhenUnexpectedFailure_ReturnsInternalServerError()
    {
        _factory.LicenseReplacementServiceMock
            .Setup(x =>
                x.ReplaceLicenseAsync(
                    10,
                    "Lost License"))
            .ReturnsAsync(
                Result<int>
                    .FromFailure(
                        "Unexpected replacement failure."));

        var request =
            new ReplaceLicenseRequest(
                OldLicenseId: 10,
                ReplacementReason: "Lost License");

        var response =
            await PostAuthenticatedAsync(
                "/api/LicenseReplacement",
                request);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Unexpected replacement failure.");

        _factory.LicenseReplacementServiceMock.Verify(
            x =>
                x.ReplaceLicenseAsync(
                    10,
                    "Lost License"),
            Times.Once);
    }

    // =========================================================
    // SUCCESS - DAMAGED LICENSE
    // =========================================================

    [Fact]
    public async Task Replace_WithDamagedLicenseReason_PassesCorrectRequest()
    {
        _factory.LicenseReplacementServiceMock
            .Setup(x =>
                x.ReplaceLicenseAsync(
                    20,
                    "Damaged License"))
            .ReturnsAsync(
                Result<int>.Success(200));

        var request =
            new ReplaceLicenseRequest(
                OldLicenseId: 20,
                ReplacementReason: "Damaged License");

        var response =
            await PostAuthenticatedAsync(
                "/api/LicenseReplacement",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<ReplaceLicenseResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            200,
            result.LicenseId);

        _factory.LicenseReplacementServiceMock.Verify(
            x =>
                x.ReplaceLicenseAsync(
                    20,
                    "Damaged License"),
            Times.Once);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private async Task<HttpResponseMessage> PostAuthenticatedAsync(
        string url,
        object body)
    {
        var request =
            CreateAuthenticatedRequest(url);

        request.Content =
            JsonContent.Create(body);

        return await _client.SendAsync(request);
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        string url,
        string role = "Staff")
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
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