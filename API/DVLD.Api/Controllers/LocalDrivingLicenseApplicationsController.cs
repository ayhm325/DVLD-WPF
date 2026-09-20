using Application.Common.Pagination;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.Application;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/[controller]")]
public sealed class LocalDrivingLicenseApplicationsController(
    ILocalDrivingLicenseApplicationService service)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationRequest request)
    {
        var result =
            await service
                .GetAllLocalDrivingLicenseApplicationsAsync(
                    request);

        if (result.IsFailure)
            return result.ToActionResult(this);

        var page = result.Value!;

        return Ok(new
        {
            items = page.Items
                .Select(Map)
                .ToList(),

            pageNumber = page.PageNumber,
            pageSize = page.PageSize,
            totalCount = page.TotalCount,
            totalPages = page.TotalPages,
            hasPreviousPage = page.HasPreviousPage,
            hasNextPage = page.HasNextPage
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service
                .GetLocalDrivingLicenseApplicationByIdAsync(id);

        return result.IsSuccess
            ? Ok(Map(result.Value!))
            : result.ToActionResult(this);
    }

    [HttpGet("{localId:int}/application-basic-info")]
    public async Task<IActionResult> GetApplicationBasicInfo(
        int localId)
    {
        var result =
            await service
                .GetApplicationBasicInfoAsync(localId);

        return result.IsSuccess
            ? Ok(MapApplicationBasicInfo(result.Value!))
            : result.ToActionResult(this);
    }

    [HttpGet("application/{applicationId:int}")]
    public async Task<IActionResult> GetByApplicationId(
        int applicationId)
    {
        var result =
            await service
                .GetLocalDrivingLicenseApplicationsByApplicationIdAsync(
                    applicationId);

        return result.IsSuccess
            ? Ok(
                result.Value!
                    .Select(Map)
                    .ToList())
            : result.ToActionResult(this);
    }

    [HttpGet("license-class/{licenseClassId:int}")]
    public async Task<IActionResult> GetByLicenseClassId(
        int licenseClassId)
    {
        var result =
            await service
                .GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(
                    licenseClassId);

        return result.IsSuccess
            ? Ok(
                result.Value!
                    .Select(Map)
                    .ToList())
            : result.ToActionResult(this);
    }

    [HttpGet("person/{personId:int}")]
    public async Task<IActionResult> GetByApplicantPersonId(
        int personId)
    {
        var result =
            await service
                .GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(
                    personId);

        return result.IsSuccess
            ? Ok(
                result.Value!
                    .Select(Map)
                    .ToList())
            : result.ToActionResult(this);
    }

    [HttpGet("create-info")]
    public async Task<IActionResult> GetCreateInfo()
    {
        var result =
            await service
                .GetNewLocalDrivingLicenseApplicationFeesAsync();

        return result.IsSuccess
            ? Ok(
                new CreateLocalDrivingLicenseApplicationInfoResponse
                {
                    ApplicationFees = result.Value
                })
            : result.ToActionResult(this);
    }

    [HttpGet("{localId:int}/application-id")]
    public async Task<IActionResult> GetApplicationId(
        int localId)
    {
        var result =
            await service
                .GetApplicationIdByLocalIdAsync(localId);

        return result.IsSuccess
            ? Ok(new
            {
                applicationId = result.Value
            })
            : result.ToActionResult(this);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody]
        CreateLocalDrivingLicenseApplicationRequest request)
    {
        var result =
            await service
                .CreateLocalDrivingLicenseApplicationAsync(
                    request.ApplicantPersonId,
                    request.LicenseClassId);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = result.Value
            },
            new CreateLocalDrivingLicenseApplicationResponse
            {
                LocalDrivingLicenseApplicationId =
                    result.Value
            });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody]
        UpdateLocalDrivingLicenseApplicationRequest request)
    {
        var result =
            await service
                .UpdateLocalDrivingLicenseApplicationAsync(
                    id,
                    new UpdateLocalDrivingLicenseApplicationDto
                    {
                        LicenseClassID =
                            request.LicenseClassId
                    });

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await service
                .DeleteLocalDrivingLicenseApplicationAsync(id);

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result =
            await service
                .CancelLocalDrivingLicenseApplicationAsync(id);

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
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

    private static ApplicationBasicInfoResponse
        MapApplicationBasicInfo(
            ApplicationBasicInfoDto dto)
        => new()
        {
            ApplicantPersonId =
                dto.ApplicantPersonID,

            ApplicationId =
                dto.ApplicationID,

            ApplicationStatus =
                dto.ApplicationStatus.ToString(),

            StatusText =
                dto.StatusText,

            PaidFees =
                dto.PaidFees,

            ApplicationTypeName =
                dto.ApplicationTypeName,

            ApplicantFullName =
                dto.ApplicantFullName,

            ApplicationDate =
                dto.ApplicationDate,

            LastStatusDate =
                dto.LastStatusDate,

            CreatedByUserName =
                dto.CreatedByUserName
        };
}