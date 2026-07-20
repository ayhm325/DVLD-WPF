using Domain.Entities;

namespace Application.Interfaces
{
    public interface IApplicationRepository
    {
        Task<ApplicationD?> GetApplicationByIdAsync(int id);

        Task<List<ApplicationD>> GetAllApplicationsAsync();

        Task<List<ApplicationD>> GetApplicationsByPersonIdAsync(int personId);

        Task<List<ApplicationD>> GetApplicationsByApplicationTypeIdAsync(int applicationTypeId);

        Task<List<ApplicationD>> GetApplicationsByUserIdAsync(int userId);

        Task<List<ApplicationD>> GetApplicationsByStatusAsync(int status);


        Task<bool> IsApplicationExistsByIdAsync(int id);

        Task<bool> IsPersonHasActiveApplicationAsync(int personId);

        Task<bool> IsPersonHasActiveApplicationOfTypeAsync(
            int personId,
            int applicationTypeId);


        Task<int?> HasDuplicateApplicationAsync(
            int personId,
            int licenseClassId);


        Task<int> AddNewApplicationAsync(ApplicationD application);


        Task<bool> UpdateApplicationAsync(ApplicationD application);


        Task<bool> DeleteApplicationAsync(int id);


        Task<bool> CompleteApplicationAsync(int applicationId);


        Task<bool> CancelApplicationAsync(int applicationId);
    }
}