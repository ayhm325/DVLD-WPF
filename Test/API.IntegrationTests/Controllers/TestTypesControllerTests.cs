using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.TestTypeDTO;
using DVLD.Contracts.TestType;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Controllers;

public sealed class TestTypesControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TestTypesControllerTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.TestTypeServiceMock.Reset();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/TestTypes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _factory.TestTypeServiceMock.Verify(
            x => x.GetAllTestTypesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedResponses()
    {
        _factory.TestTypeServiceMock
            .Setup(x => x.GetAllTestTypesAsync())
            .ReturnsAsync(Result<List<TestTypeDto>>.Success(
            [
                new()
                {
                    TestTypeId = 1,
                    TestTypeTitle = "Vision Test",
                    TestTypeDescription = "Vision examination",
                    TestTypeFees = 10m
                }
            ]));

        var response = await SendAuthenticatedAsync(
            HttpMethod.Get, "/api/TestTypes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<List<TestTypeResponse>>();

        var item = Assert.Single(result!);

        Assert.Equal(1, item.TestTypeId);
        Assert.Equal("Vision Test", item.TestTypeTitle);
        Assert.Equal("Vision examination", item.TestTypeDescription);
        Assert.Equal(10m, item.TestTypeFees);

        _factory.TestTypeServiceMock.Verify(
            x => x.GetAllTestTypesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenValidationFails_ReturnsBadRequest()
    {
        SetupGetAll(Result<List<TestTypeDto>>
            .FromValidationFailure("validation error"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(HttpMethod.Get, "/api/TestTypes"),
            HttpStatusCode.BadRequest,
            "validation error");
    }

    [Fact]
    public async Task GetAll_WhenNotFound_ReturnsNotFound()
    {
        SetupGetAll(Result<List<TestTypeDto>>
            .FromNotFound("not found"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(HttpMethod.Get, "/api/TestTypes"),
            HttpStatusCode.NotFound,
            "not found");
    }

    [Fact]
    public async Task GetAll_WhenConflict_ReturnsConflict()
    {
        SetupGetAll(Result<List<TestTypeDto>>
            .FromConflict("conflict"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(HttpMethod.Get, "/api/TestTypes"),
            HttpStatusCode.Conflict,
            "conflict");
    }

    [Fact]
    public async Task GetAll_WhenFailureOccurs_ReturnsInternalServerError()
    {
        SetupGetAll(Result<List<TestTypeDto>>
            .FromFailure("failure"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(HttpMethod.Get, "/api/TestTypes"),
            HttpStatusCode.InternalServerError,
            "failure");
    }

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedResponse()
    {
        _factory.TestTypeServiceMock
            .Setup(x => x.GetTestTypeByIdAsync(5))
            .ReturnsAsync(Result<TestTypeDto>.Success(new TestTypeDto
            {
                TestTypeId = 5,
                TestTypeTitle = "Theory Test",
                TestTypeDescription = "Written theoretical examination",
                TestTypeFees = 15m
            }));

        var response = await SendAuthenticatedAsync(
            HttpMethod.Get, "/api/TestTypes/5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<TestTypeResponse>();

        Assert.NotNull(result);
        Assert.Equal(5, result.TestTypeId);
        Assert.Equal("Theory Test", result.TestTypeTitle);
        Assert.Equal("Written theoretical examination", result.TestTypeDescription);
        Assert.Equal(15m, result.TestTypeFees);

        _factory.TestTypeServiceMock.Verify(
            x => x.GetTestTypeByIdAsync(5), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _factory.TestTypeServiceMock
            .Setup(x => x.GetTestTypeByIdAsync(5))
            .ReturnsAsync(Result<TestTypeDto>
                .FromNotFound("test type not found"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(
                HttpMethod.Get, "/api/TestTypes/5"),
            HttpStatusCode.NotFound,
            "test type not found");
    }

    [Fact]
    public async Task GetById_WhenValidationFails_ReturnsBadRequest()
    {
        _factory.TestTypeServiceMock
            .Setup(x => x.GetTestTypeByIdAsync(5))
            .ReturnsAsync(Result<TestTypeDto>
                .FromValidationFailure("validation error"));

        await AssertFailureAsync(
            await SendAuthenticatedAsync(
                HttpMethod.Get, "/api/TestTypes/5"),
            HttpStatusCode.BadRequest,
            "validation error");
    }

    [Fact]
    public async Task Update_WhenAnonymous_ReturnsUnauthorized()
    {
        var response = await SendUpdateAsync(
            5,
            new UpdateTestTypeRequest
            {
                TestTypeId = 5,
                TestTypeTitle = "Updated",
                TestTypeDescription = "Description",
                TestTypeFees = 20m
            },
            role: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _factory.TestTypeServiceMock.Verify(
            x => x.UpdateTestTypeAsync(
                It.IsAny<int>(), It.IsAny<TestTypeDto>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_WhenStaff_ReturnsForbidden()
    {
        var response = await SendUpdateAsync(
            5,
            new UpdateTestTypeRequest
            {
                TestTypeId = 5,
                TestTypeTitle = "Updated",
                TestTypeDescription = "Description",
                TestTypeFees = 20m
            },
            role: "Staff");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _factory.TestTypeServiceMock.Verify(
            x => x.UpdateTestTypeAsync(
                It.IsAny<int>(), It.IsAny<TestTypeDto>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        _factory.TestTypeServiceMock
            .Setup(x => x.UpdateTestTypeAsync(
                5,
                It.Is<TestTypeDto>(d =>
                    d.TestTypeId == 5 &&
                    d.TestTypeTitle == "Updated Theory Test" &&
                    d.TestTypeDescription == "Updated description" &&
                    d.TestTypeFees == 20m)))
            .ReturnsAsync(Result.Success());

        var response = await SendUpdateAsync(
            5,
            new UpdateTestTypeRequest
            {
                TestTypeId = 999,
                TestTypeTitle = "Updated Theory Test",
                TestTypeDescription = "Updated description",
                TestTypeFees = 20m
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.TestTypeServiceMock.Verify(
            x => x.UpdateTestTypeAsync(
                5,
                It.Is<TestTypeDto>(d =>
                    d.TestTypeId == 5 &&
                    d.TestTypeTitle == "Updated Theory Test" &&
                    d.TestTypeDescription == "Updated description" &&
                    d.TestTypeFees == 20m)),
            Times.Once);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        SetupUpdate(Result.NotFound("test type not found"));

        await AssertFailureAsync(
            await SendUpdateAsync(5, ValidUpdateRequest()),
            HttpStatusCode.NotFound,
            "test type not found");
    }

    [Fact]
    public async Task Update_WhenConflictOccurs_ReturnsConflict()
    {
        SetupUpdate(Result.Conflict("conflict"));

        await AssertFailureAsync(
            await SendUpdateAsync(5, ValidUpdateRequest()),
            HttpStatusCode.Conflict,
            "conflict");
    }

    [Fact]
    public async Task Update_WhenValidationFails_ReturnsBadRequest()
    {
        SetupUpdate(Result.ValidationFailure("validation error"));

        await AssertFailureAsync(
            await SendUpdateAsync(5, ValidUpdateRequest()),
            HttpStatusCode.BadRequest,
            "validation error");
    }

    [Fact]
    public async Task Update_WhenFailureOccurs_ReturnsInternalServerError()
    {
        SetupUpdate(Result.Failure("failure"));

        await AssertFailureAsync(
            await SendUpdateAsync(5, ValidUpdateRequest()),
            HttpStatusCode.InternalServerError,
            "failure");
    }

    private void SetupGetAll(Result<List<TestTypeDto>> result) =>
        _factory.TestTypeServiceMock
            .Setup(x => x.GetAllTestTypesAsync())
            .ReturnsAsync(result);

    private void SetupUpdate(Result result) =>
        _factory.TestTypeServiceMock
            .Setup(x => x.UpdateTestTypeAsync(
                It.IsAny<int>(), It.IsAny<TestTypeDto>()))
            .ReturnsAsync(result);

    private static UpdateTestTypeRequest ValidUpdateRequest() =>
        new()
        {
            TestTypeId = 5,
            TestTypeTitle = "Updated",
            TestTypeDescription = "Description",
            TestTypeFees = 20m
        };

    private async Task<HttpResponseMessage> SendUpdateAsync(
        int id,
        UpdateTestTypeRequest request,
        string? role = "Admin") =>
        await SendAuthenticatedAsync(
            HttpMethod.Put,
            $"/api/TestTypes/{id}",
            request,
            role);

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(
        HttpMethod method,
        string url,
        object? content = null,
        string? role = "Staff")
    {
        using var request = new HttpRequestMessage(method, url);

        if (role is not null)
        {
            request.Headers.Add("X-Test-User-Id", "1");
            request.Headers.Add("X-Test-Username", "testuser");
            request.Headers.Add("X-Test-FullName", "Test User");
            request.Headers.Add("X-Test-Role", role);
        }

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
}