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
            x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedAppointments()
    {
        var dto = CreateTestAppointmentDto(10);

        _factory.TestAppointmentServiceMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<TestAppointmentDto>>.Success([dto]));

        var response = await GetAsync("/api/TestAppointments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TestAppointmentResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
        AssertTestAppointmentResponse(dto, result[0]);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
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

        _factory.TestAppointmentServiceMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var response = await GetAsync("/api/TestAppointments");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task GetAll_WhenFailure_Returns500ProblemDetails()
    {
        _factory.TestAppointmentServiceMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
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

        _factory.TestAppointmentServiceMock.Setup(x => x.GetByIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TestAppointmentDto>.Success(dto));

        var response = await GetAsync("/api/TestAppointments/20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TestAppointmentResponse>();
        Assert.NotNull(result);
        AssertTestAppointmentResponse(dto, result);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetByIdAsync(20, It.IsAny<CancellationToken>()), Times.Once);
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

        _factory.TestAppointmentServiceMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.GetByLocalDrivingLicenseApplicationIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<TestAppointmentDto>>.Success([dto]));

        var response = await GetAsync("/api/TestAppointments/local-application/50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TestAppointmentResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
        AssertTestAppointmentResponse(dto, result[0]);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetByLocalDrivingLicenseApplicationIdAsync(50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByLocalApplication_WhenNotFound_ReturnsProblemDetails()
    {
        const string detail = "Appointments not found.";

        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetByLocalDrivingLicenseApplicationIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<TestAppointmentDto>>.FromNotFound(detail));

        var response = await GetAsync("/api/TestAppointments/local-application/50");

        await AssertProblemDetailsAsync(
            response, HttpStatusCode.NotFound, "Resource not found", detail);
    }

    [Fact]
    public async Task GetByTestType_WhenSuccessful_ReturnsMappedAppointments()
    {
        var dto = CreateTestAppointmentDto(40);

        _factory.TestAppointmentServiceMock.Setup(x => x.GetByTestTypeIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<TestAppointmentDto>>.Success([dto]));

        var response = await GetAsync("/api/TestAppointments/test-type/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TestAppointmentResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
        AssertTestAppointmentResponse(dto, result[0]);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetByTestTypeIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByTestType_WhenValidationFailure_ReturnsProblemDetails()
    {
        const string detail = "Invalid test type.";

        _factory.TestAppointmentServiceMock.Setup(x => x.GetByTestTypeIdAsync(1, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.GetSchedulePreparationAsync(60, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ScheduleTestDto>.Success(dto));

        var response = await GetAsync(
            "/api/TestAppointments/schedule-preparation/60/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ScheduleTestResponse>();
        Assert.NotNull(result);
        AssertScheduleResponse(dto, result);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetSchedulePreparationAsync(60, 1, It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task GetAttemptStats_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/TestAppointments/attempts/5/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAttemptStats_WhenSuccessful_ReturnsMappedStats()
    {
        var stats = new Application.DTOs.TestAppointmentDTO.ExaminationAttemptStats(
            LocalDrivingLicenseApplicationId: 5,
            TestTypeId: 1,
            TotalAttempts: 3,
            PassedAttempts: 1,
            FailedAttempts: 1,
            NoShowAttempts: 1,
            NextTrialNumber: 4);

        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAttemptStatsAsync(5, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var response = await GetAsync("/api/TestAppointments/attempts/5/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ExaminationAttemptStatsResponse>();
        Assert.NotNull(result);
        Assert.Equal(5, result.LocalDrivingLicenseApplicationId);
        Assert.Equal(1, result.TestTypeId);
        Assert.Equal(3, result.TotalAttempts);
        Assert.Equal(1, result.PassedAttempts);
        Assert.Equal(1, result.FailedAttempts);
        Assert.Equal(1, result.NoShowAttempts);
        Assert.Equal(4, result.NextTrialNumber);
    }

    [Fact]
    public async Task GetSchedulePreparation_WhenValidationFailure_ReturnsProblemDetails()
    {
        const string detail = "Invalid schedule request.";

        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetSchedulePreparationAsync(60, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ScheduleTestDto>.FromValidationFailure(detail));

        var response = await GetAsync(
            "/api/TestAppointments/schedule-preparation/60/1");

        await AssertProblemDetailsAsync(
            response, HttpStatusCode.BadRequest, "Validation error", detail);
    }

    private static DateTimeOffset ToJordanOffset(DateTime jordanWallClock)
    {
        var unspecified = DateTime.SpecifyKind(jordanWallClock, DateTimeKind.Unspecified);
        return new DateTimeOffset(
            unspecified,
            Application.Common.Time.JordanTimeZoneProvider.TimeZone.GetUtcOffset(unspecified));
    }

    [Fact]
    public async Task Schedule_WhenSuccessful_ReturnsNoContent()
    {
        var date = new DateTime(2026, 10, 1, 10, 30, 0);

        _factory.TestAppointmentServiceMock
            .Setup(x => x.ScheduleAsync(70, 1, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ScheduleTestDto>.Success(CreateScheduleTestDto()));

        var response = await PostAsync(
            "/api/TestAppointments/schedule",
            new ScheduleTestRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 70,
                AppointmentDate = ToJordanOffset(date)
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.ScheduleAsync(70, 1, date, It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(x => x.ScheduleAsync(70, 1, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var response = await PostAsync(
            "/api/TestAppointments/schedule",
            new ScheduleTestRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 70,
                AppointmentDate = ToJordanOffset(date)
            });

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task GetByCreatedUser_WhenSuccessful_ReturnsMappedAppointments()
    {
        var dto = CreateTestAppointmentDto(80);

        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetByCreatedUserIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<TestAppointmentDto>>.Success([dto]));

        var response = await GetAsync("/api/TestAppointments/created-by/5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TestAppointmentResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
        AssertTestAppointmentResponse(dto, result[0]);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetByCreatedUserIdAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetScheduleInfo_WhenSuccessful_ReturnsMappedSchedule()
    {
        var dto = CreateScheduleTestDto();

        _factory.TestAppointmentServiceMock.Setup(x => x.GetScheduleInfoAsync(90, It.IsAny<CancellationToken>()))
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

        _factory.TestAppointmentServiceMock.Setup(x => x.GetScheduleInfoAsync(90, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ScheduleTestDto>.FromNotFound(detail));

        var response = await GetAsync("/api/TestAppointments/90/schedule-info");

        await AssertProblemDetailsAsync(
            response, HttpStatusCode.NotFound, "Resource not found", detail);
    }

    [Fact]
    public async Task GetFees_WhenCalled_ReturnsFees()
    {
        _factory.TestAppointmentServiceMock.Setup(x => x.GetTestTypeFeesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(35m);

        var response = await GetAsync("/api/TestAppointments/fees/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<FeesResponse>();
        Assert.NotNull(result);
        Assert.Equal(35m, result.Fees);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetTestTypeFeesAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTrialCount_WhenCalled_ReturnsTrialCount()
    {
        _factory.TestAppointmentServiceMock.Setup(x => x.GetTrialCountAsync(100, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var response = await GetAsync(
            "/api/TestAppointments/trial-count?localAppId=100&testTypeId=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TrialCountResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TrialCount);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetTrialCountAsync(100, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsScheduled_ReturnsExpectedValue(bool scheduled)
    {
        _factory.TestAppointmentServiceMock
            .Setup(x => x.IsAppointmentAlreadyScheduledAsync(110, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduled);

        var response = await GetAsync(
            "/api/TestAppointments/scheduled?localAppId=110&testTypeId=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ScheduledResponse>();
        Assert.NotNull(result);
        Assert.Equal(scheduled, result.Scheduled);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.IsAppointmentAlreadyScheduledAsync(110, 1, It.IsAny<CancellationToken>()), Times.Once);
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
                    dto.RetakeTestApplicationID == 500), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var response = await PostAsync("/api/TestAppointments",
            new CreateTestAppointmentRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 120,
                AppointmentDate = ToJordanOffset(date),
                RetakeTestApplicationId = 500
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.TestAppointmentServiceMock.Verify(x =>
            x.AddAsync(It.Is<CreateTestAppointmentDto>(dto =>
                dto.TestTypeID == 1 &&
                dto.LocalDrivingLicenseApplicationID == 120 &&
                dto.AppointmentDate == date &&
                dto.RetakeTestApplicationID == 500), It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(x => x.AddAsync(It.IsAny<CreateTestAppointmentDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var response = await PostAsync("/api/TestAppointments",
            new CreateTestAppointmentRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 120,
                AppointmentDate = ToJordanOffset(new DateTime(2026, 10, 2, 11, 0, 0))
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
                    dto.AppointmentDate == date), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var response = await PutAsync("/api/TestAppointments/130",
            new UpdateTestAppointmentRequest
            {
                TestAppointmentId = 130,
                AppointmentDate = ToJordanOffset(date)
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.TestAppointmentServiceMock.Verify(x =>
            x.UpdateAsync(It.Is<UpdateTestAppointmentDto>(dto =>
                dto.TestAppointmentID == 130 &&
                dto.AppointmentDate == date), It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(x => x.UpdateAsync(It.IsAny<UpdateTestAppointmentDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var response = await PutAsync("/api/TestAppointments/130",
            new UpdateTestAppointmentRequest
            {
                TestAppointmentId = 130,
                AppointmentDate = ToJordanOffset(new DateTime(2026, 11, 1, 12, 30, 0))
            });

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        _factory.TestAppointmentServiceMock.Setup(x => x.DeleteAsync(140, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var response = await DeleteAsync("/api/TestAppointments/140");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.DeleteAsync(140, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("NotFound", "Appointment not found.", 404, "Resource not found")]
    [InlineData("Conflict", "Locked appointment cannot be deleted.", 409, "Conflict")]
    [InlineData("Validation", "Appointments can only be cancelled or rescheduled at least 2 days before the scheduled date.", 400, "Validation error")]
    public async Task Delete_WhenServiceReturnsError_ReturnsProblemDetails(
        string type, string detail, int status, string title)
    {
        var result = type switch
        {
            "NotFound" => Result.NotFound(detail),
            "Conflict" => Result.Conflict(detail),
            _ => Result.ValidationFailure(detail)
        };

        _factory.TestAppointmentServiceMock.Setup(x => x.DeleteAsync(140, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var response = await DeleteAsync("/api/TestAppointments/140");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.DeleteAsync(140, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PutAsync("/api/TestAppointments/140/cancel", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _factory.TestAppointmentServiceMock.Verify(
            x => x.CancelAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_WhenSuccessful_ReturnsNoContent()
    {
        _factory.TestAppointmentServiceMock.Setup(x => x.CancelAsync(140, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var response = await PutAsync("/api/TestAppointments/140/cancel");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.CancelAsync(140, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("NotFound", "Appointment not found.", 404, "Resource not found")]
    [InlineData("Conflict", "Appointment has already been cancelled.", 409, "Conflict")]
    [InlineData("ConflictLocked", "Appointment is already locked.", 409, "Conflict")]
    [InlineData("Validation", "Appointments can only be cancelled or rescheduled at least 2 days before the scheduled date.", 400, "Validation error")]
    public async Task Cancel_WhenServiceReturnsError_ReturnsProblemDetails(
        string type, string detail, int status, string title)
    {
        var result = type switch
        {
            "NotFound" => Result.NotFound(detail),
            "Validation" => Result.ValidationFailure(detail),
            _ => Result.Conflict(detail)
        };

        _factory.TestAppointmentServiceMock.Setup(x => x.CancelAsync(140, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var response = await PutAsync("/api/TestAppointments/140/cancel");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.CancelAsync(140, It.IsAny<CancellationToken>()), Times.Once);
    }


    // =========================================================
    // AVAILABILITY TESTS
    // =========================================================

    [Fact]
    public async Task GetAvailability_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            "/api/TestAppointments/availability?localAppId=10&testTypeId=1&startDate=2026-11-08&endDate=2026-11-12");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetAvailabilityAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAvailability_WithInvalidAuthentication_ReturnsUnauthorized()
    {
        var response = await GetWithInvalidAuthAsync(
            "/api/TestAppointments/availability?localAppId=10&testTypeId=1&startDate=2026-11-08&endDate=2026-11-12");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetAvailabilityAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("User")]
    [InlineData("Driver")]
    public async Task GetAvailability_WithNonStaffRole_ReturnsForbidden(string role)
    {
        var response = await GetWithRoleAsync(
            "/api/TestAppointments/availability?localAppId=10&testTypeId=1&startDate=2026-11-08&endDate=2026-11-12",
            role);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetAvailabilityAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAvailability_WithStaffRole_WhenSuccessful_Returns200WithMappedAvailability()
    {
        var expectedDto = new AppointmentAvailabilityDto
        {
            LocalAppId = 10,
            TestTypeId = 1,
            AvailableDays =
            [
                new AppointmentDayAvailabilityDto
                {
                    Date = "2026-11-08",
                    Slots =
                    [
                        new AppointmentSlotDto { StartTime = "08:00", EndTime = "08:40" },
                        new AppointmentSlotDto { StartTime = "08:40", EndTime = "09:20" }
                    ]
                }
            ]
        };

        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAvailabilityAsync(10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AppointmentAvailabilityDto>.Success(expectedDto));

        var response = await GetAsync(
            "/api/TestAppointments/availability?localAppId=10&testTypeId=1&startDate=2026-11-08&endDate=2026-11-12");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseDto = await response.Content.ReadFromJsonAsync<AppointmentAvailabilityResponse>();
        Assert.NotNull(responseDto);
        Assert.Equal(10, responseDto.LocalAppId);
        Assert.Equal(1, responseDto.TestTypeId);
        Assert.Single(responseDto.AvailableDays);
        Assert.Equal("2026-11-08", responseDto.AvailableDays[0].Date);
        Assert.Equal(2, responseDto.AvailableDays[0].Slots.Count);
        Assert.Equal("08:00", responseDto.AvailableDays[0].Slots[0].StartTime);
        Assert.Equal("08:40", responseDto.AvailableDays[0].Slots[0].EndTime);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetAvailabilityAsync(10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("testTypeId=1&startDate=2026-11-08&endDate=2026-11-12", "Invalid local driving license application ID.")]
    [InlineData("localAppId=0&testTypeId=1&startDate=2026-11-08&endDate=2026-11-12", "Invalid local driving license application ID.")]
    [InlineData("localAppId=-3&testTypeId=1&startDate=2026-11-08&endDate=2026-11-12", "Invalid local driving license application ID.")]
    [InlineData("localAppId=10&startDate=2026-11-08&endDate=2026-11-12", "Test type is required.")]
    [InlineData("localAppId=10&testTypeId=0&startDate=2026-11-08&endDate=2026-11-12", "Test type is required.")]
    [InlineData("localAppId=10&testTypeId=99&startDate=2026-11-08&endDate=2026-11-12", "Invalid test type.")]
    [InlineData("localAppId=10&testTypeId=1&endDate=2026-11-12", "Start date is required.")]
    [InlineData("localAppId=10&testTypeId=1&startDate=2026-11-08", "End date is required.")]
    [InlineData("localAppId=10&testTypeId=1&startDate=2026-11-15&endDate=2026-11-10", "Start date cannot be later than end date.")]
    public async Task GetAvailability_ValidationFailures_ReturnsBadRequestProblemDetails(
        string queryString, string expectedDetail)
    {
        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAvailabilityAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AppointmentAvailabilityDto>.FromValidationFailure(expectedDetail));

        var response = await GetAsync($"/api/TestAppointments/availability?{queryString}");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "Validation error",
            expectedDetail);
    }

    [Fact]
    public async Task GetAvailability_InvalidDateStringFormat_ReturnsBadRequest()
    {
        var response = await GetAsync(
            "/api/TestAppointments/availability?localAppId=10&testTypeId=1&startDate=invalid-date&endDate=2026-11-12");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAvailability_WhenApplicationNotFound_ReturnsNotFoundProblemDetails()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAvailabilityAsync(999, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AppointmentAvailabilityDto>.FromNotFound("Local driving license application not found."));

        var response = await GetAsync(
            "/api/TestAppointments/availability?localAppId=999&testTypeId=1&startDate=2026-11-08&endDate=2026-11-12");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found",
            "Local driving license application not found.");
    }

    [Fact]
    public async Task GetAvailability_WhenWorkflowIneligible_ReturnsConflictProblemDetails()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAvailabilityAsync(10, 2, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AppointmentAvailabilityDto>.FromConflict(
                "The Written test cannot be scheduled yet. The next required test is Theory."));

        var response = await GetAsync(
            "/api/TestAppointments/availability?localAppId=10&testTypeId=2&startDate=2026-11-08&endDate=2026-11-12");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict",
            "The Written test cannot be scheduled yet. The next required test is Theory.");
    }

    [Fact]
    public async Task GetAvailability_WhenAppointmentAlreadyScheduled_ReturnsConflictProblemDetails()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAvailabilityAsync(10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AppointmentAvailabilityDto>.FromConflict(
                "An appointment already exists for this test."));

        var response = await GetAsync(
            "/api/TestAppointments/availability?localAppId=10&testTypeId=1&startDate=2026-11-08&endDate=2026-11-12");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict",
            "An appointment already exists for this test.");
    }

    [Fact]
    public async Task GetAvailability_WhenServiceFails_Returns500ProblemDetails()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAvailabilityAsync(10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AppointmentAvailabilityDto>.FromFailure("Query execution timeout."));

        var response = await GetAsync(
            "/api/TestAppointments/availability?localAppId=10&testTypeId=1&startDate=2026-11-08&endDate=2026-11-12");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetAvailability_WhenEmptyAvailability_Returns200WithEmptyAvailableDaysCollection()
    {
        var emptyDto = new AppointmentAvailabilityDto
        {
            LocalAppId = 10,
            TestTypeId = 1,
            AvailableDays = []
        };

        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAvailabilityAsync(10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AppointmentAvailabilityDto>.Success(emptyDto));

        var response = await GetAsync(
            "/api/TestAppointments/availability?localAppId=10&testTypeId=1&startDate=2026-11-08&endDate=2026-11-12");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("availableDays", out var daysElement));
        Assert.Equal(JsonValueKind.Array, daysElement.ValueKind);
        Assert.Equal(0, daysElement.GetArrayLength());
    }

    [Fact]
    public async Task GetAvailability_ResponseMatchesCamelCaseContractAndHidesSensitiveData()
    {
        var sampleDto = new AppointmentAvailabilityDto
        {
            LocalAppId = 10,
            TestTypeId = 1,
            AvailableDays =
            [
                new AppointmentDayAvailabilityDto
                {
                    Date = "2026-11-08",
                    Slots =
                    [
                        new AppointmentSlotDto { StartTime = "08:00", EndTime = "08:40" }
                    ]
                }
            ]
        };

        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAvailabilityAsync(10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AppointmentAvailabilityDto>.Success(sampleDto));

        var response = await GetAsync(
            "/api/TestAppointments/availability?localAppId=10&testTypeId=1&startDate=2026-11-08&endDate=2026-11-12");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        // Verify exact camelCase contract
        Assert.True(root.TryGetProperty("localAppId", out var localAppIdProp));
        Assert.Equal(10, localAppIdProp.GetInt32());
        Assert.False(root.TryGetProperty("LocalAppId", out _));

        Assert.True(root.TryGetProperty("testTypeId", out var testTypeIdProp));
        Assert.Equal(1, testTypeIdProp.GetInt32());
        Assert.False(root.TryGetProperty("TestTypeId", out _));

        Assert.True(root.TryGetProperty("availableDays", out var daysProp));
        Assert.False(root.TryGetProperty("AvailableDays", out _));
        Assert.Equal(1, daysProp.GetArrayLength());

        var firstDay = daysProp[0];
        Assert.True(firstDay.TryGetProperty("date", out var dateProp));
        Assert.Equal("2026-11-08", dateProp.GetString());
        Assert.False(firstDay.TryGetProperty("Date", out _));

        Assert.True(firstDay.TryGetProperty("slots", out var slotsProp));
        Assert.False(firstDay.TryGetProperty("Slots", out _));
        Assert.Equal(1, slotsProp.GetArrayLength());

        var firstSlot = slotsProp[0];
        Assert.True(firstSlot.TryGetProperty("startTime", out var startProp));
        Assert.Equal("08:00", startProp.GetString());
        Assert.False(firstSlot.TryGetProperty("StartTime", out _));

        Assert.True(firstSlot.TryGetProperty("endTime", out var endProp));
        Assert.Equal("08:40", endProp.GetString());
        Assert.False(firstSlot.TryGetProperty("EndTime", out _));

        // Security check: Verify NO personal or applicant information is leaked
        Assert.False(root.TryGetProperty("fullName", out _));
        Assert.False(root.TryGetProperty("personId", out _));
        Assert.False(root.TryGetProperty("applicantPersonId", out _));
        Assert.False(root.TryGetProperty("nationalNo", out _));
        Assert.False(root.TryGetProperty("phone", out _));
        Assert.False(root.TryGetProperty("email", out _));
        Assert.False(root.TryGetProperty("address", out _));
    }


    private Task<HttpResponseMessage> GetWithRoleAsync(string url, string role)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Test-User-Id", "1");
        request.Headers.Add("X-Test-Username", "testuser");
        request.Headers.Add("X-Test-FullName", "Test User");
        request.Headers.Add("X-Test-Role", role);
        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> GetWithInvalidAuthAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Test-User-Id", "invalid-id");
        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> GetAsync(string url) =>
        SendAsync(HttpMethod.Get, url);

    private Task<HttpResponseMessage> DeleteAsync(string url) =>
        SendAsync(HttpMethod.Delete, url);

    private Task<HttpResponseMessage> PostAsync(string url, object body) =>
        SendAsync(HttpMethod.Post, url, body);

    private Task<HttpResponseMessage> PutAsync(string url, object body) =>
        SendAsync(HttpMethod.Put, url, body);

    private Task<HttpResponseMessage> PutAsync(string url) =>
        SendAsync(HttpMethod.Put, url);

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
