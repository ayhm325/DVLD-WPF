using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.Interfaces;
using DVLD.Contracts.Application;
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

        if (result.IsFailure)
            return HandleFailure(result);

        var response = result.Value!
            .Select(MapToResponse)
            .ToList();

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetApplicationByIdAsync(id);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(MapToResponse(result.Value!));
    }

    [HttpGet("{id:int}/basic-info")]
    public async Task<IActionResult> GetBasicInfo(int id)
    {
        var result = await service.GetBasicInfoAsync(id);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(MapToBasicInfoResponse(result.Value!));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateApplicationRequest request)
    {
        var dto = new CreateApplicationDto
        {
            ApplicantPersonID = request.ApplicantPersonId,
            ApplicationTypeID = request.ApplicationTypeId
        };

        var result = await service.AddNewApplicationAsync(dto);

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
        [FromBody] UpdateApplicationRequest request)
    {
        if (id != request.ApplicationId)
        {
            return BadRequest(new
            {
                error = "Route application ID does not match request application ID."
            });
        }

        var dto = new UpdateApplicationDto
        {
            ApplicationID = request.ApplicationId,
            ApplicationTypeID = request.ApplicationTypeId
        };

        var result = await service.UpdateApplicationAsync(dto);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await service.DeleteApplicationAsync(id);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await service.CompleteApplicationAsync(id);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await service.CancelApplicationAsync(id);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    private static ApplicationResponse MapToResponse(
        ApplicationDto dto)
    {
        return new ApplicationResponse
        {
            ApplicationId = dto.ApplicationID,
            ApplicantPersonId = dto.ApplicantPersonID,
            ApplicationDate = dto.ApplicationDate,
            ApplicationTypeId = dto.ApplicationTypeID,
            ApplicationStatus = dto.ApplicationStatus.ToString(),
            StatusText = dto.StatusText,
            LastStatusDate = dto.LastStatusDate,
            PaidFees = dto.PaidFees,
            CreatedByUserId = dto.CreatedByUserID,
            CreatedByUserName = dto.CreatedByUserName
        };
    }

    private static ApplicationBasicInfoResponse MapToBasicInfoResponse(
        ApplicationBasicInfoDto dto)
    {
        return new ApplicationBasicInfoResponse
        {
            ApplicantPersonId = dto.ApplicantPersonID,
            ApplicationId = dto.ApplicationID,
            ApplicationStatus = dto.ApplicationStatus.ToString(),
            StatusText = dto.StatusText,
            PaidFees = dto.PaidFees,
            ApplicationTypeName = dto.ApplicationTypeName,
            ApplicantFullName = dto.ApplicantFullName,
            ApplicationDate = dto.ApplicationDate,
            LastStatusDate = dto.LastStatusDate,
            CreatedByUserName = dto.CreatedByUserName
        };
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
