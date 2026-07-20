using Domain.Entities;

namespace Application.Interfaces
{
    public interface ICountryRepository
    {
        Task<List<Country>> GetAllCountriesAsync();
    }
}