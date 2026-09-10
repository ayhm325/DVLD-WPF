using System.Net;
using System.Net.Http.Json;
using Application.Common.Results;
using API.IntegrationTests.Infrastructure;
using DVLD.Contracts.LicenseIssuance;
using Moq;

namespace API.IntegrationTests.Controllers;

public sealed class LicenseIssuanceControllerTests
{
    [Fact]
    public async Task IssueFirstLicense_WithoutAuthentication_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var request =
            new IssueFirstLicenseRequest(
                LocalApplicationId: 10,
                Notes: "First license");

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseIssuance/first-license",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        factory.LicenseIssuanceServiceMock.Verify(
            x => x.IssueFirstLicenseAsync(
                It.IsAny<int>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueFirstLicense_WhenSuccessful_Returns200AndLicenseId()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseIssuanceServiceMock
            .Setup(x =>
                x.IssueFirstLicenseAsync(
                    10,
                    "First license notes"))
            .ReturnsAsync(
                Result<int>.Success(250));

        using var client =
            CreateAuthenticatedClient(factory);

        var request =
            new IssueFirstLicenseRequest(
                LocalApplicationId: 10,
                Notes: "First license notes");

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseIssuance/first-license",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<IssueFirstLicenseResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            250,
            result.LicenseId);

        factory.LicenseIssuanceServiceMock.Verify(
            x =>
                x.IssueFirstLicenseAsync(
                    10,
                    "First license notes"),
            Times.Once);
    }

    [Fact]
    public async Task IssueFirstLicense_WhenNotesAreNull_PassesNullToService()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseIssuanceServiceMock
            .Setup(x =>
                x.IssueFirstLicenseAsync(
                    10,
                    null))
            .ReturnsAsync(
                Result<int>.Success(251));

        using var client =
            CreateAuthenticatedClient(factory);

        var request =
            new IssueFirstLicenseRequest(
                LocalApplicationId: 10,
                Notes: null);

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseIssuance/first-license",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<IssueFirstLicenseResponse>();

        Assert.NotNull(result);
        Assert.Equal(251, result.LicenseId);

        factory.LicenseIssuanceServiceMock.Verify(
            x =>
                x.IssueFirstLicenseAsync(
                    10,
                    null),
            Times.Once);
    }

    [Fact]
    public async Task IssueFirstLicense_WhenValidationFails_Returns400()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseIssuanceServiceMock
            .Setup(x =>
                x.IssueFirstLicenseAsync(
                    0,
                    "Invalid application"))
            .ReturnsAsync(
                Result<int>.FromValidationFailure(
                    "Invalid local application ID."));

        using var client =
            CreateAuthenticatedClient(factory);

        var request =
            new IssueFirstLicenseRequest(
                LocalApplicationId: 0,
                Notes: "Invalid application");

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseIssuance/first-license",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "Invalid local application ID.",
            error);
    }

    [Fact]
    public async Task IssueFirstLicense_WhenNotFound_Returns404()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseIssuanceServiceMock
            .Setup(x =>
                x.IssueFirstLicenseAsync(
                    999,
                    null))
            .ReturnsAsync(
                Result<int>.FromNotFound(
                    "Local driving license application not found."));

        using var client =
            CreateAuthenticatedClient(factory);

        var request =
            new IssueFirstLicenseRequest(
                LocalApplicationId: 999,
                Notes: null);

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseIssuance/first-license",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "Local driving license application not found.",
            error);
    }

    [Fact]
    public async Task IssueFirstLicense_WhenConflict_Returns409()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseIssuanceServiceMock
            .Setup(x =>
                x.IssueFirstLicenseAsync(
                    20,
                    null))
            .ReturnsAsync(
                Result<int>.FromConflict(
                    "License has already been issued."));

        using var client =
            CreateAuthenticatedClient(factory);

        var request =
            new IssueFirstLicenseRequest(
                LocalApplicationId: 20,
                Notes: null);

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseIssuance/first-license",
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "License has already been issued.",
            error);
    }

    [Fact]
    public async Task IssueFirstLicense_WhenForbidden_Returns403()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseIssuanceServiceMock
            .Setup(x =>
                x.IssueFirstLicenseAsync(
                    20,
                    null))
            .ReturnsAsync(
                Result<int>.FromForbidden(
                    "Authenticated user is required."));

        using var client =
            CreateAuthenticatedClient(factory);

        var request =
            new IssueFirstLicenseRequest(
                LocalApplicationId: 20,
                Notes: null);

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseIssuance/first-license",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "Authenticated user is required.",
            error);
    }

    [Fact]
    public async Task IssueFirstLicense_WhenServiceFails_Returns500()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseIssuanceServiceMock
            .Setup(x =>
                x.IssueFirstLicenseAsync(
                    20,
                    null))
            .ReturnsAsync(
                Result<int>.FromFailure(
                    "Failed to issue driving license."));

        using var client =
            CreateAuthenticatedClient(factory);

        var request =
            new IssueFirstLicenseRequest(
                LocalApplicationId: 20,
                Notes: null);

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseIssuance/first-license",
                request);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "Failed to issue driving license.",
            error);
    }

    private static HttpClient CreateAuthenticatedClient(
        ApiWebApplicationFactory factory)
    {
        var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-User-Id",
            "1");

        return client;
    }

    private static async Task<string?> ReadErrorAsync(
        HttpResponseMessage response)
    {
        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        return body?.Error;
    }

    private sealed record ErrorResponse(string? Error);
}