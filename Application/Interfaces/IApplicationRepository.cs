using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IApplicationRepository
{
    // =========================================================
    // GET
    // =========================================================

    Task<ApplicationD?> GetApplicationByIdAsync(
        int id);

    Task<ApplicationD?> GetApplicationForUpdateAsync(
        int id);

    Task<List<ApplicationD>> GetAllApplicationsAsync();

    Task<List<ApplicationD>> GetApplicationsByPersonIdAsync(
        int personId);

    Task<List<ApplicationD>> GetApplicationsByApplicationTypeIdAsync(
        int applicationTypeId);

    Task<List<ApplicationD>> GetApplicationsByUserIdAsync(
        int userId);

    Task<List<ApplicationD>> GetApplicationsByStatusAsync(
        AppStatus status);


    // =========================================================
    // CHECKS
    // =========================================================

    Task<bool> IsApplicationExistsByIdAsync(
        int id);

    Task<bool> IsPersonHasActiveApplicationAsync(
        int personId);

    Task<bool> IsPersonHasActiveApplicationOfTypeAsync(
        int personId,
        int applicationTypeId);

    Task<int?> HasDuplicateApplicationAsync(
        int personId,
        int licenseClassId);


    // =========================================================
    // CREATE
    // =========================================================

    Task AddNewApplicationAsync(
        ApplicationD application);


    // =========================================================
    // DELETE
    // =========================================================

    void DeleteApplication(
        ApplicationD application);
}
