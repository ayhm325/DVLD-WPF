using Application.Common.Results;
using Application.DTOs.CountryDTO;
using Application.Interfaces;
using DVLD.Contracts.Country;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class CountriesController(ICountryService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetAllCountriesAsync();
        if (result.IsFailure) return HandleFailure(result);

        return Ok(result.Value?.Select(MapToResponse).ToList() ?? []);
    }

    private static CountryResponse MapToResponse(CountryDto dto) => new()
    {
        CountryId = dto.CountryId,
        CountryName = dto.CountryName
    };

    private IActionResult HandleFailure(Result result) =>
        result.ErrorType switch
        {
            ErrorType.Validation => BadRequest(
                new { error = result.Error }),

            ErrorType.NotFound => NotFound(
                new { error = result.Error }),

            ErrorType.Conflict => Conflict(
                new { error = result.Error }),

            ErrorType.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                new { error = result.Error }),

            _ => StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = result.Error })
        };
}