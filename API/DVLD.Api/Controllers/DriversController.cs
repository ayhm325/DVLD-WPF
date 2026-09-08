using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.DriverDTO;
using Application.Interfaces;
using DVLD.Contracts.Driver;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class DriversController(
    IDriverService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetAllAsync();

        if (result.IsFailure)
            return HandleFailure(result);

        var response = result.Value!
            .Select(MapToResponse)
            .ToList();

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetByIdAsync(id);

        if (result.IsFailure)
            return HandleFailure(result);

        if (result.Value is null)
            return NotFound(new { error = "Driver not found." });

        return Ok(MapToResponse(result.Value));
    }

    [HttpGet("person/{personId:int}")]
    public async Task<IActionResult> GetByPersonId(
        int personId)
    {
        var result =
            await service.GetByPersonIdAsync(personId);

        if (result.IsFailure)
            return HandleFailure(result);

        if (result.Value is null)
            return NotFound(new { error = "Driver not found." });

        return Ok(MapToResponse(result.Value));
    }

    [HttpGet("created-by/{userId:int}")]
    public async Task<IActionResult> GetByCreatedUserId(
        int userId)
    {
        var result =
            await service.GetByCreatedUserIdAsync(userId);

        if (result.IsFailure)
            return HandleFailure(result);

        var response = result.Value!
            .Select(MapToResponse)
            .ToList();

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDriverRequest request)
    {
        var dto = new CreateDriverDto
        {
            PersonID = request.PersonId
        };

        var result =
            await service.AddAsync(dto);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value },
            new { driverId = result.Value });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateDriverRequest request)
    {
        if (id != request.DriverId)
        {
            return BadRequest(new
            {
                error = "The route driver id does not match the request driver id."
            });
        }

        var dto = new UpdateDriverDto
        {
            DriverID = request.DriverId,
            PersonID = request.PersonId
        };

        var result =
            await service.UpdateAsync(dto);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await service.DeleteAsync(id);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    private static DriverResponse MapToResponse(
        DriverDto dto)
    {
        return new DriverResponse
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
    }

    private static IActionResult HandleFailure(
        Result result)
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
                new ObjectResult(
                    new { error = result.Error })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                },

            _ =>
                new ObjectResult(
                    new { error = result.Error })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                }
        };
    }
}