using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    public class CountryService : ICountryService
    {
        private readonly CountryRepository _countryRepository;

        // نحقن الـ Repository داخل الخدمة
        public CountryService(CountryRepository countryRepository)
        {
            _countryRepository = countryRepository;
        }

        public async Task<List<Country>> GetAllCountriesAsync()
        {
            // استدعاء مباشر للـ Repository لجلب البيانات
            return await _countryRepository.GetAllCountriesAsync();
        }
    }
}