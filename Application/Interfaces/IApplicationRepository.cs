using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IApplicationRepository
{
    // =========================================================
    // GET
    // =========================================================

    Task<ApplicationD?> GetApplicationByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ApplicationD?> GetApplicationForUpdateAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<ApplicationD>> GetAllApplicationsAsync(
        CancellationToken cancellationToken = default);

    Task<List<ApplicationD>> GetApplicationsByPersonIdAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<List<ApplicationD>> GetApplicationsByApplicationTypeIdAsync(
        int applicationTypeId,
        CancellationToken cancellationToken = default);

    Task<List<ApplicationD>> GetApplicationsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<List<ApplicationD>> GetApplicationsByStatusAsync(
        AppStatus status,
        CancellationToken cancellationToken = default);

    Task<ApplicationD?> GetApplicationForIssuanceAsync(
        int id,
        CancellationToken cancellationToken = default);

    // =========================================================
    // CHECKS
    // =========================================================

    Task<bool> IsApplicationExistsByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> IsPersonHasActiveApplicationAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<bool> IsPersonHasActiveApplicationOfTypeAsync(
        int personId,
        int applicationTypeId,
        CancellationToken cancellationToken = default);

    // =========================================================
    // CREATE
    // =========================================================

    Task AddNewApplicationAsync(
        ApplicationD application,
        CancellationToken cancellationToken = default);


    // =========================================================
    // DELETE
    // =========================================================

    void DeleteApplication(
        ApplicationD application);
}
