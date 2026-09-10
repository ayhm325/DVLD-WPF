using System.Net;
using System.Net.Http.Json;
using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.Interfaces;
using Domain.Enums;
using DVLD.Contracts.TestAppointment;
using Moq;

namespace API.IntegrationTests.Controllers;

public sealed class TestAppointmentsControllerTests
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TestAppointmentsControllerTests(
        ApiWebApplicationFactory factory)
    {
        _factory = factory;

        _factory.TestAppointmentServiceMock.Reset();

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
                "/api/TestAppointments");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedAppointments()
    {
        var dto =
            CreateTestAppointmentDto(10);

        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<TestAppointmentDto>>.Success(
                    new List<TestAppointmentDto>
                    {
                        dto
                    }));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<TestAppointmentResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        AssertTestAppointmentResponse(
            dto,
            result[0]);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<TestAppointmentDto>>
                    .FromValidationFailure(
                        "Invalid appointment data."));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid appointment data.");
    }

    [Fact]
    public async Task GetAll_WhenNotFound_ReturnsNotFound()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<TestAppointmentDto>>
                    .FromNotFound(
                        "Appointments not found."));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Appointments not found.");
    }

    [Fact]
    public async Task GetAll_WhenConflict_ReturnsConflict()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<TestAppointmentDto>>
                    .FromConflict(
                        "Appointment conflict."));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments");

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Appointment conflict.");
    }

    [Fact]
    public async Task GetAll_WhenForbidden_ReturnsForbiddenWithErrorBody()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<TestAppointmentDto>>
                    .FromForbidden(
                        "You must be logged in first."));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments");

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
        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<TestAppointmentDto>>
                    .FromFailure(
                        "Database failure."));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments");

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
    public async Task GetById_WhenSuccessful_ReturnsMappedAppointment()
    {
        var dto =
            CreateTestAppointmentDto(20);

        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetByIdAsync(20))
            .ReturnsAsync(
                Result<TestAppointmentDto>.Success(dto));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/20");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<TestAppointmentResponse>();

        Assert.NotNull(result);

        AssertTestAppointmentResponse(
            dto,
            result);

        _factory.TestAppointmentServiceMock.Verify(
            x => x.GetByIdAsync(20),
            Times.Once);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetByIdAsync(20))
            .ReturnsAsync(
                Result<TestAppointmentDto>
                    .FromNotFound(
                        "Appointment not found."));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/20");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Appointment not found.");
    }

    [Fact]
    public async Task GetById_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x => x.GetByIdAsync(0))
            .ReturnsAsync(
                Result<TestAppointmentDto>
                    .FromValidationFailure(
                        "Invalid appointment ID."));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/0");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid appointment ID.");
    }

    // =========================================================
    // GET BY LOCAL APPLICATION
    // =========================================================

    [Fact]
    public async Task GetByLocalApplication_WhenSuccessful_ReturnsMappedAppointments()
    {
        var dto =
            CreateTestAppointmentDto(30);

        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.GetByLocalDrivingLicenseApplicationIdAsync(50))
            .ReturnsAsync(
                Result<List<TestAppointmentDto>>.Success(
                    new List<TestAppointmentDto>
                    {
                        dto
                    }));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/local-application/50");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<TestAppointmentResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        AssertTestAppointmentResponse(
            dto,
            result[0]);

        _factory.TestAppointmentServiceMock.Verify(
            x =>
                x.GetByLocalDrivingLicenseApplicationIdAsync(50),
            Times.Once);
    }

    [Fact]
    public async Task GetByLocalApplication_WhenNotFound_ReturnsNotFound()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.GetByLocalDrivingLicenseApplicationIdAsync(50))
            .ReturnsAsync(
                Result<List<TestAppointmentDto>>
                    .FromNotFound(
                        "Appointments not found."));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/local-application/50");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Appointments not found.");
    }

    // =========================================================
    // GET BY TEST TYPE
    // =========================================================

    [Fact]
    public async Task GetByTestType_WhenSuccessful_ReturnsMappedAppointments()
    {
        var dto =
            CreateTestAppointmentDto(40);

        var testType =
            (TestTypeEnum)1;

        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.GetByTestTypeIdAsync(testType))
            .ReturnsAsync(
                Result<List<TestAppointmentDto>>.Success(
                    new List<TestAppointmentDto>
                    {
                        dto
                    }));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/test-type/1");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<TestAppointmentResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        AssertTestAppointmentResponse(
            dto,
            result[0]);

        _factory.TestAppointmentServiceMock.Verify(
            x =>
                x.GetByTestTypeIdAsync(testType),
            Times.Once);
    }

    [Fact]
    public async Task GetByTestType_WhenFailure_ReturnsMappedFailure()
    {
        var testType =
            (TestTypeEnum)1;

        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.GetByTestTypeIdAsync(testType))
            .ReturnsAsync(
                Result<List<TestAppointmentDto>>
                    .FromValidationFailure(
                        "Invalid test type."));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/test-type/1");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid test type.");
    }

    // =========================================================
    // SCHEDULE PREPARATION
    // =========================================================

    [Fact]
    public async Task GetSchedulePreparation_WhenSuccessful_ReturnsMappedSchedule()
    {
        var dto =
            CreateScheduleTestDto();

        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.GetSchedulePreparationAsync(
                    60,
                    1))
            .ReturnsAsync(
                Result<ScheduleTestDto>.Success(dto));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/schedule-preparation/60/1");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<ScheduleTestResponse>();

        Assert.NotNull(result);

        AssertScheduleResponse(
            dto,
            result);

        _factory.TestAppointmentServiceMock.Verify(
            x =>
                x.GetSchedulePreparationAsync(
                    60,
                    1),
            Times.Once);
    }

    [Fact]
    public async Task GetSchedulePreparation_WhenFailure_ReturnsMappedFailure()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.GetSchedulePreparationAsync(
                    60,
                    1))
            .ReturnsAsync(
                Result<ScheduleTestDto>
                    .FromValidationFailure(
                        "Invalid schedule request."));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/schedule-preparation/60/1");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid schedule request.");
    }

    // =========================================================
    // SCHEDULE
    // =========================================================

    [Fact]
    public async Task Schedule_WhenSuccessful_ReturnsNoContent()
    {
        var appointmentDate =
            new DateTime(
                2026,
                10,
                1,
                10,
                30,
                0);

        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.ScheduleAsync(
                    70,
                    1,
                    appointmentDate))
            .ReturnsAsync(
                Result<ScheduleTestDto>.Success(
                    CreateScheduleTestDto()));

        var request =
            new ScheduleTestRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 70,
                AppointmentDate = appointmentDate
            };

        var response =
            await PostAuthenticatedAsync(
                "/api/TestAppointments/schedule",
                request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        _factory.TestAppointmentServiceMock.Verify(
            x =>
                x.ScheduleAsync(
                    70,
                    1,
                    appointmentDate),
            Times.Once);
    }

    [Fact]
    public async Task Schedule_WhenValidationFailure_ReturnsBadRequest()
    {
        var appointmentDate =
            new DateTime(
                2026,
                10,
                1,
                10,
                30,
                0);

        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.ScheduleAsync(
                    70,
                    1,
                    appointmentDate))
            .ReturnsAsync(
                Result<ScheduleTestDto>
                    .FromValidationFailure(
                        "Invalid appointment date."));

        var request =
            new ScheduleTestRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 70,
                AppointmentDate = appointmentDate
            };

        var response =
            await PostAuthenticatedAsync(
                "/api/TestAppointments/schedule",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid appointment date.");
    }

    [Fact]
    public async Task Schedule_WhenConflict_ReturnsConflict()
    {
        var appointmentDate =
            new DateTime(
                2026,
                10,
                1,
                10,
                30,
                0);

        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.ScheduleAsync(
                    70,
                    1,
                    appointmentDate))
            .ReturnsAsync(
                Result<ScheduleTestDto>
                    .FromConflict(
                        "Appointment already scheduled."));

        var request =
            new ScheduleTestRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 70,
                AppointmentDate = appointmentDate
            };

        var response =
            await PostAuthenticatedAsync(
                "/api/TestAppointments/schedule",
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Appointment already scheduled.");
    }

    [Fact]
    public async Task Schedule_WhenForbidden_ReturnsForbiddenWithErrorBody()
    {
        var appointmentDate =
            new DateTime(
                2026,
                10,
                1,
                10,
                30,
                0);

        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.ScheduleAsync(
                    70,
                    1,
                    appointmentDate))
            .ReturnsAsync(
                Result<ScheduleTestDto>
                    .FromForbidden(
                        "You must be logged in first."));

        var request =
            new ScheduleTestRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 70,
                AppointmentDate = appointmentDate
            };

        var response =
            await PostAuthenticatedAsync(
                "/api/TestAppointments/schedule",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "You must be logged in first.");
    }

    // =========================================================
    // GET BY CREATED USER
    // =========================================================

    [Fact]
    public async Task GetByCreatedUser_WhenSuccessful_ReturnsMappedAppointments()
    {
        var dto =
            CreateTestAppointmentDto(80);

        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.GetByCreatedUserIdAsync(5))
            .ReturnsAsync(
                Result<List<TestAppointmentDto>>.Success(
                    new List<TestAppointmentDto>
                    {
                        dto
                    }));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/created-by/5");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<TestAppointmentResponse>>();

        Assert.NotNull(result);
        Assert.Single(result);

        AssertTestAppointmentResponse(
            dto,
            result[0]);

        _factory.TestAppointmentServiceMock.Verify(
            x =>
                x.GetByCreatedUserIdAsync(5),
            Times.Once);
    }

    // =========================================================
    // GET SCHEDULE INFO
    // =========================================================

    [Fact]
    public async Task GetScheduleInfo_WhenSuccessful_ReturnsMappedSchedule()
    {
        var dto =
            CreateScheduleTestDto();

        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.GetScheduleInfoAsync(90))
            .ReturnsAsync(
                Result<ScheduleTestDto>.Success(dto));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/90/schedule-info");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<ScheduleTestResponse>();

        Assert.NotNull(result);

        AssertScheduleResponse(
            dto,
            result);
    }

    [Fact]
    public async Task GetScheduleInfo_WhenNotFound_ReturnsNotFound()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.GetScheduleInfoAsync(90))
            .ReturnsAsync(
                Result<ScheduleTestDto>
                    .FromNotFound(
                        "Schedule information not found."));

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/90/schedule-info");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Schedule information not found.");
    }

    // =========================================================
    // FEES
    // =========================================================

    [Fact]
    public async Task GetFees_WhenCalled_ReturnsFees()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.GetTestTypeFeesAsync(1))
            .ReturnsAsync(35m);

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/fees/1");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<FeesResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            35m,
            result.Fees);

        _factory.TestAppointmentServiceMock.Verify(
            x =>
                x.GetTestTypeFeesAsync(1),
            Times.Once);
    }

    // =========================================================
    // TRIAL COUNT
    // =========================================================

    [Fact]
    public async Task GetTrialCount_WhenCalled_ReturnsTrialCount()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.GetTrialCountAsync(
                    100,
                    1))
            .ReturnsAsync(2);

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/trial-count?localAppId=100&testTypeId=1");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<TrialCountResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            2,
            result.TrialCount);

        _factory.TestAppointmentServiceMock.Verify(
            x =>
                x.GetTrialCountAsync(
                    100,
                    1),
            Times.Once);
    }

    // =========================================================
    // IS SCHEDULED
    // =========================================================

    [Fact]
    public async Task IsScheduled_WhenAppointmentExists_ReturnsTrue()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.IsAppointmentAlreadyScheduledAsync(
                    110,
                    1))
            .ReturnsAsync(true);

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/scheduled?localAppId=110&testTypeId=1");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<ScheduledResponse>();

        Assert.NotNull(result);

        Assert.True(
            result.Scheduled);

        _factory.TestAppointmentServiceMock.Verify(
            x =>
                x.IsAppointmentAlreadyScheduledAsync(
                    110,
                    1),
            Times.Once);
    }

    [Fact]
    public async Task IsScheduled_WhenAppointmentDoesNotExist_ReturnsFalse()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.IsAppointmentAlreadyScheduledAsync(
                    110,
                    1))
            .ReturnsAsync(false);

        var response =
            await GetAuthenticatedAsync(
                "/api/TestAppointments/scheduled?localAppId=110&testTypeId=1");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<ScheduledResponse>();

        Assert.NotNull(result);

        Assert.False(
            result.Scheduled);
    }

    // =========================================================
    // CREATE
    // =========================================================

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsNoContent()
    {
        var appointmentDate =
            new DateTime(
                2026,
                10,
                2,
                11,
                0,
                0);

        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.AddAsync(
                    It.Is<CreateTestAppointmentDto>(
                        dto =>
                            dto.TestTypeID == 1 &&
                            dto.LocalDrivingLicenseApplicationID == 120 &&
                            dto.AppointmentDate == appointmentDate &&
                            dto.RetakeTestApplicationID == 500)))
            .ReturnsAsync(
                Result.Success());

        var request =
            new CreateTestAppointmentRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 120,
                AppointmentDate = appointmentDate,
                RetakeTestApplicationId = 500
            };

        var response =
            await PostAuthenticatedAsync(
                "/api/TestAppointments",
                request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        _factory.TestAppointmentServiceMock.Verify(
            x =>
                x.AddAsync(
                    It.Is<CreateTestAppointmentDto>(
                        dto =>
                            dto.TestTypeID == 1 &&
                            dto.LocalDrivingLicenseApplicationID == 120 &&
                            dto.AppointmentDate == appointmentDate &&
                            dto.RetakeTestApplicationID == 500)),
            Times.Once);
    }

    [Fact]
    public async Task Create_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<CreateTestAppointmentDto>()))
            .ReturnsAsync(
                Result.ValidationFailure(
                    "Invalid appointment."));

        var request =
            new CreateTestAppointmentRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 120,
                AppointmentDate =
                    new DateTime(
                        2026,
                        10,
                        2,
                        11,
                        0,
                        0)
            };

        var response =
            await PostAuthenticatedAsync(
                "/api/TestAppointments",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid appointment.");
    }

    [Fact]
    public async Task Create_WhenConflict_ReturnsConflict()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<CreateTestAppointmentDto>()))
            .ReturnsAsync(
                Result.Conflict(
                    "Appointment already exists."));

        var request =
            new CreateTestAppointmentRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 120,
                AppointmentDate =
                    new DateTime(
                        2026,
                        10,
                        2,
                        11,
                        0,
                        0)
            };

        var response =
            await PostAuthenticatedAsync(
                "/api/TestAppointments",
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Appointment already exists.");
    }

    [Fact]
    public async Task Create_WhenForbidden_ReturnsForbiddenWithErrorBody()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<CreateTestAppointmentDto>()))
            .ReturnsAsync(
                Result.Forbidden(
                    "You must be logged in first."));

        var request =
            new CreateTestAppointmentRequest
            {
                TestTypeId = 1,
                LocalDrivingLicenseApplicationId = 120,
                AppointmentDate =
                    new DateTime(
                        2026,
                        10,
                        2,
                        11,
                        0,
                        0)
            };

        var response =
            await PostAuthenticatedAsync(
                "/api/TestAppointments",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "You must be logged in first.");
    }

    // =========================================================
    // UPDATE
    // =========================================================

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        var appointmentDate =
            new DateTime(
                2026,
                11,
                1,
                12,
                30,
                0);

        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.UpdateAsync(
                    It.Is<UpdateTestAppointmentDto>(
                        dto =>
                            dto.TestAppointmentID == 130 &&
                            dto.AppointmentDate == appointmentDate)))
            .ReturnsAsync(
                Result.Success());

        var request =
            new UpdateTestAppointmentRequest
            {
                TestAppointmentId = 130,
                AppointmentDate = appointmentDate
            };

        var response =
            await PutAuthenticatedAsync(
                "/api/TestAppointments/130",
                request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        _factory.TestAppointmentServiceMock.Verify(
            x =>
                x.UpdateAsync(
                    It.Is<UpdateTestAppointmentDto>(
                        dto =>
                            dto.TestAppointmentID == 130 &&
                            dto.AppointmentDate == appointmentDate)),
            Times.Once);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.UpdateAsync(
                    It.IsAny<UpdateTestAppointmentDto>()))
            .ReturnsAsync(
                Result.NotFound(
                    "Appointment not found."));

        var request =
            new UpdateTestAppointmentRequest
            {
                TestAppointmentId = 130,
                AppointmentDate =
                    new DateTime(
                        2026,
                        11,
                        1,
                        12,
                        30,
                        0)
            };

        var response =
            await PutAuthenticatedAsync(
                "/api/TestAppointments/130",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Appointment not found.");
    }

    [Fact]
    public async Task Update_WhenValidationFailure_ReturnsBadRequest()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.UpdateAsync(
                    It.IsAny<UpdateTestAppointmentDto>()))
            .ReturnsAsync(
                Result.ValidationFailure(
                    "Invalid appointment date."));

        var request =
            new UpdateTestAppointmentRequest
            {
                TestAppointmentId = 130,
                AppointmentDate =
                    new DateTime(
                        2026,
                        11,
                        1,
                        12,
                        30,
                        0)
            };

        var response =
            await PutAuthenticatedAsync(
                "/api/TestAppointments/130",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid appointment date.");
    }

    // =========================================================
    // DELETE
    // =========================================================

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.DeleteAsync(140))
            .ReturnsAsync(
                Result.Success());

        var response =
            await DeleteAuthenticatedAsync(
                "/api/TestAppointments/140");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        _factory.TestAppointmentServiceMock.Verify(
            x =>
                x.DeleteAsync(140),
            Times.Once);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.DeleteAsync(140))
            .ReturnsAsync(
                Result.NotFound(
                    "Appointment not found."));

        var response =
            await DeleteAuthenticatedAsync(
                "/api/TestAppointments/140");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Appointment not found.");
    }

    [Fact]
    public async Task Delete_WhenConflict_ReturnsConflict()
    {
        _factory.TestAppointmentServiceMock
            .Setup(x =>
                x.DeleteAsync(140))
            .ReturnsAsync(
                Result.Conflict(
                    "Locked appointment cannot be deleted."));

        var response =
            await DeleteAuthenticatedAsync(
                "/api/TestAppointments/140");

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Locked appointment cannot be deleted.");
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
        object? body = null)
    {
        var request =
            CreateAuthenticatedRequest(
                HttpMethod.Post,
                url);

        if (body is not null)
        {
            request.Content =
                JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PutAuthenticatedAsync(
        string url,
        object body)
    {
        var request =
            CreateAuthenticatedRequest(
                HttpMethod.Put,
                url);

        request.Content =
            JsonContent.Create(body);

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> DeleteAuthenticatedAsync(
        string url)
    {
        var request =
            CreateAuthenticatedRequest(
                HttpMethod.Delete,
                url);

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
    // DTO FACTORIES
    // =========================================================

    private static TestAppointmentDto CreateTestAppointmentDto(
        int appointmentId)
    {
        return new TestAppointmentDto
        {
            TestAppointmentID =
                appointmentId,

            TestTypeID =
                1,

            TestResult =
                (TestResultType)1,

            CreatedByUserName =
                "testuser",

            TestTypeName =
                "Theory",

            LocalDrivingLicenseApplicationID =
                100,

            AppointmentDate =
                new DateTime(
                    2026,
                    10,
                    1,
                    10,
                    30,
                    0),

            PaidFees =
                35m,

            CreatedByUserID =
                1,

            IsLocked =
                false,

            RetakeTestApplicationID =
                null
        };
    }

    private static ScheduleTestDto CreateScheduleTestDto()
    {
        return new ScheduleTestDto
        {
            AppointmentID =
                200,

            RetakeTestApplicationID =
                null,

            LocalDrivingLicenseApplicationID =
                100,

            LicenseClassName =
                "Private",

            FullName =
                "Test Person",

            Trial =
                1,

            Date =
                new DateTime(
                    2026,
                    10,
                    1,
                    10,
                    30,
                    0),

            Fees =
                35m,

            TestTypeID =
                1,

            RetakerFees =
                25m,

            TestID =
                300,

            Result =
                false,

            Notes =
                "Test notes"
        };
    }

    // =========================================================
    // ASSERTIONS
    // =========================================================

    private static void AssertTestAppointmentResponse(
        TestAppointmentDto dto,
        TestAppointmentResponse response)
    {
        Assert.Equal(
            dto.TestAppointmentID,
            response.TestAppointmentId);

        Assert.Equal(
            dto.TestTypeID,
            response.TestTypeId);

        Assert.Equal(
            (DVLD.Contracts.TestAppointment.TestResult)
                dto.TestResult,
            response.TestResult);

        Assert.Equal(
            dto.CreatedByUserName,
            response.CreatedByUserName);

        Assert.Equal(
            dto.TestTypeName,
            response.TestTypeName);

        Assert.Equal(
            dto.LocalDrivingLicenseApplicationID,
            response.LocalDrivingLicenseApplicationId);

        Assert.Equal(
            dto.AppointmentDate,
            response.AppointmentDate);

        Assert.Equal(
            dto.PaidFees,
            response.PaidFees);

        Assert.Equal(
            dto.CreatedByUserID,
            response.CreatedByUserId);

        Assert.Equal(
            dto.IsLocked,
            response.IsLocked);

        Assert.Equal(
            dto.RetakeTestApplicationID,
            response.RetakeTestApplicationId);

        Assert.Equal(
            dto.TestResultText,
            response.TestResultText);

        Assert.Equal(
            dto.Status,
            response.Status);

        Assert.Equal(
            dto.AppointmentDateFormatted,
            response.AppointmentDateFormatted);
    }

    private static void AssertScheduleResponse(
        ScheduleTestDto dto,
        ScheduleTestResponse response)
    {
        Assert.Equal(
            dto.AppointmentID,
            response.AppointmentId);

        Assert.Equal(
            dto.RetakeTestApplicationID,
            response.RetakeTestApplicationId);

        Assert.Equal(
            dto.LocalDrivingLicenseApplicationID,
            response.LocalDrivingLicenseApplicationId);

        Assert.Equal(
            dto.LicenseClassName,
            response.LicenseClassName);

        Assert.Equal(
            dto.FullName,
            response.FullName);

        Assert.Equal(
            dto.Trial,
            response.Trial);

        Assert.Equal(
            dto.Date,
            response.Date);

        Assert.Equal(
            dto.Fees,
            response.Fees);

        Assert.Equal(
            dto.TestTypeID,
            response.TestTypeId);

        Assert.Equal(
            dto.RetakerFees,
            response.RetakerFees);

        Assert.Equal(
            dto.TestID,
            response.TestId);

        Assert.Equal(
            dto.Result,
            response.Result);

        Assert.Equal(
            dto.Notes,
            response.Notes);
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

    // =========================================================
    // RESPONSE TYPES
    // =========================================================

    private sealed record ErrorResponse(
        string Error);

    private sealed record FeesResponse(
        decimal Fees);

    private sealed record TrialCountResponse(
        int TrialCount);

    private sealed record ScheduledResponse(
        bool Scheduled);
}