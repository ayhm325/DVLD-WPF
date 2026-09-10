using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs;
using DVLD.Contracts.ApplicationType;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Controllers;

public sealed class ApplicationTypesControllerTests
{
    [Fact]
    public async Task GetAll_WithoutAuthentication_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/ApplicationTypes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        factory.ApplicationTypeServiceMock.Verify(
            x => x.GetAllApplicationTypesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_Returns200AndMappedResponse()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.GetAllApplicationTypesAsync())
            .ReturnsAsync(
                Result<List<ApplicationTypeDto>>.Success(
                    new List<ApplicationTypeDto>
                    {
                        new()
                        {
                            ApplicationTypeId = 1,
                            ApplicationTypeTitle = "New Local Driving License Service",
                            ApplicationTypeFees = 20.50m
                        },
                        new()
                        {
                            ApplicationTypeId = 2,
                            ApplicationTypeTitle = "Renew Driving License",
                            ApplicationTypeFees = 15.75m
                        }
                    }));

        using var client = CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/ApplicationTypes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<ApplicationTypeResponse>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        Assert.Equal(1, result[0].ApplicationTypeId);
        Assert.Equal(
            "New Local Driving License Service",
            result[0].ApplicationTypeTitle);
        Assert.Equal(20.50m, result[0].ApplicationTypeFees);

        Assert.Equal(2, result[1].ApplicationTypeId);
        Assert.Equal(
            "Renew Driving License",
            result[1].ApplicationTypeTitle);
        Assert.Equal(15.75m, result[1].ApplicationTypeFees);

        factory.ApplicationTypeServiceMock.Verify(
            x => x.GetAllApplicationTypesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_Returns400()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.GetAllApplicationTypesAsync())
            .ReturnsAsync(
                Result<List<ApplicationTypeDto>>.FromFailure(
                    "Failed to load application types."));

