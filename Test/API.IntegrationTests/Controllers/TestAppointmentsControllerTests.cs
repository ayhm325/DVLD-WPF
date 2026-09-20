using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Domain.Enums;
using DVLD.Contracts.TestAppointment;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.IntegrationTests.Controllers;

public sealed class TestAppointmentsControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TestAppointmentsControllerTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.TestAppointmentServiceMock.Reset();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/TestAppointments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedAppointments()
    {
        var dto = CreateTestAppointmentDto(10);

        _factory.TestAppointmentServiceMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<TestAppointmentDto>>.Success([dto]));

        var response = await GetAsync("/api/TestAppointments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TestAppointmentResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
        AssertTestAppointmentResponse(dto, result[0]);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetAllAsync(), Times.Once);
    }

    [Theory]
    [InlineData("Validation", "Invalid appointment data.", 400, "Validation error")]
    [InlineData("NotFound", "Appointments not found.", 404, "Resource not found")]
    [InlineData("Conflict", "Appointment conflict.", 409, "Conflict")]
    [InlineData("Forbidden", "You must be logged in first.", 403, "Forbidden")]
    public async Task GetAll_WhenServiceReturnsError_ReturnsProblemDetails(
        string type, string detail, int status, string title)
    {
        var result = type switch
        {
            "Validation" => Result<List<TestAppointmentDto>>.FromValidationFailure(detail),
            "NotFound" => Result<List<TestAppointmentDto>>.FromNotFound(detail),
            "Conflict" => Result<List<TestAppointmentDto>>.FromConflict(detail),
            _ => Result<List<TestAppointmentDto>>.FromForbidden(detail)
        };

        _factory.TestAppointmentServiceMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(result);

        var response = await GetAsync("/api/TestAppointments");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task GetAll_WhenFailure_Returns500ProblemDetails()
    {
        _factory.TestAppointmentServiceMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<TestAppointmentDto>>.FromFailure("Database failure."));

        var response = await GetAsync("/api/TestAppointments");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedAppointment()
    {
        var dto = CreateTestAppointmentDto(20);

        _factory.TestAppointmentServiceMock.Setup(x => x.GetByIdAsync(20))
            .ReturnsAsync(Result<TestAppointmentDto>.Success(dto));

        var response = await GetAsync("/api/TestAppointments/20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TestAppointmentResponse>();
        Assert.NotNull(result);
        AssertTestAppointmentResponse(dto, result);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetByIdAsync(20), Times.Once);
    }

    [Theory]
    [InlineData(20, "NotFound", "Appointment not found.", 404, "Resource not found")]
    [InlineData(0, "Validation", "Invalid appointment ID.", 400, "Validation error")]
    public async Task GetById_WhenServiceReturnsError_ReturnsProblemDetails(
        int id, string type, string detail, int status, string title)
    {
        var result = type == "NotFound"
            ? Result<TestAppointmentDto>.FromNotFound(detail)
            : Result<TestAppointmentDto>.FromValidationFailure(detail);

        _factory.TestAppointmentServiceMock.Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync(result);

        var response = await GetAsync($"/api/TestAppointments/{id}");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task GetByLocalApplication_WhenSuccessful_ReturnsMappedAppointments()
    {
        var dto = CreateTestAppointmentDto(30);

        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetByLocalDrivingLicenseApplicationIdAsync(50))
            .ReturnsAsync(Result<List<TestAppointmentDto>>.Success([dto]));

        var response = await GetAsync("/api/TestAppointments/local-application/50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TestAppointmentResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
        AssertTestAppointmentResponse(dto, result[0]);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetByLocalDrivingLicenseApplicationIdAsync(50), Times.Once);
    }

    [Fact]
    public async Task GetByLocalApplication_WhenNotFound_ReturnsProblemDetails()
    {
        const string detail = "Appointments not found.";

        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetByLocalDrivingLicenseApplicationIdAsync(50))
            .ReturnsAsync(Result<List<TestAppointmentDto>>.FromNotFound(detail));

        var response = await GetAsync("/api/TestAppointments/local-application/50");

        await AssertProblemDetailsAsync(
            response, HttpStatusCode.NotFound, "Resource not found", detail);
    }

    [Fact]
    public async Task GetByTestType_WhenSuccessful_ReturnsMappedAppointments()
    {
        var dto = CreateTestAppointmentDto(40);

        _factory.TestAppointmentServiceMock.Setup(x => x.GetByTestTypeIdAsync(1))
            .ReturnsAsync(Result<List<TestAppointmentDto>>.Success([dto]));

        var response = await GetAsync("/api/TestAppointments/test-type/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TestAppointmentResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
        AssertTestAppointmentResponse(dto, result[0]);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetByTestTypeIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetByTestType_WhenValidationFailure_ReturnsProblemDetails()
    {
        const string detail = "Invalid test type.";

        _factory.TestAppointmentServiceMock.Setup(x => x.GetByTestTypeIdAsync(1))
            .ReturnsAsync(Result<List<TestAppointmentDto>>.FromValidationFailure(detail));

        var response = await GetAsync("/api/TestAppointments/test-type/1");

        await AssertProblemDetailsAsync(
            response, HttpStatusCode.BadRequest, "Validation error", detail);
    }

    [Fact]
    public async Task GetSchedulePreparation_WhenSuccessful_ReturnsMappedSchedule()
    {
        var dto = CreateScheduleTestDto();

        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetSchedulePreparationAsync(60, 1))
            .ReturnsAsync(Result<ScheduleTestDto>.Success(dto));

        var response = await GetAsync(
            "/api/TestAppointments/schedule-preparation/60/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ScheduleTestResponse>();
        Assert.NotNull(result);
        AssertScheduleResponse(dto, result);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetSchedulePreparationAsync(60, 1), Times.Once);
    }

    [Fact]
    public async Task GetSchedulePreparation_WhenValidationFailure_ReturnsProblemDetails()
    {
        const string detail = "Invalid schedule request.";

        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetSchedulePreparationAsync(60, 1))
            .ReturnsAsync(Result<ScheduleTestDto>.FromValidationFailure(detail));

        var response = await GetAsync(
            "/api/TestAppointments/schedule-preparation/60/1");

        await AssertProblemDetailsAsync(
            response, HttpStatusCode.BadRequest, "Validation error", detail);
    }

    [Fact]
    public async Task Schedule_WhenSuccessful_ReturnsNoContent()
    {
        var date = new DateTime(2026, 10, 1, 10, 30, 0);

        _factory.TestAppointmentServiceMock
            .Setup(x => x.ScheduleAsync(70, 1, date))
            .ReturnsAsync(Result<ScheduleTestDto>.Success(CreateScheduleTestDto()));

        var response = await PostAsync(
            "/api/TestAppointments/schedule",
            new ScheduleTestRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 70,
                AppointmentDate = date
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.ScheduleAsync(70, 1, date), Times.Once);
    }

    [Theory]
    [InlineData("Validation", "Invalid appointment date.", 400, "Validation error")]
    [InlineData("Conflict", "Appointment already scheduled.", 409, "Conflict")]
    [InlineData("Forbidden", "You must be logged in first.", 403, "Forbidden")]
    public async Task Schedule_WhenServiceReturnsError_ReturnsProblemDetails(
        string type, string detail, int status, string title)
    {
        var date = new DateTime(2026, 10, 1, 10, 30, 0);

        var result = type switch
        {
            "Validation" => Result<ScheduleTestDto>.FromValidationFailure(detail),
            "Conflict" => Result<ScheduleTestDto>.FromConflict(detail),
            _ => Result<ScheduleTestDto>.FromForbidden(detail)
        };

        _factory.TestAppointmentServiceMock
            .Setup(x => x.ScheduleAsync(70, 1, date))
            .ReturnsAsync(result);

        var response = await PostAsync(
            "/api/TestAppointments/schedule",
            new ScheduleTestRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 70,
                AppointmentDate = date
            });

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task GetByCreatedUser_WhenSuccessful_ReturnsMappedAppointments()
    {
        var dto = CreateTestAppointmentDto(80);

        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetByCreatedUserIdAsync(5))
            .ReturnsAsync(Result<List<TestAppointmentDto>>.Success([dto]));

        var response = await GetAsync("/api/TestAppointments/created-by/5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TestAppointmentResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
        AssertTestAppointmentResponse(dto, result[0]);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetByCreatedUserIdAsync(5), Times.Once);
    }

    [Fact]
    public async Task GetScheduleInfo_WhenSuccessful_ReturnsMappedSchedule()
    {
        var dto = CreateScheduleTestDto();

        _factory.TestAppointmentServiceMock.Setup(x => x.GetScheduleInfoAsync(90))
            .ReturnsAsync(Result<ScheduleTestDto>.Success(dto));

        var response = await GetAsync("/api/TestAppointments/90/schedule-info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ScheduleTestResponse>();
        Assert.NotNull(result);
        AssertScheduleResponse(dto, result);
    }

    [Fact]
    public async Task GetScheduleInfo_WhenNotFound_ReturnsProblemDetails()
    {
        const string detail = "Schedule information not found.";

        _factory.TestAppointmentServiceMock.Setup(x => x.GetScheduleInfoAsync(90))
            .ReturnsAsync(Result<ScheduleTestDto>.FromNotFound(detail));

        var response = await GetAsync("/api/TestAppointments/90/schedule-info");

        await AssertProblemDetailsAsync(
            response, HttpStatusCode.NotFound, "Resource not found", detail);
    }

    [Fact]
    public async Task GetFees_WhenCalled_ReturnsFees()
    {
        _factory.TestAppointmentServiceMock.Setup(x => x.GetTestTypeFeesAsync(1))
            .ReturnsAsync(35m);

        var response = await GetAsync("/api/TestAppointments/fees/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<FeesResponse>();
        Assert.NotNull(result);
        Assert.Equal(35m, result.Fees);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetTestTypeFeesAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetTrialCount_WhenCalled_ReturnsTrialCount()
    {
        _factory.TestAppointmentServiceMock.Setup(x => x.GetTrialCountAsync(100, 1))
            .ReturnsAsync(2);

        var response = await GetAsync(
            "/api/TestAppointments/trial-count?localAppId=100&testTypeId=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TrialCountResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TrialCount);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetTrialCountAsync(100, 1), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsScheduled_ReturnsExpectedValue(bool scheduled)
    {
        _factory.TestAppointmentServiceMock
            .Setup(x => x.IsAppointmentAlreadyScheduledAsync(110, 1))
            .ReturnsAsync(scheduled);

        var response = await GetAsync(
            "/api/TestAppointments/scheduled?localAppId=110&testTypeId=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ScheduledResponse>();
        Assert.NotNull(result);
        Assert.Equal(scheduled, result.Scheduled);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.IsAppointmentAlreadyScheduledAsync(110, 1), Times.Once);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsNoContent()
    {
        var date = new DateTime(2026, 10, 2, 11, 0, 0);

        _factory.TestAppointmentServiceMock.Setup(x =>
                x.AddAsync(It.Is<CreateTestAppointmentDto>(dto =>
                    dto.TestTypeID == 1 &&
                    dto.LocalDrivingLicenseApplicationID == 120 &&
                    dto.AppointmentDate == date &&
                    dto.RetakeTestApplicationID == 500)))
            .ReturnsAsync(Result.Success());

        var response = await PostAsync("/api/TestAppointments",
            new CreateTestAppointmentRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 120,
                AppointmentDate = date,
                RetakeTestApplicationId = 500
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.TestAppointmentServiceMock.Verify(x =>
            x.AddAsync(It.Is<CreateTestAppointmentDto>(dto =>
                dto.TestTypeID == 1 &&
                dto.LocalDrivingLicenseApplicationID == 120 &&
                dto.AppointmentDate == date &&
                dto.RetakeTestApplicationID == 500)), Times.Once);
    }

    [Theory]
    [InlineData("Validation", "Invalid appointment.", 400, "Validation error")]
    [InlineData("Conflict", "Appointment already exists.", 409, "Conflict")]
    [InlineData("Forbidden", "You must be logged in first.", 403, "Forbidden")]
    public async Task Create_WhenServiceReturnsError_ReturnsProblemDetails(
        string type, string detail, int status, string title)
    {
        var result = type switch
        {
            "Validation" => Result.ValidationFailure(detail),
            "Conflict" => Result.Conflict(detail),
            _ => Result.Forbidden(detail)
        };

        _factory.TestAppointmentServiceMock
            .Setup(x => x.AddAsync(It.IsAny<CreateTestAppointmentDto>()))
            .ReturnsAsync(result);

        var response = await PostAsync("/api/TestAppointments",
            new CreateTestAppointmentRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 120,
                AppointmentDate = new DateTime(2026, 10, 2, 11, 0, 0)
            });

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        var date = new DateTime(2026, 11, 1, 12, 30, 0);

        _factory.TestAppointmentServiceMock.Setup(x =>
                x.UpdateAsync(It.Is<UpdateTestAppointmentDto>(dto =>
                    dto.TestAppointmentID == 130 &&
                    dto.AppointmentDate == date)))
            .ReturnsAsync(Result.Success());

        var response = await PutAsync("/api/TestAppointments/130",
            new UpdateTestAppointmentRequest
            {
                TestAppointmentId = 130,
                AppointmentDate = date
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.TestAppointmentServiceMock.Verify(x =>
            x.UpdateAsync(It.Is<UpdateTestAppointmentDto>(dto =>
                dto.TestAppointmentID == 130 &&
                dto.AppointmentDate == date)), Times.Once);
    }

    [Theory]
    [InlineData("NotFound", "Appointment not found.", 404, "Resource not found")]
    [InlineData("Validation", "Invalid appointment date.", 400, "Validation error")]
    public async Task Update_WhenServiceReturnsError_ReturnsProblemDetails(
        string type, string detail, int status, string title)
    {
        var result = type == "NotFound"
            ? Result.NotFound(detail)
            : Result.ValidationFailure(detail);

        _factory.TestAppointmentServiceMock
            .Setup(x => x.UpdateAsync(It.IsAny<UpdateTestAppointmentDto>()))
            .ReturnsAsync(result);

        var response = await PutAsync("/api/TestAppointments/130",
            new UpdateTestAppointmentRequest
            {
                TestAppointmentId = 130,
                AppointmentDate = new DateTime(2026, 11, 1, 12, 30, 0)
            });

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        _factory.TestAppointmentServiceMock.Setup(x => x.DeleteAsync(140))
            .ReturnsAsync(Result.Success());

        var response = await DeleteAsync("/api/TestAppointments/140");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.DeleteAsync(140), Times.Once);
    }

    [Theory]
    [InlineData("NotFound", "Appointment not found.", 404, "Resource not found")]
    [InlineData("Conflict", "Locked appointment cannot be deleted.", 409, "Conflict")]
    public async Task Delete_WhenServiceReturnsError_ReturnsProblemDetails(
        string type, string detail, int status, string title)
    {
        var result = type == "NotFound"
            ? Result.NotFound(detail)
            : Result.Conflict(detail);

        _factory.TestAppointmentServiceMock.Setup(x => x.DeleteAsync(140))
            .ReturnsAsync(result);

        var response = await DeleteAsync("/api/TestAppointments/140");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.DeleteAsync(140), Times.Once);
    }

    private Task<HttpResponseMessage> GetAsync(string url) =>
        SendAsync(HttpMethod.Get, url);

    private Task<HttpResponseMessage> DeleteAsync(string url) =>
        SendAsync(HttpMethod.Delete, url);

    private Task<HttpResponseMessage> PostAsync(string url, object body) =>
        SendAsync(HttpMethod.Post, url, body);

    private Task<HttpResponseMessage> PutAsync(string url, object body) =>
        SendAsync(HttpMethod.Put, url, body);

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

    private static TestAppointmentDto CreateTestAppointmentDto(int id) => new()
    {
        TestAppointmentID = id,
        TestTypeID = 1,
        TestResult = (TestResultType)1,
        CreatedByUserName = "testuser",
        TestTypeName = "Theory",
        LocalDrivingLicenseApplicationID = 100,
        AppointmentDate = new DateTime(2026, 10, 1, 10, 30, 0),
        PaidFees = 35m,
        CreatedByUserID = 1,
        IsLocked = false,
        RetakeTestApplicationID = null
    };

    private static ScheduleTestDto CreateScheduleTestDto() => new()
    {
        AppointmentID = 200,
        RetakeTestApplicationID = null,
        LocalDrivingLicenseApplicationID = 100,
        LicenseClassName = "Private",
        FullName = "Test Person",
        Trial = 1,
        Date = new DateTime(2026, 10, 1, 10, 30, 0),
        Fees = 35m,
        TestTypeID = 1,
        RetakerFees = 25m,
        TestID = 300,
        Result = false,
        Notes = "Test notes"
    };

    private static void AssertTestAppointmentResponse(
        TestAppointmentDto dto,
        TestAppointmentResponse response)
    {
        Assert.Equal(dto.TestAppointmentID, response.TestAppointmentId);
        Assert.Equal(dto.TestTypeID, response.TestTypeId);
        Assert.Equal(
            (DVLD.Contracts.TestAppointment.TestResult)dto.TestResult,
            response.TestResult);
        Assert.Equal(dto.CreatedByUserName, response.CreatedByUserName);
        Assert.Equal(dto.TestTypeName, response.TestTypeName);
        Assert.Equal(dto.LocalDrivingLicenseApplicationID,
            response.LocalDrivingLicenseApplicationId);
        Assert.Equal(dto.AppointmentDate, response.AppointmentDate);
        Assert.Equal(dto.PaidFees, response.PaidFees);
        Assert.Equal(dto.CreatedByUserID, response.CreatedByUserId);
        Assert.Equal(dto.IsLocked, response.IsLocked);
        Assert.Equal(dto.RetakeTestApplicationID,
            response.RetakeTestApplicationId);
        Assert.Equal(dto.TestResultText, response.TestResultText);
        Assert.Equal(dto.Status, response.Status);
        Assert.Equal(dto.AppointmentDateFormatted,
            response.AppointmentDateFormatted);
    }

    private static void AssertScheduleResponse(
        ScheduleTestDto dto,
        ScheduleTestResponse response)
    {
        Assert.Equal(dto.AppointmentID, response.AppointmentId);
        Assert.Equal(dto.RetakeTestApplicationID, response.RetakeTestApplicationId);
        Assert.Equal(dto.LocalDrivingLicenseApplicationID,
            response.LocalDrivingLicenseApplicationId);
        Assert.Equal(dto.LicenseClassName, response.LicenseClassName);
        Assert.Equal(dto.FullName, response.FullName);
        Assert.Equal(dto.Trial, response.Trial);
        Assert.Equal(dto.Date, response.Date);
        Assert.Equal(dto.Fees, response.Fees);
        Assert.Equal(dto.TestTypeID, response.TestTypeId);
        Assert.Equal(dto.RetakerFees, response.RetakerFees);
        Assert.Equal(dto.TestID, response.TestId);
        Assert.Equal(dto.Result, response.Result);
        Assert.Equal(dto.Notes, response.Notes);
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

    private sealed record FeesResponse(decimal Fees);
    private sealed record TrialCountResponse(int TrialCount);
    private sealed record ScheduledResponse(bool Scheduled);
}
