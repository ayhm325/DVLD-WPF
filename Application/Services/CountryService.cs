using Application.Interfaces;
using Domain.Entities;


namespace Application.Services
{
    public class CountryService : ICountryService
    {
        private readonly ICountryRepository _countryRepository;

        // نحقن الـ Repository داخل الخدمة
        public CountryService(ICountryRepository countryRepository)
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