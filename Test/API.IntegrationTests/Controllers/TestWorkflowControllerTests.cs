using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Domain.Enums;
using DVLD.Contracts.TestWorkflow;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.IntegrationTests.Controllers;

public sealed class TestWorkflowControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TestWorkflowControllerTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.TestWorkflowServiceMock.Reset();
        _client = factory.CreateClient();
    }

    // CanSchedule

    [Fact]
    public async Task CanSchedule_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            "/api/TestWorkflow/can-schedule?localAppId=10&testType=Theory");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _factory.TestWorkflowServiceMock.Verify(
            x => x.CanScheduleTestAsync(It.IsAny<int>(), It.IsAny<TestTypeEnum>()),
            Times.Never);
    }

    [Theory]
    [InlineData("Theory", TestTypeEnum.Theory)]
    [InlineData("Written", TestTypeEnum.Written)]
    [InlineData("Practical", TestTypeEnum.Practical)]
    public async Task CanSchedule_WhenSuccessful_ReturnsAllowed(
        string testType, TestTypeEnum expectedType)
    {
        _factory.TestWorkflowServiceMock
            .Setup(x => x.CanScheduleTestAsync(10, expectedType))
            .ReturnsAsync(Result.Success());

        var response = await SendAsync(
            HttpMethod.Get,
            $"/api/TestWorkflow/can-schedule?localAppId=10&testType={testType}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TestWorkflowResponse>();
        Assert.NotNull(result);
        Assert.True(result.Allowed);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorType);
        Assert.Null(result.NextTestType);

        _factory.TestWorkflowServiceMock.Verify(
            x => x.CanScheduleTestAsync(10, expectedType), Times.Once);
    }

    [Theory]
    [InlineData(400, "Validation error", "Test cannot be scheduled.")]
    [InlineData(404, "Resource not found", "Application not found.")]
    [InlineData(409, "Conflict", "Test already exists.")]
    [InlineData(403, "Forbidden", "Access denied.")]
    public async Task CanSchedule_WhenServiceReturnsExpectedError_ReturnsProblemDetails(
        int status, string title, string detail)
    {
        var result = status switch
        {
            400 => Result.ValidationFailure(detail),
            404 => Result.NotFound(detail),
            409 => Result.Conflict(detail),
            _ => Result.Forbidden(detail)
        };

        _factory.TestWorkflowServiceMock
            .Setup(x => x.CanScheduleTestAsync(10, TestTypeEnum.Theory))
            .ReturnsAsync(result);

        var response = await SendAsync(
            HttpMethod.Get,
            "/api/TestWorkflow/can-schedule?localAppId=10&testType=Theory");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task CanSchedule_WhenFailureOccurs_Returns500ProblemDetails()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x => x.CanScheduleTestAsync(10, TestTypeEnum.Theory))
            .ReturnsAsync(Result.Failure("Unexpected failure."));

        var response = await SendAsync(
            HttpMethod.Get,
            "/api/TestWorkflow/can-schedule?localAppId=10&testType=Theory");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    // GetNextTest

    [Fact]
    public async Task GetNextTest_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/TestWorkflow/next-test/10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _factory.TestWorkflowServiceMock.Verify(
            x => x.GetNextTestTypeAsync(It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(TestTypeEnum.Written)]
    [InlineData(TestTypeEnum.Practical)]
    public async Task GetNextTest_WhenSuccessful_ReturnsNextTestType(
        TestTypeEnum expectedType)
    {
        _factory.TestWorkflowServiceMock
            .Setup(x => x.GetNextTestTypeAsync(10))
            .ReturnsAsync(Result<TestTypeEnum>.Success(expectedType));

        var response = await SendAsync(
            HttpMethod.Get,
            "/api/TestWorkflow/next-test/10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TestWorkflowResponse>();
        Assert.NotNull(result);
        Assert.True(result.Allowed);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorType);
        Assert.Equal((int)expectedType, result.NextTestType);

        _factory.TestWorkflowServiceMock.Verify(
            x => x.GetNextTestTypeAsync(10), Times.Once);
    }

    [Theory]
    [InlineData(400, "Validation error", "Invalid application.")]
    [InlineData(404, "Resource not found", "Application not found.")]
    [InlineData(409, "Conflict", "Workflow conflict.")]
    [InlineData(403, "Forbidden", "Access denied.")]
    public async Task GetNextTest_WhenServiceReturnsExpectedError_ReturnsProblemDetails(
        int status, string title, string detail)
    {
        var result = status switch
        {
            400 => Result<TestTypeEnum>.FromValidationFailure(detail),
            404 => Result<TestTypeEnum>.FromNotFound(detail),
            409 => Result<TestTypeEnum>.FromConflict(detail),
            _ => Result<TestTypeEnum>.FromForbidden(detail)
        };

        _factory.TestWorkflowServiceMock
            .Setup(x => x.GetNextTestTypeAsync(10))
            .ReturnsAsync(result);

        var response = await SendAsync(
            HttpMethod.Get,
            "/api/TestWorkflow/next-test/10");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task GetNextTest_WhenFailureOccurs_Returns500ProblemDetails()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x => x.GetNextTestTypeAsync(10))
            .ReturnsAsync(Result<TestTypeEnum>.FromFailure("Unexpected failure."));

        var response = await SendAsync(
            HttpMethod.Get,
            "/api/TestWorkflow/next-test/10");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    // CanTake

    [Fact]
    public async Task CanTake_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/TestWorkflow/can-take/20");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _factory.TestWorkflowServiceMock.Verify(
            x => x.CanTakeTestAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CanTake_WhenSuccessful_ReturnsAllowed()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x => x.CanTakeTestAsync(20))
            .ReturnsAsync(Result.Success());

        var response = await SendAsync(
            HttpMethod.Get,
            "/api/TestWorkflow/can-take/20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TestWorkflowResponse>();
        Assert.NotNull(result);
        Assert.True(result.Allowed);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorType);
        Assert.Null(result.NextTestType);

        _factory.TestWorkflowServiceMock.Verify(
            x => x.CanTakeTestAsync(20), Times.Once);
    }

    [Theory]
    [InlineData(400, "Validation error", "Appointment is invalid.")]
    [InlineData(404, "Resource not found", "Appointment not found.")]
    [InlineData(409, "Conflict", "Test already taken.")]
    [InlineData(403, "Forbidden", "Access denied.")]
    public async Task CanTake_WhenServiceReturnsExpectedError_ReturnsProblemDetails(
        int status, string title, string detail)
    {
        var result = status switch
        {
            400 => Result.ValidationFailure(detail),
            404 => Result.NotFound(detail),
            409 => Result.Conflict(detail),
            _ => Result.Forbidden(detail)
        };

        _factory.TestWorkflowServiceMock
            .Setup(x => x.CanTakeTestAsync(20))
            .ReturnsAsync(result);

        var response = await SendAsync(
            HttpMethod.Get,
            "/api/TestWorkflow/can-take/20");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task CanTake_WhenFailureOccurs_Returns500ProblemDetails()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x => x.CanTakeTestAsync(20))
            .ReturnsAsync(Result.Failure("Unexpected failure."));

        var response = await SendAsync(
            HttpMethod.Get,
            "/api/TestWorkflow/can-take/20");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    // Helpers

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string uri, object? content = null)
    {
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Test-User-Id", "1");
        request.Headers.Add("X-Test-Username", "testuser");
        request.Headers.Add("X-Test-FullName", "Test User");
        request.Headers.Add("X-Test-Role", "Staff");

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
