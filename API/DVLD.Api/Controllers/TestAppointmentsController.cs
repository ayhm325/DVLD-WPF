using Application.DTOs.TestAppointmentDTO;
using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.TestAppointment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/[controller]")]
public sealed class TestAppointmentsController(
    ITestAppointmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await service.GetAllAsync();

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            result.Value!
                .Select(MapToResponse)
                .ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetByIdAsync(id);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            MapToResponse(result.Value!));
    }

    [HttpGet("local-application/{localAppId:int}")]
    public async Task<IActionResult> GetByLocalApplication(
        int localAppId)
    {
        var result =
            await service
                .GetByLocalDrivingLicenseApplicationIdAsync(
                    localAppId);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            result.Value!
                .Select(MapToResponse)
                .ToList());
    }

    [HttpGet("test-type/{testType:int}")]
    public async Task<IActionResult> GetByTestType(
        int testType)
    {
        var result =
            await service.GetByTestTypeIdAsync(testType);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            result.Value!
                .Select(MapToResponse)
                .ToList());
    }

    [HttpGet("schedule-preparation/{localAppId:int}/{testTypeId:int}")]
    public async Task<IActionResult> GetSchedulePreparation(
        int localAppId,
        int testTypeId)
    {
        var result =
            await service.GetSchedulePreparationAsync(
                localAppId,
                testTypeId);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            MapToScheduleResponse(result.Value!));
    }

    [HttpPost("schedule")]
    public async Task<IActionResult> Schedule(
        [FromBody]
        ScheduleTestRequest request)
    {
        var result =
            await service.ScheduleAsync(
                request.LocalDrivingLicenseApplicationId,
                request.TestTypeId,
                Application.Validators.TestAppointmentValidator.ToFacilityTime(request.AppointmentDate));

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    [HttpGet("created-by/{userId:int}")]
    public async Task<IActionResult> GetByCreatedUser(
        int userId)
    {
        var result =
            await service.GetByCreatedUserIdAsync(userId);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            result.Value!
                .Select(MapToResponse)
                .ToList());
    }

    [HttpGet("{appointmentId:int}/schedule-info")]
    public async Task<IActionResult> GetScheduleInfo(
        int appointmentId)
    {
        var result =
            await service.GetScheduleInfoAsync(
                appointmentId);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            MapToScheduleResponse(result.Value!));
    }

    [HttpGet("fees/{testTypeId:int}")]
    public async Task<IActionResult> GetFees(
        int testTypeId)
    {
        var fees =
            await service.GetTestTypeFeesAsync(
                testTypeId);

        return Ok(
            new
            {
                fees
            });
    }

    [HttpGet("trial-count")]
    public async Task<IActionResult> GetTrialCount(
        [FromQuery] int localAppId,
        [FromQuery] int testTypeId)
    {
        var count =
            await service.GetTrialCountAsync(
                localAppId,
                testTypeId);

        return Ok(
            new
            {
                trialCount = count
            });
    }

    [HttpGet("scheduled")]
    public async Task<IActionResult> IsScheduled(
        [FromQuery] int localAppId,
        [FromQuery] int testTypeId)
    {
        var exists =
            await service
                .IsAppointmentAlreadyScheduledAsync(
                    localAppId,
                    testTypeId);

        return Ok(
            new
            {
                scheduled = exists
            });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody]
        CreateTestAppointmentRequest request)
    {
        var result =
            await service.AddAsync(
                new CreateTestAppointmentDto
                {
                    TestTypeID =
                        request.TestTypeId,

                    LocalDrivingLicenseApplicationID =
                        request.LocalDrivingLicenseApplicationId,

                    AppointmentDate =
                        Application.Validators.TestAppointmentValidator.ToFacilityTime(request.AppointmentDate),

                    RetakeTestApplicationID =
                        request.RetakeTestApplicationId
                });

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody]
        UpdateTestAppointmentRequest request)
    {
        var result =
            await service.UpdateAsync(
                new UpdateTestAppointmentDto
                {
                    TestAppointmentID = id,
                    AppointmentDate =
                        Application.Validators.TestAppointmentValidator.ToFacilityTime(request.AppointmentDate)
                });

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await service.DeleteAsync(id);

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    [HttpPost("{id:int}/cancel")]
    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result =
            await service.CancelAsync(id);

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    [HttpGet("attempts/{localAppId:int}/{testTypeId:int}")]
    public async Task<IActionResult> GetAttemptStats(
        int localAppId,
        int testTypeId,
        CancellationToken cancellationToken)
    {
        var stats = await service.GetAttemptStatsAsync(
            localAppId,
            testTypeId,
            cancellationToken);

        return Ok(new ExaminationAttemptStatsResponse(
            stats.LocalDrivingLicenseApplicationId,
            stats.TestTypeId,
            stats.TotalAttempts,
            stats.PassedAttempts,
            stats.FailedAttempts,
            stats.NoShowAttempts,
            stats.NextTrialNumber));
    }

    private static TestAppointmentResponse MapToResponse(
        TestAppointmentDto dto)
        => new()
        {
            TestAppointmentId =
                dto.TestAppointmentID,

            TestTypeId =
                dto.TestTypeID,

            TestResult =
                (DVLD.Contracts.TestAppointment.TestResult)
                    dto.TestResult,

            CreatedByUserName =
                dto.CreatedByUserName,

            TestTypeName =
                dto.TestTypeName,

            LocalDrivingLicenseApplicationId =
                dto.LocalDrivingLicenseApplicationID,

            ApplicantFullName =
                dto.ApplicantFullName,

            NationalNo =
                dto.NationalNo,

            LicenseClassName =
                dto.LicenseClassName,

            AppointmentDate =
                dto.AppointmentDate,

            PaidFees =
                dto.PaidFees,

            CreatedByUserId =
                dto.CreatedByUserID,

            IsLocked =
                dto.IsLocked,

            RetakeTestApplicationId =
                dto.RetakeTestApplicationID,

            TestResultText =
                dto.TestResultText,

            Status =
                dto.Status,

            AppointmentDateFormatted =
                dto.AppointmentDateFormatted,

            CancelledAt =
                dto.CancelledAt,

            CancelledByUserId =
                dto.CancelledByUserID,

            NoShowDate =
                dto.NoShowDate
        };

    private static ScheduleTestResponse
        MapToScheduleResponse(
            ScheduleTestDto dto)
        => new()
        {
            AppointmentId =
                dto.AppointmentID,

            RetakeTestApplicationId =
                dto.RetakeTestApplicationID,

            LocalDrivingLicenseApplicationId =
                dto.LocalDrivingLicenseApplicationID,

            LicenseClassName =
                dto.LicenseClassName,

            FullName =
                dto.FullName,

            Trial =
                dto.Trial,

            Date =
                dto.Date,

            Fees =
                dto.Fees,

            TestTypeId =
                dto.TestTypeID,

            RetakerFees =
                dto.RetakerFees,

            TestId =
                dto.TestID,

            Result =
                dto.Result,

            Notes =
                dto.Notes
        };

    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] int localAppId,
        [FromQuery] int testTypeId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var result = await service.GetAvailabilityAsync(
            localAppId,
            testTypeId,
            startDate,
            endDate);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(MapToAvailabilityResponse(result.Value!));
    }

    private static AppointmentAvailabilityResponse MapToAvailabilityResponse(
        AppointmentAvailabilityDto dto) =>
        new()
        {
            LocalAppId = dto.LocalAppId,
            TestTypeId = dto.TestTypeId,
            AvailableDays = dto.AvailableDays.Select(d => new AppointmentDayAvailabilityResponse
            {
                Date = d.Date,
                Slots = d.Slots.Select(s => new AppointmentSlotResponse
                {
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                }).ToList()
            }).ToList()
        };
}
