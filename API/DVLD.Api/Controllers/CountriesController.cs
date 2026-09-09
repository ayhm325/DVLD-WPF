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
public class CountriesController : ControllerBase
{
    private readonly ICountryService _countryService;

    public CountriesController(ICountryService countryService)
    {
        _countryService = countryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _countryService.GetAllCountriesAsync();

        if (result.IsFailure)
            return HandleFailure(result);

        var response = result.Value?
            .Select(MapToResponse)
            .ToList() ?? [];


        return Ok(response);
    }

    private static CountryResponse MapToResponse(
        CountryDto dto)
    {
        return new CountryResponse
        {
            CountryId = dto.CountryId,
            CountryName = dto.CountryName
        };
    }

    private IActionResult HandleFailure(Result result)
    {
        return result.ErrorType switch
        {
            ErrorType.Validation =>
                BadRequest(new { error = result.Error }),

            ErrorType.NotFound =>
                NotFound(new { error = result.Error }),

            ErrorType.Conflict =>
                Conflict(new { error = result.Error }),

            ErrorType.Forbidden =>
                StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { error = result.Error }),

            _ =>
                StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = result.Error })
        };
    }
}