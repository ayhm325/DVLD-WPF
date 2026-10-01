using Application.Common.Results;
using Application.DTOs.ApplicationDTO;

namespace Application.Interfaces;

public interface IApplicationService
{
    // =========================================================
    // GET
    // =========================================================

    Task<Result<List<ApplicationDto>>>
        GetAllApplicationsAsync();

    Task<Result<ApplicationDto>>
        GetApplicationByIdAsync(int id);

    Task<Result<ApplicationBasicInfoDto>>
        GetBasicInfoAsync(int id);

    Task<Result<ApplicationDto>>
    GetApplicationForIssuanceAsync(int id);


    // =========================================================
    // CREATE / UPDATE
    // =========================================================

    Task<Result<int>>
        AddNewApplicationAsync(
            CreateApplicationDto dto);

    Task<Result>
        UpdateApplicationAsync(
            UpdateApplicationDto dto);


    // =========================================================
    // DELETE
    // =========================================================

    Task<Result>
        DeleteApplicationAsync(int id);


    // =========================================================
    // BUSINESS
    // =========================================================


    Task<Result>
        CompleteApplicationAsync(int id);

    Task<Result>
        CancelApplicationAsync(int id);

    /// <summary>
    /// Mutates the application status to Completed without calling SaveChangesAsync.
    /// Intended for use by outer workflow services that manage their own transaction
    /// and persistence boundary. The outer caller is responsible for flushing changes.
    /// </summary>
    Task<Result>
        CompleteApplicationStateAsync(int id);
}