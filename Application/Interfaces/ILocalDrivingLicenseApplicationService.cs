using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ILocalDrivingLicenseApplicationService
    {
        
        Task<List<LocalDrivingLicenseApplicationListDto>> GetAllLocalDrivingLicenseApplicationsAsync();
        Task<LocalDrivingLicenseApplicationListDto?> GetLocalDrivingLicenseApplicationByIdAsync(int id);

       
        Task<List<LocalDrivingLicenseApplicationListDto>> GetLocalDrivingLicenseApplicationsByApplicationIdAsync(int applicationId);
        Task<List<LocalDrivingLicenseApplicationListDto>> GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(int licenseClassId);
        Task<List<LocalDrivingLicenseApplicationListDto>> GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(int applicantPersonId);

       
        Task<int> AddLocalDrivingLicenseApplicationAsync(LocalDrivingLicenseApplicationCreateUpdateDto dto);
        Task<bool> UpdateLocalDrivingLicenseApplicationAsync(int id, LocalDrivingLicenseApplicationCreateUpdateDto dto);
        Task<bool> DeleteLocalDrivingLicenseApplicationAsync(int id);

        Task<int?> GetApplicationIdByLocalIdAsync(int localId);

        Task<bool> IsLocalDrivingLicenseApplicationExistsAsync(int id);
    }
}