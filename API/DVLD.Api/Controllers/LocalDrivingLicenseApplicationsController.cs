using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class LocalDrivingLicenseApplicationsController(
    ILocalDrivingLicenseApplicationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await service.GetAllLocalDrivingLicenseApplicationsAsync();

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetLocalDrivingLicenseApplicationByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("application/{applicationId:int}")]
    public async Task<IActionResult> GetByApplicationId(
        int applicationId)
    {
        var result =
            await service.GetLocalDrivingLicenseApplicationsByApplicationIdAsync(
                applicationId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("license-class/{licenseClassId:int}")]
    public async Task<IActionResult> GetByLicenseClassId(
        int licenseClassId)
    {
        var result =
            await service.GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(
                licenseClassId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("person/{personId:int}")]
    public async Task<IActionResult> GetByApplicantPersonId(
        int personId)
    {
        var result =
            await service.GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(
                personId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{localId:int}/application-id")]
    public async Task<IActionResult> GetApplicationId(
        int localId)
    {
        var result =
            await service.GetApplicationIdByLocalIdAsync(localId);

        return result.IsSuccess
            ? Ok(new { applicationId = result.Value })
            : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateLocalDrivingLicenseApplicationRequest request)
    {
        var result =
            await service.CreateLocalDrivingLicenseApplicationAsync(
                request.Application,
                request.LocalApplication);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value },
            new { localApplicationId = result.Value });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateLocalDrivingLicenseApplicationDto dto)
    {
        var result =
            await service.UpdateLocalDrivingLicenseApplicationAsync(
                id,
                dto);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await service.DeleteLocalDrivingLicenseApplicationAsync(id);

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

public sealed record CreateLocalDrivingLicenseApplicationRequest(
    CreateApplicationDto Application,
    CreateLocalDrivingLicenseApplicationDto LocalApplication);