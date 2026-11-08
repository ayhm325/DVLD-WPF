using Application.Common.Results;
using Application.DTOs.CountryDTO;
using Application.Interfaces;
using Application.Mappings;

namespace Application.Services;

public class CountryService : ICountryService
{
    private readonly ICountryRepository _countryRepository;

    public CountryService(ICountryRepository countryRepository)
    {
        _countryRepository = countryRepository;
    }

    public async Task<Result<List<CountryDto>>> GetAllCountriesAsync(
        CancellationToken cancellationToken = default)
    {
        var countries =
            await _countryRepository.GetAllCountriesAsync(cancellationToken);

        var result =
            countries
                .Select(CountryMapper.ToDto)
                .ToList();

        return Result<List<CountryDto>>.Success(result);
    }
}