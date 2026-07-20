using Domain.Entities;

namespace Application.Interfaces
{
    public interface IApplicationTypeRepository
    {
        Task<List<ApplicationType>> GetAllApplicationTypesAsync();

        Task<ApplicationType?> GetApplicationTypeByIdAsync(int id);

        Task<bool> UpdateApplicationTypeAsync(ApplicationType appType);
    }
}