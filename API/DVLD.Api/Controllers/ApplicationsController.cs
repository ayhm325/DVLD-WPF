using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/[controller]")]
public sealed class ApplicationsController(IApplicationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetAllApplicationsAsync();
        if (result.IsFailure) return result.ToActionResult(this);

        return result.Value is null
            ? UnexpectedResult("Application service returned a successful result without applications.")
            : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetApplicationByIdAsync(id);
        if (result.IsFailure) return result.ToActionResult(this);

        return result.Value is null
            ? UnexpectedResult("Application service returned a successful result without an application.")
            : Ok(MapToResponse(result.Value));
    }

    [HttpGet("{id:int}/basic-info")]
    public async Task<IActionResult> GetBasicInfo(int id)
    {
        var result = await service.GetBasicInfoAsync(id);
        if (result.IsFailure) return result.ToActionResult(this);

        return result.Value is null
            ? UnexpectedResult("Application service returned a successful result without application information.")
            : Ok(MapToBasicInfoResponse(result.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateApplicationRequest request)
    {
        var result = await service.AddNewApplicationAsync(new CreateApplicationDto
        {
            ApplicantPersonID = request.ApplicantPersonId,
            ApplicationTypeID = request.ApplicationTypeId
        });

        if (result.IsFailure) return result.ToActionResult(this);

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { applicationId = result.Value });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateApplicationRequest request)
    {
        if (id != request.ApplicationId)
            return Result.ValidationFailure("Route application ID does not match request application ID.").ToActionResult(this);

        var result = await service.UpdateApplicationAsync(new UpdateApplicationDto
        {
            ApplicationID = request.ApplicationId,
            ApplicationTypeID = request.ApplicationTypeId
        });

        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await service.DeleteApplicationAsync(id);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await service.CompleteApplicationAsync(id);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await service.CancelApplicationAsync(id);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }

    private IActionResult UnexpectedResult(string message) => Result.Failure(message).ToActionResult(this);

    private static ApplicationResponse MapToResponse(ApplicationDto dto) => new()
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

    private static ApplicationBasicInfoResponse MapToBasicInfoResponse(ApplicationBasicInfoDto dto) => new()
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