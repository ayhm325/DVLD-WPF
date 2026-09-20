using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using DVLD.Api.Results;
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
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(MapToResponse(result.Value));
    }

    [HttpGet("driver/{driverId:int}")]
    public async Task<IActionResult> GetByDriverId(int driverId)
    {
        var result = await service.GetByDriverIdAsync(driverId);
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("application/{applicationId:int}")]
    public async Task<IActionResult> GetByApplicationId(int applicationId)
    {
        var result = await service.GetByApplicationIdAsync(applicationId);
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("license-class/{licenseClassId:int}")]
    public async Task<IActionResult> GetByLicenseClassId(int licenseClassId)
    {
        var result = await service.GetByLicenseClassIdAsync(licenseClassId);
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("person/{personId:int}")]
    public async Task<IActionResult> GetByPersonId(int personId)
    {
        var result = await service.GetLicensesByPersonIdAsync(personId);
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("local-application/{localAppId:int}/details")]
    public async Task<IActionResult> GetDetails(int localAppId)
    {
        var result = await service.GetDetailsAsync(localAppId);
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(MapToResponse(result.Value));
    }

    [HttpGet("{licenseId:int}/details")]
    public async Task<IActionResult> GetDetailsById(int licenseId)
    {
        var result = await service.GetLicenseDetailsByIdAsync(licenseId);
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(MapToResponse(result.Value));
    }

    private IActionResult UnexpectedResult() =>
        Result.Failure(
            "License service returned a successful result without data.")
        .ToActionResult(this);

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
}
