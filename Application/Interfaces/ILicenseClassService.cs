using Application.DTOs;

namespace Application.Interfaces
{
    public interface ILicenseClassService
    {
        Task<List<LicenseClassDto>> GetAllLicenseClassesAsync();
    
        Task<LicenseClassDto?> GetLicenseClassByIdAsync(int id);

    }
}
