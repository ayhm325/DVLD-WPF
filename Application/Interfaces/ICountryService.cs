using Application.Common.Results;
using Application.DTOs.CountryDTO;

namespace Application.Interfaces;

public interface ICountryService
{
    Task<Result<List<CountryDto>>> GetAllCountriesAsync();
}