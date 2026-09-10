using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.Interfaces;
using Domain.Enums;
using DVLD.Contracts.Application;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Controllers;

public sealed class ApplicationsControllerTests
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApplicationsControllerTests(
        ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ApplicationServiceMock.Reset();

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await _client.GetAsync(
                "/api/Applications");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedResponses()
    {
        var applicationDate =
            new DateTime(2026, 1, 10);

        var lastStatusDate =
            new DateTime(2026, 1, 11);

        var dto = new ApplicationDto
        {
            ApplicationID = 10,
            ApplicantPersonID = 20,
            ApplicationDate = applicationDate,
            ApplicationTypeID = 30,
            ApplicationStatus = AppStatus.New,
            LastStatusDate = lastStatusDate,
            PaidFees = 25.50m,
            CreatedByUserID = 40,
            CreatedByUserName = "admin"
        };

        _factory.ApplicationServiceMock
            .Setup(x => x.GetAllApplicationsAsync())
            .ReturnsAsync(
                Result<List<ApplicationDto>>.Success(
                    new List<ApplicationDto>
                    {
                        dto
                    }));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/Applications");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    List<ApplicationResponse>>();

        Assert.NotNull(result);

        var item = Assert.Single(result);

        Assert.Equal(10, item.ApplicationId);
        Assert.Equal(20, item.ApplicantPersonId);
        Assert.Equal(applicationDate, item.ApplicationDate);
        Assert.Equal(30, item.ApplicationTypeId);
        Assert.Equal("New", item.ApplicationStatus);
        Assert.Equal("New", item.StatusText);
        Assert.Equal(lastStatusDate, item.LastStatusDate);
        Assert.Equal(25.50m, item.PaidFees);
        Assert.Equal(40, item.CreatedByUserId);
        Assert.Equal("admin", item.CreatedByUserName);

        _factory.ApplicationServiceMock.Verify(
            x => x.GetAllApplicationsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenValidationFails_ReturnsBadRequest()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.GetAllApplicationsAsync())
            .ReturnsAsync(
                Result<List<ApplicationDto>>
                    .FromValidationFailure(
                        "validation error"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/Applications");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.BadRequest,
            "validation error");
    }

    [Fact]
    public async Task GetAll_WhenNotFound_ReturnsNotFound()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.GetAllApplicationsAsync())
            .ReturnsAsync(
                Result<List<ApplicationDto>>
                    .FromNotFound(
                        "not found"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/Applications");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.NotFound,
            "not found");
    }

    [Fact]
    public async Task GetAll_WhenConflict_ReturnsConflict()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.GetAllApplicationsAsync())
            .ReturnsAsync(
                Result<List<ApplicationDto>>
                    .FromConflict(
                        "conflict"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/Applications");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.Conflict,
            "conflict");
    }

    [Fact]
    public async Task GetAll_WhenForbidden_ReturnsForbidden()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.GetAllApplicationsAsync())
            .ReturnsAsync(
                Result<List<ApplicationDto>>
                    .FromForbidden(
                        "forbidden"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/Applications");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.Forbidden,
            "forbidden");
    }

    [Fact]
    public async Task GetAll_WhenFailureOccurs_ReturnsInternalServerError()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.GetAllApplicationsAsync())
            .ReturnsAsync(
                Result<List<ApplicationDto>>
                    .FromFailure(
                        "failure"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/Applications");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.InternalServerError,
            "failure");
    }

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedResponse()
    {
        var dto = new ApplicationDto
        {
            ApplicationID = 100,
            ApplicantPersonID = 200,
            ApplicationDate =
                new DateTime(2026, 2, 1),
            ApplicationTypeID = 300,
            ApplicationStatus = AppStatus.Completed,
            LastStatusDate =
                new DateTime(2026, 2, 2),
            PaidFees = 75m,
            CreatedByUserID = 400,
            CreatedByUserName = "staff"
        };

        _factory.ApplicationServiceMock
            .Setup(x => x.GetApplicationByIdAsync(100))
            .ReturnsAsync(
                Result<ApplicationDto>.Success(dto));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/Applications/100");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        Assert.NotNull(result);

        Assert.Equal(100, result.ApplicationId);
        Assert.Equal(200, result.ApplicantPersonId);
        Assert.Equal(300, result.ApplicationTypeId);
        Assert.Equal("Completed", result.ApplicationStatus);
        Assert.Equal("Completed", result.StatusText);
        Assert.Equal(75m, result.PaidFees);
        Assert.Equal(400, result.CreatedByUserId);
        Assert.Equal("staff", result.CreatedByUserName);

        _factory.ApplicationServiceMock.Verify(
            x => x.GetApplicationByIdAsync(100),
            Times.Once);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.GetApplicationByIdAsync(100))
            .ReturnsAsync(
                Result<ApplicationDto>
                    .FromNotFound(
                        "application not found"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/Applications/100");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.NotFound,
            "application not found");
    }

    [Fact]
    public async Task GetBasicInfo_WhenSuccessful_ReturnsMappedResponse()
    {
        var dto = new ApplicationBasicInfoDto
        {
            ApplicantPersonID = 10,
            ApplicationID = 20,
            ApplicationStatus = AppStatus.Cancelled,
            PaidFees = 50m,
            ApplicationTypeName = "New Driving License",
            ApplicantFullName = "John Doe",
            ApplicationDate =
                new DateTime(2026, 3, 1),
            LastStatusDate =
                new DateTime(2026, 3, 2),
            CreatedByUserName = "admin"
        };

        _factory.ApplicationServiceMock
            .Setup(x => x.GetBasicInfoAsync(20))
            .ReturnsAsync(
                Result<ApplicationBasicInfoDto>
                    .Success(dto));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/Applications/20/basic-info");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ApplicationBasicInfoResponse>();

        Assert.NotNull(result);

        Assert.Equal(10, result.ApplicantPersonId);
        Assert.Equal(20, result.ApplicationId);
        Assert.Equal("Cancelled", result.ApplicationStatus);
        Assert.Equal("Cancelled", result.StatusText);
        Assert.Equal(50m, result.PaidFees);
        Assert.Equal(
            "New Driving License",
            result.ApplicationTypeName);
        Assert.Equal(
            "John Doe",
            result.ApplicantFullName);
        Assert.Equal(
            new DateTime(2026, 3, 1),
            result.ApplicationDate);
        Assert.Equal(
            new DateTime(2026, 3, 2),
            result.LastStatusDate);
        Assert.Equal(
            "admin",
            result.CreatedByUserName);

        _factory.ApplicationServiceMock.Verify(
            x => x.GetBasicInfoAsync(20),
            Times.Once);
    }

    [Fact]
    public async Task GetBasicInfo_WhenNotFound_ReturnsNotFound()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.GetBasicInfoAsync(20))
            .ReturnsAsync(
                Result<ApplicationBasicInfoDto>
                    .FromNotFound(
                        "not found"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Get,
                "/api/Applications/20/basic-info");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.NotFound,
            "not found");
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreated()
    {
        _factory.ApplicationServiceMock
            .Setup(x =>
                x.AddNewApplicationAsync(
                    It.Is<CreateApplicationDto>(
                        d =>
                            d.ApplicantPersonID == 100 &&
                            d.ApplicationTypeID == 200)))
            .ReturnsAsync(
                Result<int>.Success(500));

        var request =
            new CreateApplicationRequest
            {
                ApplicantPersonId = 100,
                ApplicationTypeId = 200
            };

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/Applications",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    CreateApplicationResponse>();

        Assert.NotNull(result);
        Assert.Equal(
            500,
            result.ApplicationId);

        _factory.ApplicationServiceMock.Verify(
            x =>
                x.AddNewApplicationAsync(
                    It.Is<CreateApplicationDto>(
                        d =>
                            d.ApplicantPersonID == 100 &&
                            d.ApplicationTypeID == 200)),
            Times.Once);
    }

    [Fact]
    public async Task Create_WhenValidationFails_ReturnsBadRequest()
    {
        _factory.ApplicationServiceMock
            .Setup(x =>
                x.AddNewApplicationAsync(
                    It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>
                    .FromValidationFailure(
                        "validation error"));

        var request =
            new CreateApplicationRequest
            {
                ApplicantPersonId = 100,
                ApplicationTypeId = 200
            };

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/Applications",
                request);

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.BadRequest,
            "validation error");
    }

    [Fact]
    public async Task Create_WhenConflictOccurs_ReturnsConflict()
    {
        _factory.ApplicationServiceMock
            .Setup(x =>
                x.AddNewApplicationAsync(
                    It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>
                    .FromConflict(
                        "conflict"));

        var request =
            new CreateApplicationRequest
            {
                ApplicantPersonId = 100,
                ApplicationTypeId = 200
            };

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/Applications",
                request);

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.Conflict,
            "conflict");
    }

    [Fact]
    public async Task Update_WhenRouteIdDoesNotMatchRequestId_ReturnsBadRequest()
    {
        var request =
            new UpdateApplicationRequest
            {
                ApplicationId = 200,
                ApplicationTypeId = 300
            };

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Put,
                "/api/Applications/100",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "Route application ID does not match request application ID.",
            error);

        _factory.ApplicationServiceMock.Verify(
            x =>
                x.UpdateApplicationAsync(
                    It.IsAny<UpdateApplicationDto>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        _factory.ApplicationServiceMock
            .Setup(x =>
                x.UpdateApplicationAsync(
                    It.Is<UpdateApplicationDto>(
                        d =>
                            d.ApplicationID == 100 &&
                            d.ApplicationTypeID == 300)))
            .ReturnsAsync(
                Result.Success());

        var request =
            new UpdateApplicationRequest
            {
                ApplicationId = 100,
                ApplicationTypeId = 300
            };

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Put,
                "/api/Applications/100",
                request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        _factory.ApplicationServiceMock.Verify(
            x =>
                x.UpdateApplicationAsync(
                    It.Is<UpdateApplicationDto>(
                        d =>
                            d.ApplicationID == 100 &&
                            d.ApplicationTypeID == 300)),
            Times.Once);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        _factory.ApplicationServiceMock
            .Setup(x =>
                x.UpdateApplicationAsync(
                    It.IsAny<UpdateApplicationDto>()))
            .ReturnsAsync(
                Result.NotFound(
                    "application not found"));

        var request =
            new UpdateApplicationRequest
            {
                ApplicationId = 100,
                ApplicationTypeId = 300
            };

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Put,
                "/api/Applications/100",
                request);

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.NotFound,
            "application not found");
    }

    [Fact]
    public async Task Update_WhenValidationFails_ReturnsBadRequest()
    {
        _factory.ApplicationServiceMock
            .Setup(x =>
                x.UpdateApplicationAsync(
                    It.IsAny<UpdateApplicationDto>()))
            .ReturnsAsync(
                Result.ValidationFailure(
                    "validation error"));

        var request =
            new UpdateApplicationRequest
            {
                ApplicationId = 100,
                ApplicationTypeId = 300
            };

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Put,
                "/api/Applications/100",
                request);

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.BadRequest,
            "validation error");
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.DeleteApplicationAsync(100))
            .ReturnsAsync(
                Result.Success());

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Delete,
                "/api/Applications/100");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        _factory.ApplicationServiceMock.Verify(
            x => x.DeleteApplicationAsync(100),
            Times.Once);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.DeleteApplicationAsync(100))
            .ReturnsAsync(
                Result.NotFound(
                    "application not found"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Delete,
                "/api/Applications/100");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.NotFound,
            "application not found");
    }

    [Fact]
    public async Task Complete_WhenSuccessful_ReturnsNoContent()
    {
        _factory.ApplicationServiceMock
            .Setup(x =>
                x.CompleteApplicationAsync(100))
            .ReturnsAsync(
                Result.Success());

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/Applications/100/complete");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        _factory.ApplicationServiceMock.Verify(
            x =>
                x.CompleteApplicationAsync(100),
            Times.Once);
    }

    [Fact]
    public async Task Complete_WhenConflictOccurs_ReturnsConflict()
    {
        _factory.ApplicationServiceMock
            .Setup(x =>
                x.CompleteApplicationAsync(100))
            .ReturnsAsync(
                Result.Conflict(
                    "cannot complete application"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/Applications/100/complete");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.Conflict,
            "cannot complete application");
    }

    [Fact]
    public async Task Cancel_WhenSuccessful_ReturnsNoContent()
    {
        _factory.ApplicationServiceMock
            .Setup(x =>
                x.CancelApplicationAsync(100))
            .ReturnsAsync(
                Result.Success());

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/Applications/100/cancel");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        _factory.ApplicationServiceMock.Verify(
            x =>
                x.CancelApplicationAsync(100),
            Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenForbidden_ReturnsForbidden()
    {
        _factory.ApplicationServiceMock
            .Setup(x =>
                x.CancelApplicationAsync(100))
            .ReturnsAsync(
                Result.Forbidden(
                    "forbidden"));

        var response =
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/Applications/100/cancel");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.Forbidden,
            "forbidden");
    }

    private async Task<HttpResponseMessage>
        SendAuthenticatedAsync(
            HttpMethod method,
            string url,
            object? content = null)
    {
        using var request =
            new HttpRequestMessage(
                method,
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
            "Staff");

        if (content is not null)
        {
            request.Content =
                JsonContent.Create(content);
        }

        return await _client.SendAsync(request);
    }

    private static async Task
        AssertFailureResponseAsync(
            HttpResponseMessage response,
            HttpStatusCode expectedStatus,
            string expectedError)
    {
        Assert.Equal(
            expectedStatus,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            expectedError,
            error);
    }

    private static async Task<string?>
        ReadErrorAsync(
            HttpResponseMessage response)
    {
        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        return body?.Error;
    }

    private sealed record ErrorResponse(
        string? Error);

    private sealed record CreateApplicationResponse(
        int ApplicationId);
}