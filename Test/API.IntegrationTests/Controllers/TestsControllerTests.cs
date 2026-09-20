using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.DTOs.TestDTO;
using DVLD.Contracts.Test;
using Moq;

namespace API.IntegrationTests.Controllers;

public sealed class TestsControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TestsControllerTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.TestServiceMock.Reset();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/Tests");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedTests()
    {
        var dto = CreateTestDto(10);

        _factory.TestServiceMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<TestDto>>.Success([dto]));

        var response = await GetAsync("/api/Tests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TestResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
        AssertTestResponse(dto, result[0]);

        _factory.TestServiceMock.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Theory]
    [InlineData("Validation", "Invalid test data.", 400, "Validation error")]
    [InlineData("NotFound", "Tests not found.", 404, "Resource not found")]
    [InlineData("Conflict", "Test conflict.", 409, "Conflict")]
    [InlineData("Forbidden", "You must be logged in first.", 403, "Forbidden")]
    public async Task GetAll_WhenServiceReturnsError_ReturnsProblemDetails(
        string type, string detail, int status, string title)
    {
        var result = type switch
        {
            "Validation" => Result<List<TestDto>>.FromValidationFailure(detail),
            "NotFound" => Result<List<TestDto>>.FromNotFound(detail),
            "Conflict" => Result<List<TestDto>>.FromConflict(detail),
            _ => Result<List<TestDto>>.FromForbidden(detail)
        };

        _factory.TestServiceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(result);

        var response = await GetAsync("/api/Tests");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task GetAll_WhenFailure_Returns500ProblemDetails()
    {
        _factory.TestServiceMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<TestDto>>.FromFailure("Database failure."));

        var response = await GetAsync("/api/Tests");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedTest()
    {
        var dto = CreateTestDto(20);

        _factory.TestServiceMock.Setup(x => x.GetByIdAsync(20))
            .ReturnsAsync(Result<TestDto>.Success(dto));

        var response = await GetAsync("/api/Tests/20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TestResponse>();
        Assert.NotNull(result);
        AssertTestResponse(dto, result);

        _factory.TestServiceMock.Verify(x => x.GetByIdAsync(20), Times.Once);
    }

    [Theory]
    [InlineData(20, "NotFound", "Test not found.", 404, "Resource not found")]
    [InlineData(0, "Validation", "Invalid test ID.", 400, "Validation error")]
    public async Task GetById_WhenServiceReturnsError_ReturnsProblemDetails(
        int id, string type, string detail, int status, string title)
    {
        var result = type == "NotFound"
            ? Result<TestDto>.FromNotFound(detail)
            : Result<TestDto>.FromValidationFailure(detail);

        _factory.TestServiceMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(result);

        var response = await GetAsync($"/api/Tests/{id}");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task GetByAppointmentId_WhenSuccessful_ReturnsMappedTests()
    {
        var dto = CreateTestDto(30);

        _factory.TestServiceMock.Setup(x => x.GetByTestAppointmentIdAsync(50))
            .ReturnsAsync(Result<List<TestDto>>.Success([dto]));

        var response = await GetAsync("/api/Tests/appointment/50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TestResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
        AssertTestResponse(dto, result[0]);

        _factory.TestServiceMock.Verify(
            x => x.GetByTestAppointmentIdAsync(50), Times.Once);
    }

    [Fact]
    public async Task GetByAppointmentId_WhenNotFound_ReturnsProblemDetails()
    {
        const string detail = "Tests not found for appointment.";

        _factory.TestServiceMock.Setup(x => x.GetByTestAppointmentIdAsync(50))
            .ReturnsAsync(Result<List<TestDto>>.FromNotFound(detail));

        var response = await GetAsync("/api/Tests/appointment/50");

        await AssertProblemDetailsAsync(
            response, HttpStatusCode.NotFound, "Resource not found", detail);
    }

    [Fact]
    public async Task GetByUserId_WhenSuccessful_ReturnsMappedTests()
    {
        var dto = CreateTestDto(40);

        _factory.TestServiceMock.Setup(x => x.GetByUserIdAsync(7))
            .ReturnsAsync(Result<List<TestDto>>.Success([dto]));

        var response = await GetAsync("/api/Tests/created-by/7");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TestResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
        AssertTestResponse(dto, result[0]);

        _factory.TestServiceMock.Verify(
            x => x.GetByUserIdAsync(7), Times.Once);
    }

    [Fact]
    public async Task GetByUserId_WhenForbidden_ReturnsProblemDetails()
    {
        const string detail = "Access denied.";

        _factory.TestServiceMock.Setup(x => x.GetByUserIdAsync(7))
            .ReturnsAsync(Result<List<TestDto>>.FromForbidden(detail));

        var response = await GetAsync("/api/Tests/created-by/7");

        await AssertProblemDetailsAsync(
            response, HttpStatusCode.Forbidden, "Forbidden", detail);
    }

    [Theory]
    [InlineData(100, true, "Passed successfully", 500)]
    [InlineData(101, false, "Failed", 501)]
    public async Task AddResult_WhenSuccessful_ReturnsCreatedTestId(
        int appointmentId, bool testResult, string notes, int testId)
    {
        _factory.TestServiceMock.Setup(x =>
                x.AddAsync(It.Is<SaveTestResultDto>(dto =>
                    dto.TestAppointmentID == appointmentId &&
                    dto.TestResult == testResult &&
                    dto.Notes == notes)))
            .ReturnsAsync(Result<int>.Success(testId));

        var request = new SaveTestResultRequest(
            appointmentId, testResult, notes);

        var response = await PostAsync("/api/Tests", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(testId, await response.Content.ReadFromJsonAsync<int>());

        _factory.TestServiceMock.Verify(x =>
            x.AddAsync(It.Is<SaveTestResultDto>(dto =>
                dto.TestAppointmentID == appointmentId &&
                dto.TestResult == testResult &&
                dto.Notes == notes)), Times.Once);
    }

    [Theory]
    [InlineData("Validation", "Invalid test result.", 400, "Validation error")]
    [InlineData("NotFound", "Appointment not found.", 404, "Resource not found")]
    [InlineData("Conflict", "Appointment is already locked.", 409, "Conflict")]
    [InlineData("Forbidden", "You must be logged in first.", 403, "Forbidden")]
    public async Task AddResult_WhenServiceReturnsError_ReturnsProblemDetails(
        string type, string detail, int status, string title)
    {
        var result = type switch
        {
            "Validation" => Result<int>.FromValidationFailure(detail),
            "NotFound" => Result<int>.FromNotFound(detail),
            "Conflict" => Result<int>.FromConflict(detail),
            _ => Result<int>.FromForbidden(detail)
        };

        _factory.TestServiceMock.Setup(x =>
                x.AddAsync(It.IsAny<SaveTestResultDto>()))
            .ReturnsAsync(result);

        var response = await PostAsync(
            "/api/Tests",
            new SaveTestResultRequest(100, true, null));

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task AddResult_WhenFailure_Returns500ProblemDetails()
    {
        _factory.TestServiceMock.Setup(x =>
                x.AddAsync(It.IsAny<SaveTestResultDto>()))
            .ReturnsAsync(Result<int>.FromFailure("Failed to save test result."));

        var response = await PostAsync(
            "/api/Tests",
            new SaveTestResultRequest(100, true, null));

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    private Task<HttpResponseMessage> GetAsync(string url) =>
        SendAsync(HttpMethod.Get, url);

    private Task<HttpResponseMessage> PostAsync(string url, object body) =>
        SendAsync(HttpMethod.Post, url, body);

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Test-User-Id", "1");
        request.Headers.Add("X-Test-Username", "testuser");
        request.Headers.Add("X-Test-FullName", "Test User");
        request.Headers.Add("X-Test-Role", "Staff");

        if (body is not null)
            request.Content = JsonContent.Create(body);

        return await _client.SendAsync(request);
    }

    private static TestDto CreateTestDto(int testId) => new()
    {
        TestID = testId,
        TestAppointmentID = 100,
        TestResult = true,
        Notes = "Passed successfully",
        CreatedByUserID = 1,
        CreatedByUserName = "testuser",
        TestTypeName = "Theory",
        AppointmentDate = new DateTime(2026, 10, 1, 10, 30, 0)
    };

    private static void AssertTestResponse(TestDto dto, TestResponse response)
    {
        Assert.Equal(dto.TestID, response.TestId);
        Assert.Equal(dto.TestAppointmentID, response.TestAppointmentId);
        Assert.Equal(dto.TestResult, response.TestResult);
        Assert.Equal(dto.Notes, response.Notes);
        Assert.Equal(dto.CreatedByUserID, response.CreatedByUserId);
        Assert.Equal(dto.CreatedByUserName, response.CreatedByUserName);
        Assert.Equal(dto.TestTypeName, response.TestTypeName);
        Assert.Equal(dto.AppointmentDate, response.AppointmentDate);
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
