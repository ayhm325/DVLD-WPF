using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.DTOs.TestDTO;
using Application.Interfaces;
using DVLD.Contracts.Test;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class TestsController(
    ITestService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await service.GetAllAsync();

        if (result.IsFailure)
            return HandleFailure(result);

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
            return HandleFailure(result);

        return Ok(
            MapToResponse(result.Value!));
    }

    [HttpGet("appointment/{appointmentId:int}")]
    public async Task<IActionResult> GetByAppointmentId(
        int appointmentId)
    {
        var result =
            await service.GetByTestAppointmentIdAsync(
                appointmentId);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(
            result.Value!
                .Select(MapToResponse)
                .ToList());
    }

    [HttpGet("created-by/{userId:int}")]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var result =
            await service.GetByUserIdAsync(userId);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(
            result.Value!
                .Select(MapToResponse)
                .ToList());
    }

    [HttpPost]
    public async Task<IActionResult> AddResult(
        [FromBody] SaveTestResultRequest request)
    {
        var dto = new SaveTestResultDto
        {
            TestAppointmentID = request.TestAppointmentId,
            TestResult = request.TestResult,
            Notes = request.Notes
        };

        var result =
            await service.AddAsync(dto);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    private static TestResponse MapToResponse(
        TestDto dto) =>
        new(
            TestId: dto.TestID,
            TestAppointmentId: dto.TestAppointmentID,
            TestResult: dto.TestResult,
            Notes: dto.Notes,
            CreatedByUserId: dto.CreatedByUserID,
            CreatedByUserName: dto.CreatedByUserName,
            TestTypeName: dto.TestTypeName,
            AppointmentDate: dto.AppointmentDate);

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
                new ObjectResult(
                    new { error = result.Error })
                {
                    StatusCode =
                        StatusCodes.Status403Forbidden
                },

            _ =>
                new ObjectResult(
                    new { error = result.Error })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                }
        };
    }
}