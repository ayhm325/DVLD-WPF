using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using DVLD.Contracts.License;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/[controller]")]
public sealed class LicensesController(ILicenseService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetAllAsync();
        if (result.IsFailure) return HandleFailure(result);

        return result.Value is null
            ? ServerError("License service returned no data.")
            : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        if (result.IsFailure) return HandleFailure(result);

        return result.Value is null
            ? ServerError("License service returned no data.")
            : Ok(MapToResponse(result.Value));
    }

    [HttpGet("driver/{driverId:int}")]
    public async Task<IActionResult> GetByDriverId(int driverId)
    {
        var result = await service.GetByDriverIdAsync(driverId);
        if (result.IsFailure) return HandleFailure(result);

        return result.Value is null
            ? ServerError("License service returned no data.")
            : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("application/{applicationId:int}")]
    public async Task<IActionResult> GetByApplicationId(int applicationId)
    {
        var result = await service.GetByApplicationIdAsync(applicationId);
        if (result.IsFailure) return HandleFailure(result);

        return result.Value is null
            ? ServerError("License service returned no data.")
            : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("license-class/{licenseClassId:int}")]
    public async Task<IActionResult> GetByLicenseClassId(int licenseClassId)
    {
        var result = await service.GetByLicenseClassIdAsync(licenseClassId);
        if (result.IsFailure) return HandleFailure(result);

        return result.Value is null
            ? ServerError("License service returned no data.")
            : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("person/{personId:int}")]
    public async Task<IActionResult> GetByPersonId(int personId)
    {
        var result = await service.GetLicensesByPersonIdAsync(personId);
        if (result.IsFailure) return HandleFailure(result);

        return result.Value is null
            ? ServerError("License service returned no data.")
            : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("local-application/{localAppId:int}/details")]
    public async Task<IActionResult> GetDetails(int localAppId)
    {
        var result = await service.GetDetailsAsync(localAppId);
        if (result.IsFailure) return HandleFailure(result);

        return result.Value is null
            ? ServerError("License service returned no data.")
            : Ok(MapToResponse(result.Value));
    }

    [HttpGet("{licenseId:int}/details")]
    public async Task<IActionResult> GetDetailsById(int licenseId)
    {
        var result = await service.GetLicenseDetailsByIdAsync(licenseId);
        if (result.IsFailure) return HandleFailure(result);

        return result.Value is null
            ? ServerError("License service returned no data.")
            : Ok(MapToResponse(result.Value));
    }

    private static LicenseResponse MapToResponse(LicenseDto dto) => new()
    {
        LicenseId = dto.LicenseID,
        ApplicationId = dto.ApplicationID,
        DriverId = dto.DriverID,
        DriverName = dto.DriverName,
        LicenseClassId = dto.LicenseClassID,
        LicenseClassName = dto.LicenseClassName,
        IssueDate = dto.IssueDate,
        ExpirationDate = dto.ExpirationDate,
        Notes = dto.Notes,
        PaidFees = dto.PaidFees,
        IsActive = dto.IsActive,
        IssueReason = dto.IssueReason,
        IssueReasonText = dto.IssueReasonText,
        CreatedByUserId = dto.CreatedByUserID,
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

            _ => ServerError(result.Error)
        };

    private static IActionResult ServerError(string error) =>
        new ObjectResult(new { error })
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
}