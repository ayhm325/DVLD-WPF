using Application.DTOs.CountryDTO;
using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.Country;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class CountriesController(
    ICountryService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await service.GetAllCountriesAsync();

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            result.Value?
                .Select(MapToResponse)
                .ToList()
            ?? []);
    }

    private static CountryResponse MapToResponse(
        CountryDto dto)
        => new()
        {
            CountryId = dto.CountryId,
            CountryName = dto.CountryName
        };
}