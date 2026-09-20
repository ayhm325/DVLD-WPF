using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs;
using DVLD.Contracts.ApplicationType;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.IntegrationTests.Controllers;

public sealed class ApplicationTypesControllerTests
{
    [Fact]
    public async Task GetAll_WhenAnonymous_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();

        var response = await SendAsync(factory, HttpMethod.Get, "/api/ApplicationTypes", role: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        VerifyGetNever(factory);
    }

    [Fact]
    public async Task GetAll_WhenStaff_Returns403()
    {
        await using var factory = new ApiWebApplicationFactory();

        var response = await SendAsync(factory, HttpMethod.Get, "/api/ApplicationTypes", role: "Staff");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        VerifyGetNever(factory);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedResponse()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.GetAllApplicationTypesAsync())
            .ReturnsAsync(Result<List<ApplicationTypeDto>>.Success(
            [
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
            ]));

        var response = await SendAsync(factory, HttpMethod.Get, "/api/ApplicationTypes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<ApplicationTypeResponse>>();

        Assert.NotNull(result);
        Assert.Collection(result,
            x =>
            {
                Assert.Equal(1, x.ApplicationTypeId);
                Assert.Equal("New Local Driving License Service", x.ApplicationTypeTitle);
                Assert.Equal(20.50m, x.ApplicationTypeFees);
            },
            x =>
            {
                Assert.Equal(2, x.ApplicationTypeId);
                Assert.Equal("Renew Driving License", x.ApplicationTypeTitle);
                Assert.Equal(15.75m, x.ApplicationTypeFees);
            });

        factory.ApplicationTypeServiceMock.Verify(
            x => x.GetAllApplicationTypesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_Returns500()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.GetAllApplicationTypesAsync())
            .ReturnsAsync(Result<List<ApplicationTypeDto>>.FromFailure(
                "Failed to load application types."));

        await AssertProblemDetailsAsync(
            await SendAsync(factory, HttpMethod.Get, "/api/ApplicationTypes"),
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedResponse()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.GetApplicationTypeByIdAsync(3))
            .ReturnsAsync(Result<ApplicationTypeDto>.Success(new()
            {
                ApplicationTypeId = 3,
                ApplicationTypeTitle = "Replace Lost License",
                ApplicationTypeFees = 25m
            }));

        var response = await SendAsync(factory, HttpMethod.Get, "/api/ApplicationTypes/3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApplicationTypeResponse>();

        Assert.NotNull(result);
        Assert.Equal(3, result.ApplicationTypeId);
        Assert.Equal("Replace Lost License", result.ApplicationTypeTitle);
        Assert.Equal(25m, result.ApplicationTypeFees);

        factory.ApplicationTypeServiceMock.Verify(
            x => x.GetApplicationTypeByIdAsync(3), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.GetApplicationTypeByIdAsync(99))
            .ReturnsAsync(Result<ApplicationTypeDto>.FromNotFound(
                "Application type not found."));

        await AssertProblemDetailsAsync(
            await SendAsync(factory, HttpMethod.Get, "/api/ApplicationTypes/99"),
            HttpStatusCode.NotFound,
            "Resource not found",
            "Application type not found.");
    }

    [Fact]
    public async Task GetById_WhenValidationFails_Returns400()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.GetApplicationTypeByIdAsync(0))
            .ReturnsAsync(Result<ApplicationTypeDto>.FromValidationFailure(
                "Invalid application type ID."));

