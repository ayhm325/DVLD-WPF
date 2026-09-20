using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.TestAppointment;
using DVLD.Contracts.TestWorkflow;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/[controller]")]
public sealed class TestWorkflowController(
    ITestWorkflowService service) : ControllerBase
{
    [HttpGet("can-schedule")]
    public async Task<IActionResult> CanSchedule(
        [FromQuery] int localAppId,
        [FromQuery] TestType testType)
    {
        var result =
            await service.CanScheduleTestAsync(
                localAppId,
                ToDomainTestType(testType));

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            new TestWorkflowResponse(
                Allowed: true,
                Error: null,
                ErrorType: null,
                NextTestType: null));
    }

    [HttpGet("next-test/{localAppId:int}")]
    public async Task<IActionResult> GetNextTest(
        int localAppId)
    {
        var result =
            await service.GetNextTestTypeAsync(
                localAppId);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            new TestWorkflowResponse(
                Allowed: true,
                Error: null,
                ErrorType: null,
                NextTestType: (int)result.Value));
    }

    [HttpGet("can-take/{appointmentId:int}")]
    public async Task<IActionResult> CanTake(
        int appointmentId)
    {
        var result =
            await service.CanTakeTestAsync(
                appointmentId);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            new TestWorkflowResponse(
                Allowed: true,
                Error: null,
                ErrorType: null,
                NextTestType: null));
    }

    private static TestTypeEnum ToDomainTestType(
        TestType testType)
        => testType switch
        {
            TestType.Theory =>
                TestTypeEnum.Theory,

            TestType.Written =>
                TestTypeEnum.Written,

            TestType.Practical =>
                TestTypeEnum.Practical,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(testType))
        };
}