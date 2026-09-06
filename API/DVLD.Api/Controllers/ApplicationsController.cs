using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ApplicationsController(
    IApplicationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetAllApplicationsAsync();

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetApplicationByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{id:int}/basic-info")]
    public async Task<IActionResult> GetBasicInfo(int id)
    {
        var result =
            await service.GetBasicInfoAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateApplicationDto dto)
    {
        var result =
            await service.AddNewApplicationAsync(dto);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value },
            new { applicationId = result.Value });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateApplicationDto dto)
    {
        var result =
            await service.UpdateApplicationAsync(dto);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await service.DeleteApplicationAsync(id);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var result =
            await service.CompleteApplicationAsync(id);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result =
            await service.CancelApplicationAsync(id);

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