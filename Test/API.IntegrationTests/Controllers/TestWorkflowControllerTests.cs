using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.Interfaces;
using Domain.Enums;
using DVLD.Contracts.TestAppointment;
using DVLD.Contracts.TestWorkflow;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Controllers;

public sealed class TestWorkflowControllerTests
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TestWorkflowControllerTests(
        ApiWebApplicationFactory factory)
    {
        _factory = factory;

        _factory.TestWorkflowServiceMock.Reset();

        _client = factory.CreateClient();
    }

    // ============================================================
    // CanSchedule
    // ============================================================

    [Fact]
    public async Task CanSchedule_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await _client.GetAsync(
                "/api/TestWorkflow/can-schedule?localAppId=10&testType=Theory");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        _factory.TestWorkflowServiceMock.Verify(
            x => x.CanScheduleTestAsync(
                It.IsAny<int>(),
                It.IsAny<TestTypeEnum>()),
            Times.Never);
    }

    [Fact]
    public async Task CanSchedule_WithTheory_MapsToDomainTheoryAndReturnsAllowed()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.CanScheduleTestAsync(
                    10,
                    TestTypeEnum.Theory))
            .ReturnsAsync(
                Result.Success());

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/can-schedule?localAppId=10&testType=Theory");

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<TestWorkflowResponse>();

        Assert.NotNull(result);

        Assert.True(result.Allowed);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorType);
        Assert.Null(result.NextTestType);

        _factory.TestWorkflowServiceMock.Verify(
            x =>
                x.CanScheduleTestAsync(
                    10,
                    TestTypeEnum.Theory),
            Times.Once);
    }

    [Fact]
    public async Task CanSchedule_WithWritten_MapsToDomainWrittenAndReturnsAllowed()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.CanScheduleTestAsync(
                    10,
                    TestTypeEnum.Written))
            .ReturnsAsync(
                Result.Success());

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/can-schedule?localAppId=10&testType=Written");

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<TestWorkflowResponse>();

        Assert.NotNull(result);

        Assert.True(result.Allowed);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorType);
        Assert.Null(result.NextTestType);

        _factory.TestWorkflowServiceMock.Verify(
            x =>
                x.CanScheduleTestAsync(
                    10,
                    TestTypeEnum.Written),
            Times.Once);
    }

    [Fact]
    public async Task CanSchedule_WithPractical_MapsToDomainPracticalAndReturnsAllowed()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.CanScheduleTestAsync(
                    10,
                    TestTypeEnum.Practical))
            .ReturnsAsync(
                Result.Success());

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/can-schedule?localAppId=10&testType=Practical");

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<TestWorkflowResponse>();

        Assert.NotNull(result);

        Assert.True(result.Allowed);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorType);
        Assert.Null(result.NextTestType);

        _factory.TestWorkflowServiceMock.Verify(
            x =>
                x.CanScheduleTestAsync(
                    10,
                    TestTypeEnum.Practical),
            Times.Once);
    }

    [Fact]
    public async Task CanSchedule_WhenValidationFails_ReturnsBadRequestResponse()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.CanScheduleTestAsync(
                    10,
                    TestTypeEnum.Theory))
            .ReturnsAsync(
                Result.ValidationFailure(
                    "Test cannot be scheduled."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/can-schedule?localAppId=10&testType=Theory");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.BadRequest,
            "Test cannot be scheduled.",
            nameof(ErrorType.Validation));

        _factory.TestWorkflowServiceMock.Verify(
            x =>
                x.CanScheduleTestAsync(
                    10,
                    TestTypeEnum.Theory),
            Times.Once);
    }

    [Fact]
    public async Task CanSchedule_WhenNotFound_ReturnsNotFoundResponse()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.CanScheduleTestAsync(
                    10,
                    TestTypeEnum.Theory))
            .ReturnsAsync(
                Result.NotFound(
                    "Application not found."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/can-schedule?localAppId=10&testType=Theory");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.NotFound,
            "Application not found.",
            nameof(ErrorType.NotFound));
    }

    [Fact]
    public async Task CanSchedule_WhenConflictOccurs_ReturnsConflictResponse()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.CanScheduleTestAsync(
                    10,
                    TestTypeEnum.Theory))
            .ReturnsAsync(
                Result.Conflict(
                    "Test already exists."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/can-schedule?localAppId=10&testType=Theory");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.Conflict,
            "Test already exists.",
            nameof(ErrorType.Conflict));
    }

    [Fact]
    public async Task CanSchedule_WhenForbidden_ReturnsForbiddenResponse()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.CanScheduleTestAsync(
                    10,
                    TestTypeEnum.Theory))
            .ReturnsAsync(
                Result.Forbidden(
                    "Access denied."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/can-schedule?localAppId=10&testType=Theory");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.Forbidden,
            "Access denied.",
            nameof(ErrorType.Forbidden));
    }

    [Fact]
    public async Task CanSchedule_WhenUnexpectedFailureOccurs_ReturnsInternalServerError()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.CanScheduleTestAsync(
                    10,
                    TestTypeEnum.Theory))
            .ReturnsAsync(
                Result.Failure(
                    "Unexpected failure."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/can-schedule?localAppId=10&testType=Theory");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.InternalServerError,
            "Unexpected failure.",
            nameof(ErrorType.Failure));
    }

    // ============================================================
    // GetNextTest
    // ============================================================

    [Fact]
    public async Task GetNextTest_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await _client.GetAsync(
                "/api/TestWorkflow/next-test/10");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        _factory.TestWorkflowServiceMock.Verify(
            x => x.GetNextTestTypeAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetNextTest_WhenSuccessful_ReturnsNextTestType()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.GetNextTestTypeAsync(10))
            .ReturnsAsync(
                Result<TestTypeEnum>.Success(
                    TestTypeEnum.Written));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/next-test/10");

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<TestWorkflowResponse>();

        Assert.NotNull(result);

        Assert.True(result.Allowed);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorType);

        Assert.Equal(
            (int)TestTypeEnum.Written,
            result.NextTestType);

        _factory.TestWorkflowServiceMock.Verify(
            x => x.GetNextTestTypeAsync(10),
            Times.Once);
    }

    [Fact]
    public async Task GetNextTest_WhenNextTestIsPractical_ReturnsPracticalValue()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.GetNextTestTypeAsync(10))
            .ReturnsAsync(
                Result<TestTypeEnum>.Success(
                    TestTypeEnum.Practical));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/next-test/10");

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<TestWorkflowResponse>();

        Assert.NotNull(result);

        Assert.True(result.Allowed);

        Assert.Equal(
            (int)TestTypeEnum.Practical,
            result.NextTestType);
    }

    [Fact]
    public async Task GetNextTest_WhenValidationFails_ReturnsBadRequestResponse()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.GetNextTestTypeAsync(10))
            .ReturnsAsync(
                Result<TestTypeEnum>
                    .FromValidationFailure(
                        "Invalid application."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/next-test/10");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.BadRequest,
            "Invalid application.",
            nameof(ErrorType.Validation));
    }

    [Fact]
    public async Task GetNextTest_WhenNotFound_ReturnsNotFoundResponse()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.GetNextTestTypeAsync(10))
            .ReturnsAsync(
                Result<TestTypeEnum>
                    .FromNotFound(
                        "Application not found."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/next-test/10");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.NotFound,
            "Application not found.",
            nameof(ErrorType.NotFound));
    }

    [Fact]
    public async Task GetNextTest_WhenConflictOccurs_ReturnsConflictResponse()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.GetNextTestTypeAsync(10))
            .ReturnsAsync(
                Result<TestTypeEnum>
                    .FromConflict(
                        "Workflow conflict."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/next-test/10");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.Conflict,
            "Workflow conflict.",
            nameof(ErrorType.Conflict));
    }

    [Fact]
    public async Task GetNextTest_WhenForbidden_ReturnsForbiddenResponse()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.GetNextTestTypeAsync(10))
            .ReturnsAsync(
                Result<TestTypeEnum>
                    .FromForbidden(
                        "Access denied."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/next-test/10");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.Forbidden,
            "Access denied.",
            nameof(ErrorType.Forbidden));
    }

    [Fact]
    public async Task GetNextTest_WhenUnexpectedFailureOccurs_ReturnsInternalServerError()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.GetNextTestTypeAsync(10))
            .ReturnsAsync(
                Result<TestTypeEnum>
                    .FromFailure(
                        "Unexpected failure."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/next-test/10");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.InternalServerError,
            "Unexpected failure.",
            nameof(ErrorType.Failure));
    }

    // ============================================================
    // CanTake
    // ============================================================

    [Fact]
    public async Task CanTake_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await _client.GetAsync(
                "/api/TestWorkflow/can-take/20");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        _factory.TestWorkflowServiceMock.Verify(
            x => x.CanTakeTestAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task CanTake_WhenSuccessful_ReturnsAllowed()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.CanTakeTestAsync(20))
            .ReturnsAsync(
                Result.Success());

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/can-take/20");

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<TestWorkflowResponse>();

        Assert.NotNull(result);

        Assert.True(result.Allowed);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorType);
        Assert.Null(result.NextTestType);

        _factory.TestWorkflowServiceMock.Verify(
            x => x.CanTakeTestAsync(20),
            Times.Once);
    }

    [Fact]
    public async Task CanTake_WhenValidationFails_ReturnsBadRequestResponse()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.CanTakeTestAsync(20))
            .ReturnsAsync(
                Result.ValidationFailure(
                    "Appointment is invalid."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/can-take/20");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.BadRequest,
            "Appointment is invalid.",
            nameof(ErrorType.Validation));
    }

    [Fact]
    public async Task CanTake_WhenNotFound_ReturnsNotFoundResponse()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.CanTakeTestAsync(20))
            .ReturnsAsync(
                Result.NotFound(
                    "Appointment not found."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/can-take/20");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.NotFound,
            "Appointment not found.",
            nameof(ErrorType.NotFound));
    }

    [Fact]
    public async Task CanTake_WhenConflictOccurs_ReturnsConflictResponse()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.CanTakeTestAsync(20))
            .ReturnsAsync(
                Result.Conflict(
                    "Test already taken."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/can-take/20");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.Conflict,
            "Test already taken.",
            nameof(ErrorType.Conflict));
    }

    [Fact]
    public async Task CanTake_WhenForbidden_ReturnsForbiddenResponse()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.CanTakeTestAsync(20))
            .ReturnsAsync(
                Result.Forbidden(
                    "Access denied."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/can-take/20");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.Forbidden,
            "Access denied.",
            nameof(ErrorType.Forbidden));
    }

    [Fact]
    public async Task CanTake_WhenUnexpectedFailureOccurs_ReturnsInternalServerError()
    {
        _factory.TestWorkflowServiceMock
            .Setup(x =>
                x.CanTakeTestAsync(20))
            .ReturnsAsync(
                Result.Failure(
                    "Unexpected failure."));

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/TestWorkflow/can-take/20");

        var response =
            await _client.SendAsync(request);

        await AssertWorkflowFailureAsync(
            response,
            HttpStatusCode.InternalServerError,
            "Unexpected failure.",
            nameof(ErrorType.Failure));
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static HttpRequestMessage
        CreateAuthenticatedRequest(
            HttpMethod method,
            string uri)
    {
        var request =
            new HttpRequestMessage(
                method,
                uri);

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

        return request;
    }

    private static async Task
        AssertWorkflowFailureAsync(
            HttpResponseMessage response,
            HttpStatusCode expectedStatus,
            string expectedError,
            string expectedErrorType)
    {
        Assert.Equal(
            expectedStatus,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<TestWorkflowResponse>();

        Assert.NotNull(result);

        Assert.False(result.Allowed);

        Assert.Equal(
            expectedError,
            result.Error);

        Assert.Equal(
            expectedErrorType,
            result.ErrorType);

        Assert.Null(result.NextTestType);
    }
}