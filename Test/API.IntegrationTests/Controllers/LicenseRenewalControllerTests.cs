using System.Net;
using System.Net.Http.Json;
using Application.Common.Results;
using API.IntegrationTests.Infrastructure;
using DVLD.Contracts.LicenseRenewal;
using Moq;

namespace API.IntegrationTests.Controllers;

public sealed class LicenseRenewalControllerTests
{
    [Fact]
    public async Task Renew_WithoutAuthentication_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var request =
            new RenewLicenseRequest(
                OldLicenseId: 10,
                Notes: "Renewal");

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseRenewal",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        factory.LicenseRenewalServiceMock.Verify(
            x => x.RenewLicenseAsync(
                It.IsAny<int>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task Renew_WhenSuccessful_Returns200AndLicenseId()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseRenewalServiceMock
            .Setup(x =>
                x.RenewLicenseAsync(
                    10,
                    "Renewal notes"))
            .ReturnsAsync(
                Result<int>.Success(300));

        using var client =
            CreateAuthenticatedClient(factory);

        var request =
            new RenewLicenseRequest(
                OldLicenseId: 10,
                Notes: "Renewal notes");

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseRenewal",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<RenewLicenseResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            300,
            result.LicenseId);

        factory.LicenseRenewalServiceMock.Verify(
            x =>
                x.RenewLicenseAsync(
                    10,
                    "Renewal notes"),
            Times.Once);
    }

    [Fact]
    public async Task Renew_WhenNotesAreNull_PassesNullToService()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseRenewalServiceMock
            .Setup(x =>
                x.RenewLicenseAsync(
                    10,
                    null))
            .ReturnsAsync(
                Result<int>.Success(301));

        using var client =
            CreateAuthenticatedClient(factory);

        var request =
            new RenewLicenseRequest(
                OldLicenseId: 10,
                Notes: null);

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseRenewal",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<RenewLicenseResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            301,
            result.LicenseId);

        factory.LicenseRenewalServiceMock.Verify(
            x =>
                x.RenewLicenseAsync(
                    10,
                    null),
            Times.Once);
    }

    [Fact]
    public async Task Renew_WhenValidationFails_Returns400()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseRenewalServiceMock
            .Setup(x =>
                x.RenewLicenseAsync(
                    0,
                    null))
            .ReturnsAsync(
                Result<int>.FromValidationFailure(
                    "Invalid license ID."));

        using var client =
            CreateAuthenticatedClient(factory);

        var request =
            new RenewLicenseRequest(
                OldLicenseId: 0,
                Notes: null);

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseRenewal",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "Invalid license ID.",
            error);
    }

    [Fact]
    public async Task Renew_WhenNotFound_Returns404()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseRenewalServiceMock
            .Setup(x =>
                x.RenewLicenseAsync(
                    999,
                    null))
            .ReturnsAsync(
                Result<int>.FromNotFound(
                    "License not found."));

        using var client =
            CreateAuthenticatedClient(factory);

        var request =
            new RenewLicenseRequest(
                OldLicenseId: 999,
                Notes: null);

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseRenewal",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "License not found.",
            error);
    }

    [Fact]
    public async Task Renew_WhenConflict_Returns409()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseRenewalServiceMock
            .Setup(x =>
                x.RenewLicenseAsync(
                    20,
                    null))
            .ReturnsAsync(
                Result<int>.FromConflict(
                    "License cannot be renewed."));

        using var client =
            CreateAuthenticatedClient(factory);

        var request =
            new RenewLicenseRequest(
                OldLicenseId: 20,
                Notes: null);

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseRenewal",
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "License cannot be renewed.",
            error);
    }

    [Fact]
    public async Task Renew_WhenForbidden_Returns403()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseRenewalServiceMock
            .Setup(x =>
                x.RenewLicenseAsync(
                    20,
                    null))
            .ReturnsAsync(
                Result<int>.FromForbidden(
                    "Authenticated user is required."));

        using var client =
            CreateAuthenticatedClient(factory);

        var request =
            new RenewLicenseRequest(
                OldLicenseId: 20,
                Notes: null);

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseRenewal",
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
    public async Task Renew_WhenServiceFails_Returns500()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseRenewalServiceMock
            .Setup(x =>
                x.RenewLicenseAsync(
                    20,
                    null))
            .ReturnsAsync(
                Result<int>.FromFailure(
                    "Failed to renew license."));

        using var client =
            CreateAuthenticatedClient(factory);

        var request =
            new RenewLicenseRequest(
                OldLicenseId: 20,
                Notes: null);

        var response =
            await client.PostAsJsonAsync(
                "/api/LicenseRenewal",
                request);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "Failed to renew license.",
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