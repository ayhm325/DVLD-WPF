using Application.DTOs;
using Domain.Entities;


namespace Application.Interfaces
{
    public interface IApplicationService
    {
        Task<List<ApplicationDto>> GetAllApplicationsAsync();

        Task<ApplicationDto?> GetApplicationByIdAsync(int id);

        Task<int> AddNewApplicationAsync(ApplicationDto dto);

        Task<bool> UpdateApplicationAsync(ApplicationDto dto);

        Task<bool> DeleteApplicationAsync(int id);

        Task<int?> HasDuplicateApplicationAsync(int personId, int licenseClassId);

        Task<bool> CompleteApplicationAsync(int id);
        
        Task<bool> CancelApplicationAsync(int id);

        Task<ApplicationBasicInfoDto> GetBasicInfoAsync(int id);
       
    }
}