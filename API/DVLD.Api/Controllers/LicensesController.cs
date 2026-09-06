using Application.Common.Results;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class LicensesController(
    ILicenseService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await service.GetAllAsync();

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("driver/{driverId:int}")]
    public async Task<IActionResult> GetByDriverId(
        int driverId)
    {
        var result =
            await service.GetByDriverIdAsync(driverId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("application/{applicationId:int}")]
    public async Task<IActionResult> GetByApplicationId(
        int applicationId)
    {
        var result =
            await service.GetByApplicationIdAsync(applicationId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("license-class/{licenseClassId:int}")]
    public async Task<IActionResult> GetByLicenseClassId(
        int licenseClassId)
    {
        var result =
            await service.GetByLicenseClassIdAsync(licenseClassId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("person/{personId:int}")]
    public async Task<IActionResult> GetByPersonId(
        int personId)
    {
        var result =
            await service.GetLicensesByPersonIdAsync(personId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("local-application/{localAppId:int}/details")]
    public async Task<IActionResult> GetDetails(int localAppId)
    {
        var result =
            await service.GetDetailsAsync(localAppId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{licenseId:int}/details")]
    public async Task<IActionResult> GetDetailsById(
        int licenseId)
    {
        var result =
            await service.GetLicenseDetailsByIdAsync(licenseId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    private static IActionResult HandleFailure(Result result)
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
                new ObjectResult(new { error = result.Error })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                },

            _ =>
                new ObjectResult(new { error = result.Error })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                }
        };
    }
}