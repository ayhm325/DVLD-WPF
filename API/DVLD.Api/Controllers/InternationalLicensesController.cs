using Application.DTOs.InternationalLicenseDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.InternationalLicense;
using DVLD.Contracts.License;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/[controller]")]
public sealed class InternationalLicensesController(IInternationalService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetAllAsync();
        if (result.IsFailure) return result.ToActionResult(this);
        return result.Value is null ? UnexpectedResult() : Ok(result.Value.Select(MapToListResponse).ToList());
    }

    [HttpGet("{internationalLicenseId:int}")]
    public async Task<IActionResult> GetById(int internationalLicenseId)
    {
        var result = await service.GetByIdAsync(internationalLicenseId);
        if (result.IsFailure) return result.ToActionResult(this);
        return result.Value is null ? UnexpectedResult() : Ok(MapToResponse(result.Value));
    }

    [HttpGet("driver/{driverId:int}")]
    public async Task<IActionResult> GetByDriverId(int driverId)
    {
        var result = await service.GetByDriverIdAsync(driverId);
        if (result.IsFailure) return result.ToActionResult(this);
        return result.Value is null ? UnexpectedResult() : Ok(result.Value.Select(MapToListResponse).ToList());
    }

    [HttpGet("application/{applicationId:int}")]
    public async Task<IActionResult> GetByApplicationId(int applicationId)
    {
        var result = await service.GetByApplicationIdAsync(applicationId);
        if (result.IsFailure) return result.ToActionResult(this);
        return result.Value is null ? UnexpectedResult() : Ok(MapToResponse(result.Value));
    }

    [HttpGet("license/{localLicenseId:int}")]
    public async Task<IActionResult> GetByLocalLicenseId(int localLicenseId)
    {
        var result = await service.GetByLocalLicenseIdAsync(localLicenseId);
        if (result.IsFailure) return result.ToActionResult(this);
        return result.Value is null ? UnexpectedResult() : Ok(result.Value.Select(MapToListResponse).ToList());
    }

    [HttpGet("license/{licenseId:int}/info")]
    public async Task<IActionResult> GetLocalLicenseInfo(int licenseId)
    {
        var result = await service.GetLocalLicenseInfoAsync(licenseId);
        if (result.IsFailure) return result.ToActionResult(this);
        return result.Value is null ? UnexpectedResult() : Ok(MapToResponse(result.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Issue(IssueInternationalLicenseRequest request)
    {
        var result = await service.IssueInternationalLicenseAsync(request.LocalLicenseId);
        if (result.IsFailure) return result.ToActionResult(this);

        var licenseResult = await service.GetByIdAsync(result.Value);
        if (licenseResult.IsFailure) return licenseResult.ToActionResult(this);

        return licenseResult.Value is null
            ? UnexpectedResult("International license was issued successfully but could not be retrieved.")
            : Ok(MapToResponse(licenseResult.Value));
    }

    private IActionResult UnexpectedResult(
        string message = "International license service returned a successful result without data.") =>
        Application.Common.Results.Result.Failure(message).ToActionResult(this);

    private static InternationalLicenseResponse MapToResponse(InternationalDto dto) => new()
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

    private static DriverLicenseInfoResponse MapToResponse(DriverLicenseInfoDto dto) => new()
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

    private static InternationalLicenseListResponse MapToListResponse(InternationalDto dto) => new()
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