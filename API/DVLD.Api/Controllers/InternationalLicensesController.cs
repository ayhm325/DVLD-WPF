using Application.Common.Results;
using Application.DTOs.InternationalLicenseDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using DVLD.Contracts.InternationalLicense;
using DVLD.Contracts.License;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/[controller]")]
public sealed class InternationalLicensesController(
    IInternationalService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetAllAsync();
        if (result.IsFailure) return HandleFailure(result);

        return Ok(result.Value!.Select(MapToListResponse).ToList());
    }

    [HttpGet("{internationalLicenseId:int}")]
    public async Task<IActionResult> GetById(int internationalLicenseId)
    {
        var result = await service.GetByIdAsync(internationalLicenseId);
        if (result.IsFailure) return HandleFailure(result);

        return result.Value is null
            ? NotFound(new { error = "International license not found." })
            : Ok(MapToResponse(result.Value));
    }

    [HttpGet("driver/{driverId:int}")]
    public async Task<IActionResult> GetByDriverId(int driverId)
    {
        var result = await service.GetByDriverIdAsync(driverId);
        if (result.IsFailure) return HandleFailure(result);

        return Ok(result.Value!.Select(MapToListResponse).ToList());
    }

    [HttpGet("application/{applicationId:int}")]
    public async Task<IActionResult> GetByApplicationId(int applicationId)
    {
        var result = await service.GetByApplicationIdAsync(applicationId);
        if (result.IsFailure) return HandleFailure(result);

        return result.Value is null
            ? NotFound(new { error = "International license not found." })
            : Ok(MapToResponse(result.Value));
    }

    [HttpGet("license/{localLicenseId:int}")]
    public async Task<IActionResult> GetByLocalLicenseId(int localLicenseId)
    {
        var result = await service.GetByLocalLicenseIdAsync(localLicenseId);
        if (result.IsFailure) return HandleFailure(result);

        return Ok(result.Value!.Select(MapToListResponse).ToList());
    }

    [HttpGet("license/{licenseId:int}/info")]
    public async Task<IActionResult> GetLocalLicenseInfo(int licenseId)
    {
        var result = await service.GetLocalLicenseInfoAsync(licenseId);
        if (result.IsFailure) return HandleFailure(result);

        return result.Value is null
            ? NotFound(new { error = "License not found." })
            : Ok(MapToResponse(result.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Issue(
        [FromBody] IssueInternationalLicenseRequest request)
    {
        var result = await service.IssueInternationalLicenseAsync(
            request.LocalLicenseId);

        if (result.IsFailure) return HandleFailure(result);

        var internationalLicense =
            await service.GetByIdAsync(result.Value);

        if (internationalLicense.IsFailure)
            return HandleFailure(internationalLicense);

        return internationalLicense.Value is null
            ? NotFound(new
            {
                error = "International license was issued but could not be retrieved."
            })
            : Ok(MapToResponse(internationalLicense.Value));
    }

    private static InternationalLicenseResponse MapToResponse(
        InternationalDto dto) => new()
        {
            InternationalLicenseId = dto.InternationalLicenseID,
            ApplicationId = dto.ApplicationID,
            DriverId = dto.DriverID,
            IssuedUsingLocalLicenseId = dto.IssuedUsingLocalLicenseID,
            IssueDate = dto.IssueDate,
            ExpirationDate = dto.ExpirationDate,
            IsActive = dto.IsActive,
            CreatedByUserId = dto.CreatedByUserID,
            PersonId = dto.PersonID,
            FullName = dto.FullName,
            DateOfBirth = dto.DateOfBirth,
            ImagePath = dto.ImagePath,
            NationalNo = dto.NationalNo,
            Gender = dto.Gender,
            Fees = dto.Fees,
            CreatedByUserName = dto.CreatedByUserName
        };

    private static DriverLicenseInfoResponse MapToResponse(
        DriverLicenseInfoDto dto) => new()
        {
            LicenseId = dto.LicenseId,
            LicenseClass = dto.LicenseClass,
            IssueDate = dto.IssueDate,
            ExpirationDate = dto.ExpirationDate,
            IsActive = dto.IsActive,
            IsDetained = dto.IsDetained,
            IssueReason = dto.IssueReason,
            Notes = dto.Notes,
            LicenseClassFees = dto.LicenseClassFees,
            DriverId = dto.DriverId,
            PersonId = dto.PersonID,
            FullName = dto.FullName,
            NationalNo = dto.NationalNo,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            ImagePath = dto.ImagePath
        };

    private static IActionResult HandleFailure(Result result) =>
        result.ErrorType switch
        {
            ErrorType.Validation => new BadRequestObjectResult(
                new { error = result.Error }),

            ErrorType.NotFound => new NotFoundObjectResult(
                new { error = result.Error }),

            ErrorType.Conflict => new ConflictObjectResult(
                new { error = result.Error }),

            ErrorType.Forbidden => new ObjectResult(
                new { error = result.Error })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            _ => new ObjectResult(
                new { error = result.Error })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };

    private static InternationalLicenseListResponse MapToListResponse(
    InternationalDto dto) => new()
    {
        InternationalLicenseId = dto.InternationalLicenseID,
        ApplicationId = dto.ApplicationID,
        DriverId = dto.DriverID,
        IssuedUsingLocalLicenseId = dto.IssuedUsingLocalLicenseID,
        PersonId = dto.PersonID,
        IssueDate = dto.IssueDate,
        ExpirationDate = dto.ExpirationDate,
        IsActive = dto.IsActive
    };
}