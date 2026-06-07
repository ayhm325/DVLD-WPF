

using Domain.Entities; 
namespace Application.Interfaces
{
    public interface ICountryService
    {
        Task<List<Country>> GetAllCountriesAsync();
        // يمكنك إضافة دالة GetCountryById مستقبلاً إذا احتجتها
    }
}