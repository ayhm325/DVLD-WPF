using System.Net;
using System.Net.Http.Json;
using Application.Common.Results;
using Application.DTOs;
using API.IntegrationTests.Infrastructure;
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

        var response =
            await client.GetAsync("/api/LicenseClasses");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        factory.LicenseClassServiceMock.Verify(
            x => x.GetAllLicenseClassesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_Returns200AndMappedResponse()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetAllLicenseClassesAsync())
            .ReturnsAsync(
                Result<List<LicenseClassDto>>.Success(
                    new List<LicenseClassDto>
                    {
                        new()
                        {
                            LicenseClassID = 1,
                            LicenseClassName = "Small Motorcycle",
                            LicenseClassDescription =
                                "License for small motorcycles.",
                            MinAllowedAge = 18,
                            DefaultValidityLength = 10,
                            LicenseClassFees = 15m
                        },
                        new()
                        {
                            LicenseClassID = 2,
                            LicenseClassName = "Heavy Vehicle",
                            LicenseClassDescription =
                                "License for heavy vehicles.",
                            MinAllowedAge = 21,
                            DefaultValidityLength = 5,
                            LicenseClassFees = 50m
                        }
                    }));

        using var client =
            CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/LicenseClasses");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<LicenseClassResponse>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        Assert.Equal(1, result[0].LicenseClassId);
        Assert.Equal(
            "Small Motorcycle",
            result[0].LicenseClassName);
        Assert.Equal(
            "License for small motorcycles.",
            result[0].LicenseClassDescription);
        Assert.Equal(
            (byte)18,
            result[0].MinAllowedAge);
        Assert.Equal(
            (byte)10,
            result[0].DefaultValidityLength);
        Assert.Equal(
            15m,
            result[0].LicenseClassFees);

        Assert.Equal(2, result[1].LicenseClassId);
        Assert.Equal(
            "Heavy Vehicle",
            result[1].LicenseClassName);
        Assert.Equal(
            "License for heavy vehicles.",
            result[1].LicenseClassDescription);
        Assert.Equal(
            (byte)21,
            result[1].MinAllowedAge);
        Assert.Equal(
            (byte)5,
            result[1].DefaultValidityLength);
        Assert.Equal(
            50m,
            result[1].LicenseClassFees);

        factory.LicenseClassServiceMock.Verify(
            x => x.GetAllLicenseClassesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_Returns500()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetAllLicenseClassesAsync())
            .ReturnsAsync(
                Result<List<LicenseClassDto>>.FromFailure(
                    "Failed to load license classes."));

        using var client =
            CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/LicenseClasses");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "Failed to load license classes.",
            error);
    }

    [Fact]
    public async Task GetById_WhenSuccessful_Returns200AndMappedResponse()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(3))
            .ReturnsAsync(
                Result<LicenseClassDto>.Success(
                    new LicenseClassDto
                    {
                        LicenseClassID = 3,
                        LicenseClassName = "Private Car",
                        LicenseClassDescription =
                            "License for private cars.",
                        MinAllowedAge = 18,
                        DefaultValidityLength = 10,
                        LicenseClassFees = 25m
                    }));

        using var client =
            CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/LicenseClasses/3");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<LicenseClassResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            3,
            result.LicenseClassId);

        Assert.Equal(
            "Private Car",
            result.LicenseClassName);

        Assert.Equal(
            "License for private cars.",
            result.LicenseClassDescription);

        Assert.Equal(
            (byte)18,
            result.MinAllowedAge);

        Assert.Equal(
            (byte)10,
            result.DefaultValidityLength);

        Assert.Equal(
            25m,
            result.LicenseClassFees);

        factory.LicenseClassServiceMock.Verify(
            x => x.GetLicenseClassByIdAsync(3),
            Times.Once);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(99))
            .ReturnsAsync(
                Result<LicenseClassDto>.FromNotFound(
                    "License class not found."));

        using var client =
            CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/LicenseClasses/99");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "License class not found.",
            error);
    }

    [Fact]
    public async Task GetById_WhenValidationFails_Returns400()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(0))
            .ReturnsAsync(
                Result<LicenseClassDto>.FromValidationFailure(
                    "Invalid license class ID."));

        using var client =
            CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/LicenseClasses/0");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "Invalid license class ID.",
            error);
    }

    [Fact]
    public async Task GetById_WhenServiceFails_Returns500()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(5))
            .ReturnsAsync(
                Result<LicenseClassDto>.FromFailure(
                    "Failed to load license class."));

        using var client =
            CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/LicenseClasses/5");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "Failed to load license class.",
            error);
    }

    [Fact]
    public async Task GetById_WhenValueIsNull_Returns500WithSpecificError()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(5))
            .ReturnsAsync(
                Result<LicenseClassDto>.Success(null!));

        using var client =
            CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/LicenseClasses/5");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "License class data is unavailable.",
            error);

        factory.LicenseClassServiceMock.Verify(
            x => x.GetLicenseClassByIdAsync(5),
            Times.Once);
    }

    [Fact]
    public async Task GetById_WhenConflictFailure_Returns500()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(5))
            .ReturnsAsync(
                Result<LicenseClassDto>.FromConflict(
                    "License class conflict."));

        using var client =
            CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/LicenseClasses/5");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "License class conflict.",
            error);
    }

    [Fact]
    public async Task GetById_WhenForbiddenFailure_Returns500()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.LicenseClassServiceMock
            .Setup(x => x.GetLicenseClassByIdAsync(5))
            .ReturnsAsync(
                Result<LicenseClassDto>.FromForbidden(
                    "Access denied."));

        using var client =
            CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/LicenseClasses/5");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "Access denied.",
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