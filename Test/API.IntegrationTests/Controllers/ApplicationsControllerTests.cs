using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Domain.Enums;
using DVLD.Contracts.Application;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Controllers;

public sealed class ApplicationsControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApplicationsControllerTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ApplicationServiceMock.Reset();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/Applications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _factory.ApplicationServiceMock.Verify(
            x => x.GetAllApplicationsAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenAdmin_ReturnsForbidden()
    {
        var response = await SendAuthenticatedAsync(
            HttpMethod.Get, "/api/Applications", role: "Admin");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _factory.ApplicationServiceMock.Verify(
            x => x.GetAllApplicationsAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedResponses()
    {
        var date = new DateTime(2026, 1, 10);
        var lastDate = new DateTime(2026, 1, 11);

        _factory.ApplicationServiceMock
            .Setup(x => x.GetAllApplicationsAsync())
            .ReturnsAsync(Result<List<ApplicationDto>>.Success(
            [
                new()
                {
                    ApplicationID = 10,
                    ApplicantPersonID = 20,
                    ApplicationDate = date,
                    ApplicationTypeID = 30,
                    ApplicationStatus = AppStatus.New,
                    LastStatusDate = lastDate,
                    PaidFees = 25.50m,
                    CreatedByUserID = 40,
                    CreatedByUserName = "admin"
                }
            ]));

        var response = await SendAuthenticatedAsync(
            HttpMethod.Get, "/api/Applications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<ApplicationResponse>>();
        var item = Assert.Single(result!);

        Assert.Equal(10, item.ApplicationId);
        Assert.Equal(20, item.ApplicantPersonId);
        Assert.Equal(date, item.ApplicationDate);
        Assert.Equal(30, item.ApplicationTypeId);
        Assert.Equal("New", item.ApplicationStatus);
        Assert.Equal("New", item.StatusText);
        Assert.Equal(lastDate, item.LastStatusDate);
        Assert.Equal(25.50m, item.PaidFees);
        Assert.Equal(40, item.CreatedByUserId);
        Assert.Equal("admin", item.CreatedByUserName);

        _factory.ApplicationServiceMock.Verify(
            x => x.GetAllApplicationsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenValidationFails_ReturnsBadRequest()
    {
        SetupGetAll(Result<List<ApplicationDto>>.FromValidationFailure("validation error"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(HttpMethod.Get, "/api/Applications"),
            HttpStatusCode.BadRequest, "validation error");
    }

    [Fact]
    public async Task GetAll_WhenNotFound_ReturnsNotFound()
    {
        SetupGetAll(Result<List<ApplicationDto>>.FromNotFound("not found"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(HttpMethod.Get, "/api/Applications"),
            HttpStatusCode.NotFound, "not found");
    }

    [Fact]
    public async Task GetAll_WhenConflict_ReturnsConflict()
    {
        SetupGetAll(Result<List<ApplicationDto>>.FromConflict("conflict"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(HttpMethod.Get, "/api/Applications"),
            HttpStatusCode.Conflict, "conflict");
    }

    [Fact]
    public async Task GetAll_WhenFailureOccurs_ReturnsInternalServerError()
    {
        SetupGetAll(Result<List<ApplicationDto>>.FromFailure("failure"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(HttpMethod.Get, "/api/Applications"),
            HttpStatusCode.InternalServerError, "failure");
    }

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedResponse()
    {
        var dto = new ApplicationDto
        {
            ApplicationID = 100,
            ApplicantPersonID = 200,
            ApplicationDate = new DateTime(2026, 2, 1),
            ApplicationTypeID = 300,
            ApplicationStatus = AppStatus.Completed,
            LastStatusDate = new DateTime(2026, 2, 2),
            PaidFees = 75m,
            CreatedByUserID = 400,
            CreatedByUserName = "staff"
        };

        _factory.ApplicationServiceMock
            .Setup(x => x.GetApplicationByIdAsync(100))
            .ReturnsAsync(Result<ApplicationDto>.Success(dto));

        var response = await SendAuthenticatedAsync(
            HttpMethod.Get, "/api/Applications/100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApplicationResponse>();

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
            x => x.GetApplicationByIdAsync(100), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.GetApplicationByIdAsync(100))
            .ReturnsAsync(Result<ApplicationDto>.FromNotFound("application not found"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(
                HttpMethod.Get, "/api/Applications/100"),
            HttpStatusCode.NotFound, "application not found");
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
            ApplicationDate = new DateTime(2026, 3, 1),
            LastStatusDate = new DateTime(2026, 3, 2),
            CreatedByUserName = "admin"
        };

        _factory.ApplicationServiceMock
            .Setup(x => x.GetBasicInfoAsync(20))
            .ReturnsAsync(Result<ApplicationBasicInfoDto>.Success(dto));

        var response = await SendAuthenticatedAsync(
            HttpMethod.Get, "/api/Applications/20/basic-info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<ApplicationBasicInfoResponse>();

        Assert.NotNull(result);
        Assert.Equal(10, result.ApplicantPersonId);
        Assert.Equal(20, result.ApplicationId);
        Assert.Equal("Cancelled", result.ApplicationStatus);
        Assert.Equal("Cancelled", result.StatusText);
        Assert.Equal(50m, result.PaidFees);
        Assert.Equal("New Driving License", result.ApplicationTypeName);
        Assert.Equal("John Doe", result.ApplicantFullName);
        Assert.Equal(new DateTime(2026, 3, 1), result.ApplicationDate);
        Assert.Equal(new DateTime(2026, 3, 2), result.LastStatusDate);
        Assert.Equal("admin", result.CreatedByUserName);

        _factory.ApplicationServiceMock.Verify(
            x => x.GetBasicInfoAsync(20), Times.Once);
    }

    [Fact]
    public async Task GetBasicInfo_WhenNotFound_ReturnsNotFound()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.GetBasicInfoAsync(20))
            .ReturnsAsync(Result<ApplicationBasicInfoDto>.FromNotFound("not found"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(
                HttpMethod.Get, "/api/Applications/20/basic-info"),
            HttpStatusCode.NotFound, "not found");
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreated()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.AddNewApplicationAsync(It.Is<CreateApplicationDto>(
                d => d.ApplicantPersonID == 100 && d.ApplicationTypeID == 200)))
            .ReturnsAsync(Result<int>.Success(500));

        var response = await SendAuthenticatedAsync(
            HttpMethod.Post,
            "/api/Applications",
            new CreateApplicationRequest
            {
                ApplicantPersonId = 100,
                ApplicationTypeId = 200
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<CreateApplicationResponse>();

        Assert.NotNull(result);
        Assert.Equal(500, result.ApplicationId);

        _factory.ApplicationServiceMock.Verify(
            x => x.AddNewApplicationAsync(It.Is<CreateApplicationDto>(
                d => d.ApplicantPersonID == 100 && d.ApplicationTypeID == 200)),
            Times.Once);
    }

    [Fact]
    public async Task Create_WhenValidationFails_ReturnsBadRequest()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.AddNewApplicationAsync(It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(Result<int>.FromValidationFailure("validation error"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/Applications",
                new CreateApplicationRequest
                {
                    ApplicantPersonId = 100,
                    ApplicationTypeId = 200
                }),
            HttpStatusCode.BadRequest, "validation error");
    }

    [Fact]
    public async Task Create_WhenConflictOccurs_ReturnsConflict()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.AddNewApplicationAsync(It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(Result<int>.FromConflict("conflict"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(
                HttpMethod.Post,
                "/api/Applications",
                new CreateApplicationRequest
                {
                    ApplicantPersonId = 100,
                    ApplicationTypeId = 200
                }),
            HttpStatusCode.Conflict, "conflict");
    }

    [Fact]
    public async Task Update_WhenRouteIdDoesNotMatchRequestId_ReturnsBadRequest()
    {
        var response = await SendAuthenticatedAsync(
            HttpMethod.Put,
            "/api/Applications/100",
            new UpdateApplicationRequest
            {
                ApplicationId = 200,
                ApplicationTypeId = 300
            });

        await AssertFailureAsync(
            response,
            HttpStatusCode.BadRequest,
            "Route application ID does not match request application ID.");

        _factory.ApplicationServiceMock.Verify(
            x => x.UpdateApplicationAsync(It.IsAny<UpdateApplicationDto>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.UpdateApplicationAsync(It.Is<UpdateApplicationDto>(
                d => d.ApplicationID == 100 && d.ApplicationTypeID == 300)))
            .ReturnsAsync(Result.Success());

        var response = await SendAuthenticatedAsync(
            HttpMethod.Put,
            "/api/Applications/100",
            new UpdateApplicationRequest
            {
                ApplicationId = 100,
                ApplicationTypeId = 300
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.ApplicationServiceMock.Verify(
            x => x.UpdateApplicationAsync(It.Is<UpdateApplicationDto>(
                d => d.ApplicationID == 100 && d.ApplicationTypeID == 300)),
            Times.Once);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        SetupUpdate(Result.NotFound("application not found"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(
                HttpMethod.Put,
                "/api/Applications/100",
                new UpdateApplicationRequest
                {
                    ApplicationId = 100,
                    ApplicationTypeId = 300
                }),
            HttpStatusCode.NotFound, "application not found");
    }

    [Fact]
    public async Task Update_WhenValidationFails_ReturnsBadRequest()
    {
        SetupUpdate(Result.ValidationFailure("validation error"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(
                HttpMethod.Put,
                "/api/Applications/100",
                new UpdateApplicationRequest
                {
                    ApplicationId = 100,
                    ApplicationTypeId = 300
                }),
            HttpStatusCode.BadRequest, "validation error");
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.DeleteApplicationAsync(100))
            .ReturnsAsync(Result.Success());

        var response = await SendAuthenticatedAsync(
            HttpMethod.Delete, "/api/Applications/100");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _factory.ApplicationServiceMock.Verify(
            x => x.DeleteApplicationAsync(100), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.DeleteApplicationAsync(100))
            .ReturnsAsync(Result.NotFound("application not found"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(
                HttpMethod.Delete, "/api/Applications/100"),
            HttpStatusCode.NotFound,
            "application not found");
    }

    [Fact]
    public async Task Complete_WhenSuccessful_ReturnsNoContent()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.CompleteApplicationAsync(100))
            .ReturnsAsync(Result.Success());

        var response = await SendAuthenticatedAsync(
            HttpMethod.Post, "/api/Applications/100/complete");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _factory.ApplicationServiceMock.Verify(
            x => x.CompleteApplicationAsync(100), Times.Once);
    }

    [Fact]
    public async Task Complete_WhenConflictOccurs_ReturnsConflict()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.CompleteApplicationAsync(100))
            .ReturnsAsync(Result.Conflict("cannot complete application"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(
                HttpMethod.Post, "/api/Applications/100/complete"),
            HttpStatusCode.Conflict,
            "cannot complete application");
    }

    [Fact]
    public async Task Cancel_WhenSuccessful_ReturnsNoContent()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.CancelApplicationAsync(100))
            .ReturnsAsync(Result.Success());

        var response = await SendAuthenticatedAsync(
            HttpMethod.Post, "/api/Applications/100/cancel");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _factory.ApplicationServiceMock.Verify(
            x => x.CancelApplicationAsync(100), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenForbidden_ReturnsForbidden()
    {
        _factory.ApplicationServiceMock
            .Setup(x => x.CancelApplicationAsync(100))
            .ReturnsAsync(Result.Forbidden("forbidden"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(
                HttpMethod.Post, "/api/Applications/100/cancel"),
            HttpStatusCode.Forbidden,
            "forbidden");
    }

    private void SetupGetAll(Result<List<ApplicationDto>> result) =>
        _factory.ApplicationServiceMock
            .Setup(x => x.GetAllApplicationsAsync())
            .ReturnsAsync(result);

    private void SetupUpdate(Result result) =>
        _factory.ApplicationServiceMock
            .Setup(x => x.UpdateApplicationAsync(It.IsAny<UpdateApplicationDto>()))
            .ReturnsAsync(result);

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(
        HttpMethod method,
        string url,
        object? content = null,
        string role = "Staff")
    {
        using var request = new HttpRequestMessage(method, url);

        request.Headers.Add("X-Test-User-Id", "1");
        request.Headers.Add("X-Test-Username", "testuser");
        request.Headers.Add("X-Test-FullName", "Test User");
        request.Headers.Add("X-Test-Role", role);

        if (content is not null)
            request.Content = JsonContent.Create(content);

        return await _client.SendAsync(request);
    }

    private static async Task AssertFailureAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedError)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(expectedError, await ReadErrorAsync(response));
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())?.Error;

    private sealed record ErrorResponse(string? Error);
    private sealed record CreateApplicationResponse(int ApplicationId);
}