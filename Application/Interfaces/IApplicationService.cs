using Application.Common.Results;
using Application.DTOs.ApplicationDTO;

namespace Application.Interfaces;

public interface IApplicationService
{
    // =========================================================
    // GET
    // =========================================================

    Task<Result<List<ApplicationDto>>>
        GetAllApplicationsAsync(
            CancellationToken cancellationToken = default);

    Task<Result<ApplicationDto>>
        GetApplicationByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

    Task<Result<ApplicationBasicInfoDto>>
        GetBasicInfoAsync(
            int id,
            CancellationToken cancellationToken = default);

    Task<Result<ApplicationDto>>
        GetApplicationForIssuanceAsync(
            int id,
            CancellationToken cancellationToken = default);


    // =========================================================
    // CREATE / UPDATE
    // =========================================================

    Task<Result<int>>
        AddNewApplicationAsync(
            CreateApplicationDto dto,
            CancellationToken cancellationToken = default);

    Task<Result>
        UpdateApplicationAsync(
            UpdateApplicationDto dto,
            CancellationToken cancellationToken = default);


    // =========================================================
    // DELETE
    // =========================================================

    Task<Result>
        DeleteApplicationAsync(
            int id,
            CancellationToken cancellationToken = default);


    // =========================================================
    // BUSINESS
    // =========================================================

    Task<Result>
        CancelApplicationAsync(
            int id,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Mutates the application status to Completed without calling SaveChangesAsync.
    /// Intended for use by outer workflow services that manage their own transaction
    /// and persistence boundary. The outer caller is responsible for flushing changes.
    /// </summary>
    Task<Result>
        CompleteApplicationStateAsync(
            int id,
            CancellationToken cancellationToken = default);
}