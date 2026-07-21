using Application.Common.Results;
using Application.Interfaces;
using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    public class CountryService : ICountryService
    {
        private readonly ICountryRepository _countryRepository;

        public CountryService(ICountryRepository countryRepository)
        {
            _countryRepository = countryRepository;
        }

        public async Task<Result<List<Country>>> GetAllCountriesAsync()
        {
            var countries = await _countryRepository.GetAllCountriesAsync();

            return Result<List<Country>>.Success(countries);
        }
    }
}