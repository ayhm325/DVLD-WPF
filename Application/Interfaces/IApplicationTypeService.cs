

using Application.DTOs;

namespace Application.Interfaces
{
    public interface IApplicationTypeService
    {
        Task<List<ApplicationTypeDto>> GetAllApplicationTypesAsync();

        Task<ApplicationTypeDto?> GetApplicationTypeByIdAsync(int id);   

        Task<bool> UpdateApplicationTypeAsync(int id, ApplicationTypeDto dto);

        //Task<bool> IsApplicationTypeExistsAsync(int id);
    }
}
