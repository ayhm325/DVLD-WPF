using Application.Common.Results;
using Application.Interfaces;
using DVLD.Contracts.TestAppointment;
using DVLD.Contracts.TestWorkflow;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class TestWorkflowController(
    ITestWorkflowService service)
    : ControllerBase
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
            return HandleFailure(result);

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
            return HandleFailure(result);

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
            return HandleFailure(result);

        return Ok(
            new TestWorkflowResponse(
                Allowed: true,
                Error: null,
                ErrorType: null,
                NextTestType: null));
    }

    private static TestTypeEnum ToDomainTestType(
        TestType testType) =>
        testType switch
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

    private static IActionResult HandleFailure(
        Result result)
    {
        return result.ErrorType switch
        {
            ErrorType.Validation =>
                new BadRequestObjectResult(
                    new TestWorkflowResponse(
                        Allowed: false,
                        Error: result.Error,
                        ErrorType: nameof(ErrorType.Validation),
                        NextTestType: null)),

            ErrorType.NotFound =>
                new NotFoundObjectResult(
                    new TestWorkflowResponse(
                        Allowed: false,
                        Error: result.Error,
                        ErrorType: nameof(ErrorType.NotFound),
                        NextTestType: null)),

            ErrorType.Conflict =>
                new ConflictObjectResult(
                    new TestWorkflowResponse(
                        Allowed: false,
                        Error: result.Error,
                        ErrorType: nameof(ErrorType.Conflict),
                        NextTestType: null)),

            ErrorType.Forbidden =>
                new ObjectResult(
                    new TestWorkflowResponse(
                        Allowed: false,
                        Error: result.Error,
                        ErrorType: nameof(ErrorType.Forbidden),
                        NextTestType: null))
                {
                    StatusCode =
                        StatusCodes.Status403Forbidden
                },

            _ =>
                new ObjectResult(
                    new TestWorkflowResponse(
                        Allowed: false,
                        Error: result.Error,
                        ErrorType: nameof(ErrorType.Failure),
                        NextTestType: null))
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                }
        };
    }
}