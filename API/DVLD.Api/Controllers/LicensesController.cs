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
    public async Task<IActionResult> GetAll(
        [FromQuery] LicenseQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (Request.Query.ContainsKey("pageNumber") ||
            Request.Query.ContainsKey("pageSize") ||
            Request.Query.ContainsKey("searchQuery") ||
            Request.Query.ContainsKey("searchBy") ||
            Request.Query.ContainsKey("statusFilter") ||
            Request.Query.ContainsKey("paged"))
        {
            var result = await service.GetAllAsync(request, cancellationToken);

            if (result.IsFailure)
                return result.ToActionResult(this);

            var page = result.Value!;

            return Ok(new
            {
                items = page.Items
                    .Select(MapToResponse)
                    .ToList(),
                pageNumber = page.PageNumber,
                pageSize = page.PageSize,
                totalCount = page.TotalCount,
                totalPages = page.TotalPages,
                hasPreviousPage = page.HasPreviousPage,
                hasNextPage = page.HasNextPage
            });
        }

        var unpagedResult = request.IsExpired.HasValue
            ? await service.GetAllAsync(request.IsExpired.Value, cancellationToken)
            : await service.GetAllAsync(cancellationToken);

        return unpagedResult.IsFailure
            ? unpagedResult.ToActionResult(this)
            : unpagedResult.Value is null
                ? UnexpectedResult()
                : Ok(unpagedResult.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("renewal-summary")]
    public async Task<IActionResult> GetRenewalSummary(CancellationToken cancellationToken = default)
    {
        var result = await service.GetRenewalSummaryAsync(cancellationToken);
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(new
                {
                    totalExpired = result.Value.TotalExpired,
                    eligibleForRenewal = result.Value.EligibleForRenewal,
                    ineligibleForRenewal = result.Value.IneligibleForRenewal
                });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(MapToResponse(result.Value));
    }

    [HttpGet("driver/{driverId:int}")]
    public async Task<IActionResult> GetByDriverId(int driverId, CancellationToken cancellationToken = default)
    {
        var result = await service.GetByDriverIdAsync(driverId, cancellationToken);
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("application/{applicationId:int}")]
    public async Task<IActionResult> GetByApplicationId(int applicationId, CancellationToken cancellationToken = default)
    {
        var result = await service.GetByApplicationIdAsync(applicationId, cancellationToken);
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("license-class/{licenseClassId:int}")]
    public async Task<IActionResult> GetByLicenseClassId(int licenseClassId, CancellationToken cancellationToken = default)
    {
        var result = await service.GetByLicenseClassIdAsync(licenseClassId, cancellationToken);
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("person/{personId:int}")]
    public async Task<IActionResult> GetByPersonId(int personId, CancellationToken cancellationToken = default)
    {
        var result = await service.GetLicensesByPersonIdAsync(personId, cancellationToken);
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("local-application/{localAppId:int}/details")]
    public async Task<IActionResult> GetDetails(int localAppId, CancellationToken cancellationToken = default)
    {
        var result = await service.GetDetailsAsync(localAppId, cancellationToken);
        return result.IsFailure
            ? result.ToActionResult(this)
            : result.Value is null
                ? UnexpectedResult()
                : Ok(MapToResponse(result.Value));
    }

    [HttpGet("{licenseId:int}/details")]
    public async Task<IActionResult> GetDetailsById(int licenseId, CancellationToken cancellationToken = default)
    {
        var result = await service.GetLicenseDetailsByIdAsync(licenseId, cancellationToken);
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
        IsExpired = dto.IsExpired,
        IsEligibleForRenewal = dto.IsEligibleForRenewal,
        IneligibilityReason = dto.IneligibilityReason,
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
            IsExpired = dto.IsExpired,
            IsEligibleForRenewal = dto.IsEligibleForRenewal,
            IneligibilityReason = dto.IneligibilityReason,
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
