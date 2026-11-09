using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.DriverDTO;
using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.Application;
using DVLD.Contracts.Driver;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/[controller]")]
public sealed class DriversController(IDriverService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DriverQueryRequest request, CancellationToken cancellationToken = default)
    {
        if (Request.Query.ContainsKey("pageNumber") ||
            Request.Query.ContainsKey("pageSize") ||
            Request.Query.ContainsKey("searchQuery") ||
            Request.Query.ContainsKey("searchBy") ||
            Request.Query.ContainsKey("licenseFilter") ||
            Request.Query.ContainsKey("paged"))
        {
            var result = await service.GetAllAsync(request, cancellationToken);

            if (result.IsFailure)
                return result.ToActionResult(this);

            var page = result.Value!;

            return Ok(new
            {
                items = page.Items
                    .Select(MapToListResponse)
                    .ToList(),
                pageNumber = page.PageNumber,
                pageSize = page.PageSize,
                totalCount = page.TotalCount,
                totalPages = page.TotalPages,
                hasPreviousPage = page.HasPreviousPage,
                hasNextPage = page.HasNextPage
            });
        }

        var unpagedResult = await service.GetAllAsync(cancellationToken);

        if (unpagedResult.IsFailure)
            return unpagedResult.ToActionResult(this);

        return unpagedResult.Value is null
            ? UnexpectedResult("Driver service returned a successful result without drivers.")
            : Ok(unpagedResult.Value.Select(MapToListResponse).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return result.Value is null
            ? UnexpectedResult("Driver service returned a successful result without a driver.")
            : Ok(MapToResponse(result.Value));
    }

    [HttpGet("person/{personId:int}")]
    public async Task<IActionResult> GetByPersonId(int personId, CancellationToken cancellationToken = default)
    {
        var result = await service.GetByPersonIdAsync(personId, cancellationToken);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return result.Value is null
            ? UnexpectedResult("Driver service returned a successful result without a driver.")
            : Ok(MapToResponse(result.Value));
    }

    [HttpGet("created-by/{userId:int}")]
    public async Task<IActionResult> GetByCreatedUserId(int userId, CancellationToken cancellationToken = default)
    {
        var result = await service.GetByCreatedUserIdAsync(userId, cancellationToken);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return result.Value is null
            ? UnexpectedResult("Driver service returned a successful result without drivers.")
            : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateDriverRequest request, CancellationToken cancellationToken = default)
    {
        var result = await service.AddAsync(new CreateDriverDto
        {
            PersonID = request.PersonId
        }, cancellationToken);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value },
            new { driverId = result.Value });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id != request.DriverId)
        {
            return Result.ValidationFailure(
                "The route driver id does not match the request driver id.")
                .ToActionResult(this);
        }

        var result = await service.UpdateAsync(new UpdateDriverDto
        {
            DriverID = request.DriverId,
            PersonID = request.PersonId
        }, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var result = await service.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    private IActionResult UnexpectedResult(string message) =>
        Result.Failure(message).ToActionResult(this);

    private static DriverResponse MapToResponse(DriverDto dto) => new()
    {
        DriverId = dto.DriverID,
        PersonId = dto.PersonID,
        FullName = dto.FullName,
        NationalNo = dto.NationalNo,
        DateOfBirth = dto.DateOfBirth,
        Gender = dto.Gender.ToString(),
        ImagePath = dto.ImagePath,
        ActiveLicenses = dto.ActiveLicenses,
        CreatedByUserId = dto.CreatedByUserID,
        CreatedByUserName = dto.CreatedByUserName,
        CreatedDate = dto.CreatedDate
    };

    private static DriverListResponse MapToListResponse(DriverDto dto) => new()
    {
        DriverId = dto.DriverID,
        PersonId = dto.PersonID,
        FullName = dto.FullName,
        ActiveLicenses = dto.ActiveLicenses,
        NationalNo = dto.NationalNo,
        DateOfBirth = dto.DateOfBirth,
        CreatedDate = dto.CreatedDate
    };
}
