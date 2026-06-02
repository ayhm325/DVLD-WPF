using System.Collections.Generic;
using System.Threading.Tasks;
using DVLD.Domain.Entities; // أو مسار كائن الـ Country عندك

namespace Application.Interfaces
{
    public interface ICountryService
    {
        Task<List<Country>> GetAllCountriesAsync();
        // يمكنك إضافة دالة GetCountryById مستقبلاً إذا احتجتها
    }
}