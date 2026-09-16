using System.Net;
using System.Net.Http.Json;
using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs;
using DVLD.Contracts.LicenseClass;
using Moq;

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
        factory.LicenseClassServiceMock.Verify(
            x => x.GetAllLicenseClassesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_Returns200AndMappedResponse()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetAllLicenseClassesAsync())
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

        var result = await response.Content
            .ReadFromJsonAsync<List<LicenseClassResponse>>();

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
            x => x.GetAllLicenseClassesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_Returns500()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetAllLicenseClassesAsync())
            .ReturnsAsync(Result<List<LicenseClassDto>>.FromFailure(
                "Failed to load license classes."));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync("/api/LicenseClasses");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("Failed to load license classes.", await ReadErrorAsync(response));
    }

    [Fact]
    public async Task GetById_WhenSuccessful_Returns200AndMappedResponse()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(3))
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

        var result = await response.Content
            .ReadFromJsonAsync<LicenseClassResponse>();

        Assert.NotNull(result);
        Assert.Equal(3, result!.LicenseClassId);
        Assert.Equal("Private Car", result.LicenseClassName);
        Assert.Equal("License for private cars.", result.LicenseClassDescription);
        Assert.Equal((byte)18, result.MinAllowedAge);
        Assert.Equal((byte)10, result.DefaultValidityLength);
        Assert.Equal(25m, result.LicenseClassFees);

        factory.LicenseClassServiceMock.Verify(
            x => x.GetLicenseClassByIdAsync(3), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(99))
            .ReturnsAsync(Result<LicenseClassDto>.FromNotFound(
                "License class not found."));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync("/api/LicenseClasses/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("License class not found.", await ReadErrorAsync(response));
    }

    [Fact]
    public async Task GetById_WhenValidationFails_Returns400()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(0))
            .ReturnsAsync(Result<LicenseClassDto>.FromValidationFailure(
                "Invalid license class ID."));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync("/api/LicenseClasses/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Invalid license class ID.", await ReadErrorAsync(response));
    }

    [Fact]
    public async Task GetById_WhenServiceFails_Returns500()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(5))
            .ReturnsAsync(Result<LicenseClassDto>.FromFailure(
                "Failed to load license class."));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync("/api/LicenseClasses/5");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("Failed to load license class.", await ReadErrorAsync(response));
    }

    [Fact]
    public async Task GetById_WhenValueIsNull_Returns500WithSpecificError()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(5))
            .ReturnsAsync(Result<LicenseClassDto>.Success(null!));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync("/api/LicenseClasses/5");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("License class data is unavailable.", await ReadErrorAsync(response));

        factory.LicenseClassServiceMock.Verify(
            x => x.GetLicenseClassByIdAsync(5), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenConflictFailure_Returns500()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(5))
            .ReturnsAsync(Result<LicenseClassDto>.FromConflict(
                "License class conflict."));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync("/api/LicenseClasses/5");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("License class conflict.", await ReadErrorAsync(response));
    }

    [Fact]
    public async Task GetById_WhenForbiddenFailure_ReturnsForbidden()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(5))
            .ReturnsAsync(Result<LicenseClassDto>.FromForbidden(
                "Access denied."));

        using var client = CreateAuthenticatedClient(factory);
        var response = await client.GetAsync("/api/LicenseClasses/5");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Access denied.", await ReadErrorAsync(response));
    }

    private static HttpClient CreateAuthenticatedClient(ApiWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", "1");
        return client;
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        return body?.Error;
    }

    private sealed record ErrorResponse(string? Error);
}