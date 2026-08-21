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

    Task<int?>
        HasDuplicateApplicationAsync(
            int personId,
            int licenseClassId);

    Task<Result>
        CompleteApplicationAsync(int id);

    Task<Result>
        CancelApplicationAsync(int id);
}