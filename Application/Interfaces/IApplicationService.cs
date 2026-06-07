using Application.DTOs;


namespace Application.Interfaces
{
    public interface IApplicationService
    {
        Task<List<ApplicationDto>> GetAllApplicationsAsync();

        Task<ApplicationDto?> GetApplicationByIdAsync(int id);

        Task<int> AddNewApplicationAsync(ApplicationDto dto);

        Task<bool> UpdateApplicationAsync(ApplicationDto dto);

        Task<bool> DeleteApplicationAsync(int id);
    }
}