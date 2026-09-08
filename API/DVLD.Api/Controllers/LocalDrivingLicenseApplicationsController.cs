using Application.Common.Results;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.Interfaces;
using DVLD.Contracts.LocalDrivingLicenseApplication;
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
            ? Ok(result.Value!.Select(Map).ToList())
            : HandleFailure(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetLocalDrivingLicenseApplicationByIdAsync(id);

        return result.IsSuccess
            ? Ok(Map(result.Value!))
            : HandleFailure(result);
    }

    [HttpGet("application/{applicationId:int}")]
    public async Task<IActionResult> GetByApplicationId(int applicationId)
    {
        var result =
            await service.GetLocalDrivingLicenseApplicationsByApplicationIdAsync(
                applicationId);

        return result.IsSuccess
            ? Ok(result.Value!.Select(Map).ToList())
            : HandleFailure(result);
    }

    [HttpGet("license-class/{licenseClassId:int}")]
    public async Task<IActionResult> GetByLicenseClassId(int licenseClassId)
    {
        var result =
            await service.GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(
                licenseClassId);

        return result.IsSuccess
            ? Ok(result.Value!.Select(Map).ToList())
            : HandleFailure(result);
    }

    [HttpGet("person/{personId:int}")]
    public async Task<IActionResult> GetByApplicantPersonId(int personId)
    {
        var result =
            await service.GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(
                personId);

        return result.IsSuccess
            ? Ok(result.Value!.Select(Map).ToList())
            : HandleFailure(result);
    }

    [HttpGet("create-info")]
    public async Task<IActionResult> GetCreateInfo()
    {
        var result =
            await service.GetNewLocalDrivingLicenseApplicationFeesAsync();

        return result.IsSuccess
            ? Ok(new CreateLocalDrivingLicenseApplicationInfoResponse
            {
                ApplicationFees = result.Value
            })
            : HandleFailure(result);
    }

    [HttpGet("{localId:int}/application-id")]
    public async Task<IActionResult> GetApplicationId(int localId)
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
                request.ApplicantPersonId,
                request.LicenseClassId);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value },
            new CreateLocalDrivingLicenseApplicationResponse
            {
                LocalDrivingLicenseApplicationId = result.Value
            });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateLocalDrivingLicenseApplicationRequest request)
    {
        var dto = new UpdateLocalDrivingLicenseApplicationDto
        {
            LicenseClassID = request.LicenseClassId
        };

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

    private static LocalDrivingLicenseApplicationResponse Map(
        LocalDrivingLicenseApplicationListDto dto)
        => new()
        {
            LocalDrivingLicenseApplicationId =
                dto.LocalDrivingLicenseApplicationID,

            LicenseClassId =
                dto.LicenseClassID,

            LicenseClassName =
                dto.LicenseClassName,

            NationalNo =
                dto.NationalNo,

            FullName =
                dto.FullName,

            ApplicationDate =
                dto.ApplicationDate,

            PassedTest =
                dto.PassedTest,

            ApplicationStatus =
                dto.ApplicationStatus.ToString(),

            StatusText =
                dto.StatusText,

            ApplicationFees =
                dto.ApplicationFees,

            LicenseClassFees =
                dto.LicenseClassFees,

            HasLicense =
                dto.HasLicense,

            ApplicantPersonId =
                dto.ApplicantPersonID
        };

    private IActionResult HandleFailure<T>(Result<T> result)
        => result.ErrorType switch
        {
            ErrorType.NotFound =>
                NotFound(new { error = result.Error }),

            ErrorType.Validation =>
                BadRequest(new { error = result.Error }),

            ErrorType.Conflict =>
                Conflict(new { error = result.Error }),

            ErrorType.Forbidden =>
                Forbid(),

            _ =>
                StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = result.Error })
        };

    private IActionResult HandleFailure(Result result)
        => result.ErrorType switch
        {
            ErrorType.NotFound =>
                NotFound(new { error = result.Error }),

            ErrorType.Validation =>
                BadRequest(new { error = result.Error }),

            ErrorType.Conflict =>
                Conflict(new { error = result.Error }),

            ErrorType.Forbidden =>
                Forbid(),

            _ =>
                StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = result.Error })
        };
}