using Application.DTOs.TestAppointmentDTO;
using Application.DTOs.TestDTO;
using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.Test;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/[controller]")]
public sealed class TestsController(
    ITestService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetAllAsync();

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
        var result = await service.GetByIdAsync(id);

        if (result.IsFailure)
            return result.ToActionResult(this);

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
            return result.ToActionResult(this);

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
            return result.ToActionResult(this);

        return Ok(
            result.Value!
                .Select(MapToResponse)
                .ToList());
    }

    [HttpPost]
    public async Task<IActionResult> AddResult(
        [FromBody] SaveTestResultRequest request)
    {
        var result = await service.AddAsync(
            new SaveTestResultDto
            {
                TestAppointmentID =
                    request.TestAppointmentId,

                TestResult =
                    request.TestResult,

                Notes =
                    request.Notes
            });

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToActionResult(this);
    }

    private static TestResponse MapToResponse(
        TestDto dto)
        => new(
            TestId: dto.TestID,
            TestAppointmentId: dto.TestAppointmentID,
            TestResult: dto.TestResult,
            Notes: dto.Notes,
            CreatedByUserId: dto.CreatedByUserID,
            CreatedByUserName: dto.CreatedByUserName,
            TestTypeName: dto.TestTypeName,
            AppointmentDate: dto.AppointmentDate);
}