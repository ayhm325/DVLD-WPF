using System.Net;
using System.Net.Http.Json;
using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.DTOs.TestDTO;
using DVLD.Contracts.Test;
using Moq;

namespace API.IntegrationTests.Controllers;

public sealed class TestsControllerTests
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TestsControllerTests(
        ApiWebApplicationFactory factory)
    {
        _factory = factory;

        _factory.TestServiceMock.Reset();

        _client = factory.CreateClient();
    }

    // =========================================================
    // AUTHENTICATION
    // =========================================================

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await _client.GetAsync(
                "/api/Tests");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedTests()
    {
        var dto =
            CreateTestDto(10);

        _factory.TestServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<TestDto>>.Success(
                    new List<TestDto>
                    {
                        dto
                    }));

        var response =
            await GetAuthenticatedAsync(
                "/api/Tests");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<TestResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        AssertTestResponse(
            dto,
            result[0]);

        _factory.TestServiceMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory.TestServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<TestDto>>
                    .FromValidationFailure(
                        "Invalid test data."));

        var response =
            await GetAuthenticatedAsync(
                "/api/Tests");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid test data.");
    }

    [Fact]
    public async Task GetAll_WhenNotFound_ReturnsNotFound()
    {
        _factory.TestServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<TestDto>>
                    .FromNotFound(
                        "Tests not found."));

        var response =
            await GetAuthenticatedAsync(
                "/api/Tests");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Tests not found.");
    }

    [Fact]
    public async Task GetAll_WhenConflict_ReturnsConflict()
    {
        _factory.TestServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<TestDto>>
                    .FromConflict(
                        "Test conflict."));

        var response =
            await GetAuthenticatedAsync(
                "/api/Tests");

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Test conflict.");
    }

    [Fact]
    public async Task GetAll_WhenForbidden_ReturnsForbiddenWithErrorBody()
    {
        _factory.TestServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<TestDto>>
                    .FromForbidden(
                        "You must be logged in first."));

        var response =
            await GetAuthenticatedAsync(
                "/api/Tests");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "You must be logged in first.");
    }

    [Fact]
    public async Task GetAll_WhenFailure_ReturnsInternalServerError()
    {
        _factory.TestServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<TestDto>>
                    .FromFailure(
                        "Database failure."));

        var response =
            await GetAuthenticatedAsync(
                "/api/Tests");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Database failure.");
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedTest()
    {
        var dto =
            CreateTestDto(20);

        _factory.TestServiceMock
            .Setup(x => x.GetByIdAsync(20))
            .ReturnsAsync(
                Result<TestDto>.Success(dto));

        var response =
            await GetAuthenticatedAsync(
                "/api/Tests/20");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<TestResponse>();

        Assert.NotNull(result);

        AssertTestResponse(
            dto,
            result);

        _factory.TestServiceMock.Verify(
            x => x.GetByIdAsync(20),
            Times.Once);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _factory.TestServiceMock
            .Setup(x => x.GetByIdAsync(20))
            .ReturnsAsync(
                Result<TestDto>
                    .FromNotFound(
                        "Test not found."));

        var response =
            await GetAuthenticatedAsync(
                "/api/Tests/20");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Test not found.");
    }

    [Fact]
    public async Task GetById_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory.TestServiceMock
            .Setup(x => x.GetByIdAsync(0))
            .ReturnsAsync(
                Result<TestDto>
                    .FromValidationFailure(
                        "Invalid test ID."));

        var response =
            await GetAuthenticatedAsync(
                "/api/Tests/0");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid test ID.");
    }

    // =========================================================
    // GET BY APPOINTMENT
    // =========================================================

    [Fact]
    public async Task GetByAppointmentId_WhenSuccessful_ReturnsMappedTests()
    {
        var dto =
            CreateTestDto(30);

        _factory.TestServiceMock
            .Setup(x =>
                x.GetByTestAppointmentIdAsync(50))
            .ReturnsAsync(
                Result<List<TestDto>>.Success(
                    new List<TestDto>
                    {
                        dto
                    }));

        var response =
            await GetAuthenticatedAsync(
                "/api/Tests/appointment/50");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<TestResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        AssertTestResponse(
            dto,
            result[0]);

        _factory.TestServiceMock.Verify(
            x =>
                x.GetByTestAppointmentIdAsync(50),
            Times.Once);
    }

    [Fact]
    public async Task GetByAppointmentId_WhenNotFound_ReturnsNotFound()
    {
        _factory.TestServiceMock
            .Setup(x =>
                x.GetByTestAppointmentIdAsync(50))
            .ReturnsAsync(
                Result<List<TestDto>>
                    .FromNotFound(
                        "Tests not found for appointment."));

        var response =
            await GetAuthenticatedAsync(
                "/api/Tests/appointment/50");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Tests not found for appointment.");
    }

    // =========================================================
    // GET BY USER
    // =========================================================

    [Fact]
    public async Task GetByUserId_WhenSuccessful_ReturnsMappedTests()
    {
        var dto =
            CreateTestDto(40);

        _factory.TestServiceMock
            .Setup(x =>
                x.GetByUserIdAsync(7))
            .ReturnsAsync(
                Result<List<TestDto>>.Success(
                    new List<TestDto>
                    {
                        dto
                    }));

        var response =
            await GetAuthenticatedAsync(
                "/api/Tests/created-by/7");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<TestResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        AssertTestResponse(
            dto,
            result[0]);

        _factory.TestServiceMock.Verify(
            x =>
                x.GetByUserIdAsync(7),
            Times.Once);
    }

    [Fact]
    public async Task GetByUserId_WhenForbidden_ReturnsForbiddenWithErrorBody()
    {
        _factory.TestServiceMock
            .Setup(x =>
                x.GetByUserIdAsync(7))
            .ReturnsAsync(
                Result<List<TestDto>>
                    .FromForbidden(
                        "Access denied."));

        var response =
            await GetAuthenticatedAsync(
                "/api/Tests/created-by/7");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Access denied.");
    }

    // =========================================================
    // ADD TEST RESULT
    // =========================================================

    [Fact]
    public async Task AddResult_WhenSuccessful_ReturnsCreatedTestId()
    {
        _factory.TestServiceMock
            .Setup(x =>
                x.AddAsync(
                    It.Is<SaveTestResultDto>(
                        dto =>
                            dto.TestAppointmentID == 100 &&
                            dto.TestResult &&
                            dto.Notes == "Passed successfully")))
            .ReturnsAsync(
                Result<int>.Success(500));

        var request =
            new SaveTestResultRequest(
                TestAppointmentId: 100,
                TestResult: true,
                Notes: "Passed successfully");

        var response =
            await PostAuthenticatedAsync(
                "/api/Tests",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<int>();

        Assert.Equal(
            500,
            result);

        _factory.TestServiceMock.Verify(
            x =>
                x.AddAsync(
                    It.Is<SaveTestResultDto>(
                        dto =>
                            dto.TestAppointmentID == 100 &&
                            dto.TestResult &&
                            dto.Notes == "Passed successfully")),
            Times.Once);
    }

    [Fact]
    public async Task AddResult_WhenFailedTestResult_ReturnsCreatedTestId()
    {
        _factory.TestServiceMock
            .Setup(x =>
                x.AddAsync(
                    It.Is<SaveTestResultDto>(
                        dto =>
                            dto.TestAppointmentID == 101 &&
                            !dto.TestResult &&
                            dto.Notes == "Failed")))
            .ReturnsAsync(
                Result<int>.Success(501));

        var request =
            new SaveTestResultRequest(
                TestAppointmentId: 101,
                TestResult: false,
                Notes: "Failed");

        var response =
            await PostAuthenticatedAsync(
                "/api/Tests",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<int>();

        Assert.Equal(
            501,
            result);
    }

    [Fact]
    public async Task AddResult_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory.TestServiceMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<SaveTestResultDto>()))
            .ReturnsAsync(
                Result<int>
                    .FromValidationFailure(
                        "Invalid test result."));

        var request =
            new SaveTestResultRequest(
                TestAppointmentId: 100,
                TestResult: true,
                Notes: null);

        var response =
            await PostAuthenticatedAsync(
                "/api/Tests",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid test result.");
    }

    [Fact]
    public async Task AddResult_WhenNotFound_ReturnsNotFound()
    {
        _factory.TestServiceMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<SaveTestResultDto>()))
            .ReturnsAsync(
                Result<int>
                    .FromNotFound(
                        "Appointment not found."));

        var request =
            new SaveTestResultRequest(
                TestAppointmentId: 100,
                TestResult: true,
                Notes: null);

        var response =
            await PostAuthenticatedAsync(
                "/api/Tests",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Appointment not found.");
    }

    [Fact]
    public async Task AddResult_WhenConflict_ReturnsConflict()
    {
        _factory.TestServiceMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<SaveTestResultDto>()))
            .ReturnsAsync(
                Result<int>
                    .FromConflict(
                        "Appointment is already locked."));

        var request =
            new SaveTestResultRequest(
                TestAppointmentId: 100,
                TestResult: true,
                Notes: null);

        var response =
            await PostAuthenticatedAsync(
                "/api/Tests",
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Appointment is already locked.");
    }

    [Fact]
    public async Task AddResult_WhenForbidden_ReturnsForbiddenWithErrorBody()
    {
        _factory.TestServiceMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<SaveTestResultDto>()))
            .ReturnsAsync(
                Result<int>
                    .FromForbidden(
                        "You must be logged in first."));

        var request =
            new SaveTestResultRequest(
                TestAppointmentId: 100,
                TestResult: true,
                Notes: null);

        var response =
            await PostAuthenticatedAsync(
                "/api/Tests",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "You must be logged in first.");
    }

    [Fact]
    public async Task AddResult_WhenFailure_ReturnsInternalServerError()
    {
        _factory.TestServiceMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<SaveTestResultDto>()))
            .ReturnsAsync(
                Result<int>
                    .FromFailure(
                        "Failed to save test result."));

        var request =
            new SaveTestResultRequest(
                TestAppointmentId: 100,
                TestResult: true,
                Notes: null);

        var response =
            await PostAuthenticatedAsync(
                "/api/Tests",
                request);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Failed to save test result.");
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private async Task<HttpResponseMessage> GetAuthenticatedAsync(
        string url)
    {
        var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                url);

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostAuthenticatedAsync(
        string url,
        object body)
    {
        var request =
            CreateAuthenticatedRequest(
                HttpMethod.Post,
                url);

        request.Content =
            JsonContent.Create(body);

        return await _client.SendAsync(request);
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string url)
    {
        var request =
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

        return request;
    }

    // =========================================================
    // DTO FACTORY
    // =========================================================

    private static TestDto CreateTestDto(
        int testId)
    {
        return new TestDto
        {
            TestID =
                testId,

            TestAppointmentID =
                100,

            TestResult =
                true,

            Notes =
                "Passed successfully",

            CreatedByUserID =
                1,

            CreatedByUserName =
                "testuser",

            TestTypeName =
                "Theory",

            AppointmentDate =
                new DateTime(
                    2026,
                    10,
                    1,
                    10,
                    30,
                    0)
        };
    }

    // =========================================================
    // MAPPING ASSERTION
    // =========================================================

    private static void AssertTestResponse(
        TestDto dto,
        TestResponse response)
    {
        Assert.Equal(
            dto.TestID,
            response.TestId);

        Assert.Equal(
            dto.TestAppointmentID,
            response.TestAppointmentId);

        Assert.Equal(
            dto.TestResult,
            response.TestResult);

        Assert.Equal(
            dto.Notes,
            response.Notes);

        Assert.Equal(
            dto.CreatedByUserID,
            response.CreatedByUserId);

        Assert.Equal(
            dto.CreatedByUserName,
            response.CreatedByUserName);

        Assert.Equal(
            dto.TestTypeName,
            response.TestTypeName);

        Assert.Equal(
            dto.AppointmentDate,
            response.AppointmentDate);
    }

    private static async Task AssertErrorAsync(
        HttpResponseMessage response,
        string expectedError)
    {
        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            expectedError,
            body.Error);
    }

    private sealed record ErrorResponse(
        string Error);
}