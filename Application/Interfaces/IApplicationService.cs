using Application.DTOs;
using Application.Common.Results;

namespace Application.Interfaces
{
    public interface IApplicationService
    {
        Task<List<ApplicationDto>> GetAllApplicationsAsync();


        Task<Result<ApplicationDto>> GetApplicationByIdAsync(int id);


        Task<Result<int>> AddNewApplicationAsync(ApplicationDto dto);


        Task<Result> UpdateApplicationAsync(ApplicationDto dto);


        Task<Result> DeleteApplicationAsync(int id);


        Task<int?> HasDuplicateApplicationAsync(
            int personId,
            int licenseClassId);



        Task<Result> CompleteApplicationAsync(int id);


        Task<Result> CancelApplicationAsync(int id);


        Task<Result<ApplicationBasicInfoDto>> GetBasicInfoAsync(int id);
    }
}