using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class TestAppointmentsController(
    ITestAppointmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await service.GetAllAsync();

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("local-application/{localAppId:int}")]
    public async Task<IActionResult> GetByLocalApplication(
        int localAppId)
    {
        var result =
            await service.GetByLocalDrivingLicenseApplicationIdAsync(
                localAppId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("test-type/{testType}")]
    public async Task<IActionResult> GetByTestType(
        TestTypeEnum testType)
    {
        var result =
            await service.GetByTestTypeIdAsync(testType);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("created-by/{userId:int}")]
    public async Task<IActionResult> GetByCreatedUser(
        int userId)
    {
        var result =
            await service.GetByCreatedUserIdAsync(userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{appointmentId:int}/schedule-info")]
    public async Task<IActionResult> GetScheduleInfo(
        int appointmentId)
    {
        var result =
            await service.GetScheduleInfoAsync(appointmentId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("fees/{testTypeId:int}")]
    public async Task<IActionResult> GetFees(int testTypeId)
    {
        var fees =
            await service.GetTestTypeFeesAsync(testTypeId);

        return Ok(new { fees });
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

        return Ok(new { trialCount = count });
    }

    [HttpGet("scheduled")]
    public async Task<IActionResult> IsScheduled(
        [FromQuery] int localAppId,
        [FromQuery] int testTypeId)
    {
        var exists =
            await service.IsAppointmentAlreadyScheduledAsync(
                localAppId,
                testTypeId);

        return Ok(new { scheduled = exists });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTestAppointmentDto dto)
    {
        var result =
            await service.AddAsync(dto);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTestAppointmentDto dto)
    {
        var result =
            await service.UpdateAsync(dto);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await service.DeleteAsync(id);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    private static IActionResult HandleFailure(Result result)
    {
        return result.ErrorType switch
        {
            ErrorType.Validation =>
                new BadRequestObjectResult(
                    new { error = result.Error }),

            ErrorType.NotFound =>
                new NotFoundObjectResult(
                    new { error = result.Error }),

            ErrorType.Conflict =>
                new ConflictObjectResult(
                    new { error = result.Error }),

            ErrorType.Forbidden =>
                new ObjectResult(new { error = result.Error })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                },

            _ =>
                new ObjectResult(new { error = result.Error })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                }
        };
    }
}