        using var client = CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/ApplicationTypes");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "Failed to load application types.",
            error);
    }

    [Fact]
    public async Task GetById_WhenSuccessful_Returns200AndMappedResponse()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.GetApplicationTypeByIdAsync(3))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 3,
                        ApplicationTypeTitle = "Replace Lost License",
                        ApplicationTypeFees = 25m
                    }));

        using var client = CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/ApplicationTypes/3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<ApplicationTypeResponse>();

        Assert.NotNull(result);
        Assert.Equal(3, result.ApplicationTypeId);
        Assert.Equal(
            "Replace Lost License",
            result.ApplicationTypeTitle);
        Assert.Equal(25m, result.ApplicationTypeFees);

        factory.ApplicationTypeServiceMock.Verify(
            x => x.GetApplicationTypeByIdAsync(3),
            Times.Once);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.GetApplicationTypeByIdAsync(99))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.FromNotFound(
                    "Application type not found."));

        using var client = CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/ApplicationTypes/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "Application type not found.",
            error);
    }

    [Fact]
    public async Task GetById_WhenValidationFails_Returns400()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.GetApplicationTypeByIdAsync(0))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.FromValidationFailure(
                    "Invalid application type ID."));

        using var client = CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/ApplicationTypes/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "Invalid application type ID.",
            error);
    }

    [Fact]
    public async Task GetById_WhenServiceFails_Returns500()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.FromFailure(
                    "Unexpected application type failure."));

        using var client = CreateAuthenticatedClient(factory);

        var response =
            await client.GetAsync("/api/ApplicationTypes/5");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "Unexpected application type failure.",
            error);
    }

    [Fact]
    public async Task Update_WhenSuccessful_Returns204AndMapsRequestToDto()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x =>
                x.UpdateApplicationTypeAsync(
                    4,
                    It.Is<ApplicationTypeDto>(dto =>
                        dto.ApplicationTypeId == 4 &&
                        dto.ApplicationTypeTitle == "Updated Type" &&
                        dto.ApplicationTypeFees == 30m)))
            .ReturnsAsync(Result.Success());

        using var client = CreateAuthenticatedClient(factory);

        var request = new UpdateApplicationTypeRequest
        {
            ApplicationTypeId = 999,
            ApplicationTypeTitle = "Updated Type",
            ApplicationTypeFees = 30m
        };

        var response =
            await client.PutAsJsonAsync(
                "/api/ApplicationTypes/4",
                request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        factory.ApplicationTypeServiceMock.Verify(
            x =>
                x.UpdateApplicationTypeAsync(
                    4,
                    It.Is<ApplicationTypeDto>(dto =>
                        dto.ApplicationTypeId == 4 &&
                        dto.ApplicationTypeTitle == "Updated Type" &&
                        dto.ApplicationTypeFees == 30m)),
            Times.Once);
    }

    [Fact]
    public async Task Update_WhenValidationFails_Returns400()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x =>
                x.UpdateApplicationTypeAsync(
                    4,
                    It.IsAny<ApplicationTypeDto>()))
            .ReturnsAsync(
                Result.ValidationFailure(
                    "Invalid application type data."));

        using var client = CreateAuthenticatedClient(factory);

        var request = new UpdateApplicationTypeRequest
        {
            ApplicationTypeId = 4,
            ApplicationTypeTitle = "",
            ApplicationTypeFees = 0m
        };

        var response =
            await client.PutAsJsonAsync(
                "/api/ApplicationTypes/4",
                request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "Invalid application type data.",
            error);
    }

    [Fact]
    public async Task Update_WhenNotFound_Returns404()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x =>
                x.UpdateApplicationTypeAsync(
                    10,
                    It.IsAny<ApplicationTypeDto>()))
            .ReturnsAsync(
                Result.NotFound(
                    "Application type not found."));

        using var client = CreateAuthenticatedClient(factory);

        var request = new UpdateApplicationTypeRequest
        {
            ApplicationTypeId = 10,
            ApplicationTypeTitle = "Updated Type",
            ApplicationTypeFees = 20m
        };

        var response =
            await client.PutAsJsonAsync(
                "/api/ApplicationTypes/10",
                request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "Application type not found.",
            error);
    }

    [Fact]
    public async Task Update_WhenConflict_Returns409()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x =>
                x.UpdateApplicationTypeAsync(
                    10,
                    It.IsAny<ApplicationTypeDto>()))
            .ReturnsAsync(
                Result.Conflict(
                    "Application type update conflicts with existing data."));

        using var client = CreateAuthenticatedClient(factory);

        var request = new UpdateApplicationTypeRequest
        {
            ApplicationTypeId = 10,
            ApplicationTypeTitle = "Conflicting Type",
            ApplicationTypeFees = 20m
        };

        var response =
            await client.PutAsJsonAsync(
                "/api/ApplicationTypes/10",
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "Application type update conflicts with existing data.",
            error);
    }

    [Fact]
    public async Task Update_WhenForbidden_Returns403()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x =>
                x.UpdateApplicationTypeAsync(
                    10,
                    It.IsAny<ApplicationTypeDto>()))
            .ReturnsAsync(
                Result.Forbidden(
                    "Authenticated user is required."));

        using var client = CreateAuthenticatedClient(factory);

        var request = new UpdateApplicationTypeRequest
        {
            ApplicationTypeId = 10,
            ApplicationTypeTitle = "Updated Type",
            ApplicationTypeFees = 20m
        };

        var response =
            await client.PutAsJsonAsync(
                "/api/ApplicationTypes/10",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenServiceFails_Returns500()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x =>
                x.UpdateApplicationTypeAsync(
                    10,
                    It.IsAny<ApplicationTypeDto>()))
            .ReturnsAsync(
                Result.Failure(
                    "Failed to save application type changes."));

        using var client = CreateAuthenticatedClient(factory);

        var request = new UpdateApplicationTypeRequest
        {
            ApplicationTypeId = 10,
            ApplicationTypeTitle = "Updated Type",
            ApplicationTypeFees = 20m
        };

        var response =
            await client.PutAsJsonAsync(
                "/api/ApplicationTypes/10",
                request);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "Failed to save application type changes.",
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