        await AssertProblemDetailsAsync(
            await SendAsync(factory, HttpMethod.Get, "/api/ApplicationTypes/0"),
            HttpStatusCode.BadRequest,
            "Validation error",
            "Invalid application type ID.");
    }

    [Fact]
    public async Task GetById_WhenServiceFails_Returns500()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(Result<ApplicationTypeDto>.FromFailure(
                "Unexpected application type failure."));

        await AssertProblemDetailsAsync(
            await SendAsync(factory, HttpMethod.Get, "/api/ApplicationTypes/5"),
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task Update_WhenAnonymous_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();

        var response = await SendAsync(
            factory, HttpMethod.Put, "/api/ApplicationTypes/10", ValidRequest(), null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        VerifyUpdateNever(factory);
    }

    [Fact]
    public async Task Update_WhenStaff_Returns403()
    {
        await using var factory = new ApiWebApplicationFactory();

        var response = await SendAsync(
            factory, HttpMethod.Put, "/api/ApplicationTypes/10", ValidRequest(), "Staff");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        VerifyUpdateNever(factory);
    }

    [Fact]
    public async Task Update_WhenSuccessful_Returns204AndMapsRequestToDto()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.UpdateApplicationTypeAsync(
                4,
                It.Is<ApplicationTypeDto>(d =>
                    d.ApplicationTypeId == 4 &&
                    d.ApplicationTypeTitle == "Updated Type" &&
                    d.ApplicationTypeFees == 30m)))
            .ReturnsAsync(Result.Success());

        var response = await SendAsync(
            factory,
            HttpMethod.Put,
            "/api/ApplicationTypes/4",
            new UpdateApplicationTypeRequest
            {
                ApplicationTypeId = 999,
                ApplicationTypeTitle = "Updated Type",
                ApplicationTypeFees = 30m
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        factory.ApplicationTypeServiceMock.Verify(
            x => x.UpdateApplicationTypeAsync(
                4,
                It.Is<ApplicationTypeDto>(d =>
                    d.ApplicationTypeId == 4 &&
                    d.ApplicationTypeTitle == "Updated Type" &&
                    d.ApplicationTypeFees == 30m)),
            Times.Once);
    }

    [Fact]
    public async Task Update_WhenValidationFails_Returns400()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.UpdateApplicationTypeAsync(4, It.IsAny<ApplicationTypeDto>()))
            .ReturnsAsync(Result.ValidationFailure("Invalid application type data."));

        await AssertProblemDetailsAsync(
            await SendAsync(factory, HttpMethod.Put, "/api/ApplicationTypes/4", new UpdateApplicationTypeRequest
            {
                ApplicationTypeId = 4,
                ApplicationTypeTitle = "",
                ApplicationTypeFees = 0m
            }),
            HttpStatusCode.BadRequest,
            "Validation error",
            "Invalid application type data.");
    }

    [Fact]
    public async Task Update_WhenNotFound_Returns404()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.UpdateApplicationTypeAsync(10, It.IsAny<ApplicationTypeDto>()))
            .ReturnsAsync(Result.NotFound("Application type not found."));

        await AssertProblemDetailsAsync(
            await SendAsync(factory, HttpMethod.Put, "/api/ApplicationTypes/10", ValidRequest()),
            HttpStatusCode.NotFound,
            "Resource not found",
            "Application type not found.");
    }

    [Fact]
    public async Task Update_WhenConflict_Returns409()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.UpdateApplicationTypeAsync(10, It.IsAny<ApplicationTypeDto>()))
            .ReturnsAsync(Result.Conflict(
                "Application type update conflicts with existing data."));

        await AssertProblemDetailsAsync(
            await SendAsync(factory, HttpMethod.Put, "/api/ApplicationTypes/10", ValidRequest()),
            HttpStatusCode.Conflict,
            "Conflict",
            "Application type update conflicts with existing data.");
    }

    [Fact]
    public async Task Update_WhenServiceFails_Returns500()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.ApplicationTypeServiceMock
            .Setup(x => x.UpdateApplicationTypeAsync(10, It.IsAny<ApplicationTypeDto>()))
            .ReturnsAsync(Result.Failure("Failed to save application type changes."));

        await AssertProblemDetailsAsync(
            await SendAsync(factory, HttpMethod.Put, "/api/ApplicationTypes/10", ValidRequest()),
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    private static UpdateApplicationTypeRequest ValidRequest() => new()
    {
        ApplicationTypeId = 10,
        ApplicationTypeTitle = "Updated Type",
        ApplicationTypeFees = 20m
    };

    private static async Task<HttpResponseMessage> SendAsync(
        ApiWebApplicationFactory factory,
        HttpMethod method,
        string url,
        object? content = null,
        string? role = "Admin")
    {
        using var request = new HttpRequestMessage(method, url);

        if (role is not null)
        {
            request.Headers.Add("X-Test-User-Id", role == "Admin" ? "1" : "6");
            request.Headers.Add("X-Test-Username", "testuser");
            request.Headers.Add("X-Test-FullName", "Test User");
            request.Headers.Add("X-Test-Role", role);
        }

        if (content is not null)
            request.Content = JsonContent.Create(content);

        return await factory.CreateClient().SendAsync(request);
    }

    private static void VerifyGetNever(ApiWebApplicationFactory factory) =>
        factory.ApplicationTypeServiceMock.Verify(
            x => x.GetAllApplicationTypesAsync(), Times.Never);

    private static void VerifyUpdateNever(ApiWebApplicationFactory factory) =>
        factory.ApplicationTypeServiceMock.Verify(
            x => x.UpdateApplicationTypeAsync(
                It.IsAny<int>(),
                It.IsAny<ApplicationTypeDto>()),
            Times.Never);

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