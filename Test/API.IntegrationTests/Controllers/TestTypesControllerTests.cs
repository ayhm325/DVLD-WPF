using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.TestTypeDTO;
using DVLD.Contracts.TestType;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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
        var dto = new TestTypeDto
        {
            TestTypeId = 1,
            TestTypeTitle = "Vision Test",
            TestTypeDescription = "Vision examination",
            TestTypeFees = 10m
        };

        _factory.TestTypeServiceMock.Setup(x => x.GetAllTestTypesAsync())
            .ReturnsAsync(Result<List<TestTypeDto>>.Success([dto]));

        var response = await SendAsync(HttpMethod.Get, "/api/TestTypes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TestTypeResponse>>();
        Assert.NotNull(result);

        var item = Assert.Single(result);
        Assert.Equal(dto.TestTypeId, item.TestTypeId);
        Assert.Equal(dto.TestTypeTitle, item.TestTypeTitle);
        Assert.Equal(dto.TestTypeDescription, item.TestTypeDescription);
        Assert.Equal(dto.TestTypeFees, item.TestTypeFees);

        _factory.TestTypeServiceMock.Verify(
            x => x.GetAllTestTypesAsync(), Times.Once);
    }

    [Theory]
    [InlineData("Validation", "validation error", 400, "Validation error")]
    [InlineData("NotFound", "not found", 404, "Resource not found")]
    [InlineData("Conflict", "conflict", 409, "Conflict")]
    public async Task GetAll_WhenServiceReturnsError_ReturnsProblemDetails(
        string type, string detail, int status, string title)
    {
        var result = type switch
        {
            "Validation" => Result<List<TestTypeDto>>.FromValidationFailure(detail),
            "NotFound" => Result<List<TestTypeDto>>.FromNotFound(detail),
            _ => Result<List<TestTypeDto>>.FromConflict(detail)
        };

        SetupGetAll(result);

        var response = await SendAsync(HttpMethod.Get, "/api/TestTypes");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task GetAll_WhenFailureOccurs_Returns500ProblemDetails()
    {
        SetupGetAll(Result<List<TestTypeDto>>.FromFailure("failure"));

        var response = await SendAsync(HttpMethod.Get, "/api/TestTypes");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedResponse()
    {
        var dto = new TestTypeDto
        {
            TestTypeId = 5,
            TestTypeTitle = "Theory Test",
            TestTypeDescription = "Written theoretical examination",
            TestTypeFees = 15m
        };

        _factory.TestTypeServiceMock.Setup(x => x.GetTestTypeByIdAsync(5))
            .ReturnsAsync(Result<TestTypeDto>.Success(dto));

        var response = await SendAsync(HttpMethod.Get, "/api/TestTypes/5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TestTypeResponse>();
        Assert.NotNull(result);

        Assert.Equal(dto.TestTypeId, result.TestTypeId);
        Assert.Equal(dto.TestTypeTitle, result.TestTypeTitle);
        Assert.Equal(dto.TestTypeDescription, result.TestTypeDescription);
        Assert.Equal(dto.TestTypeFees, result.TestTypeFees);

        _factory.TestTypeServiceMock.Verify(
            x => x.GetTestTypeByIdAsync(5), Times.Once);
    }

    [Theory]
    [InlineData("NotFound", "test type not found", 404, "Resource not found")]
    [InlineData("Validation", "validation error", 400, "Validation error")]
    public async Task GetById_WhenServiceReturnsError_ReturnsProblemDetails(
        string type, string detail, int status, string title)
    {
        var result = type == "NotFound"
            ? Result<TestTypeDto>.FromNotFound(detail)
            : Result<TestTypeDto>.FromValidationFailure(detail);

        _factory.TestTypeServiceMock
            .Setup(x => x.GetTestTypeByIdAsync(5))
            .ReturnsAsync(result);

        var response = await SendAsync(HttpMethod.Get, "/api/TestTypes/5");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task Update_WhenAnonymous_ReturnsUnauthorized()
    {
        var response = await SendUpdateAsync(5, ValidUpdateRequest(), null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _factory.TestTypeServiceMock.Verify(
            x => x.UpdateTestTypeAsync(
                It.IsAny<int>(), It.IsAny<TestTypeDto>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenStaff_ReturnsForbidden()
    {
        var response = await SendUpdateAsync(5, ValidUpdateRequest(), "Staff");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _factory.TestTypeServiceMock.Verify(
            x => x.UpdateTestTypeAsync(
                It.IsAny<int>(), It.IsAny<TestTypeDto>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        _factory.TestTypeServiceMock.Setup(x => x.UpdateTestTypeAsync(
                5,
                It.Is<TestTypeDto>(d =>
                    d.TestTypeId == 5 &&
                    d.TestTypeTitle == "Updated Theory Test" &&
                    d.TestTypeDescription == "Updated description" &&
                    d.TestTypeFees == 20m)))
            .ReturnsAsync(Result.Success());

        var response = await SendUpdateAsync(5, new UpdateTestTypeRequest
        {
            TestTypeId = 999,
            TestTypeTitle = "Updated Theory Test",
            TestTypeDescription = "Updated description",
            TestTypeFees = 20m
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.TestTypeServiceMock.Verify(x => x.UpdateTestTypeAsync(
            5,
            It.Is<TestTypeDto>(d =>
                d.TestTypeId == 5 &&
                d.TestTypeTitle == "Updated Theory Test" &&
                d.TestTypeDescription == "Updated description" &&
                d.TestTypeFees == 20m)), Times.Once);
    }

    [Theory]
    [InlineData("NotFound", "test type not found", 404, "Resource not found")]
    [InlineData("Conflict", "conflict", 409, "Conflict")]
    [InlineData("Validation", "validation error", 400, "Validation error")]
    public async Task Update_WhenServiceReturnsError_ReturnsProblemDetails(
        string type, string detail, int status, string title)
    {
        var result = type switch
        {
            "NotFound" => Result.NotFound(detail),
            "Conflict" => Result.Conflict(detail),
            _ => Result.ValidationFailure(detail)
        };

        SetupUpdate(result);

        var response = await SendUpdateAsync(5, ValidUpdateRequest());

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task Update_WhenFailureOccurs_Returns500ProblemDetails()
    {
        SetupUpdate(Result.Failure("failure"));

        var response = await SendUpdateAsync(5, ValidUpdateRequest());

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    private void SetupGetAll(Result<List<TestTypeDto>> result) =>
        _factory.TestTypeServiceMock.Setup(x => x.GetAllTestTypesAsync())
            .ReturnsAsync(result);

    private void SetupUpdate(Result result) =>
        _factory.TestTypeServiceMock.Setup(x =>
                x.UpdateTestTypeAsync(
                    It.IsAny<int>(), It.IsAny<TestTypeDto>()))
            .ReturnsAsync(result);

    private static UpdateTestTypeRequest ValidUpdateRequest() => new()
    {
        TestTypeId = 5,
        TestTypeTitle = "Updated",
        TestTypeDescription = "Description",
        TestTypeFees = 20m
    };

    private Task<HttpResponseMessage> SendUpdateAsync(
        int id, UpdateTestTypeRequest request, string? role = "Admin") =>
        SendAsync(HttpMethod.Put, $"/api/TestTypes/{id}", request, role);

    private async Task<HttpResponseMessage> SendAsync(
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
