using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ILocalDrivingLicenseApplicationService
    {
        
        Task<List<LocalDrivingLicenseApplicationDto>> GetAllLocalDrivingLicenseApplicationsAsync();
        Task<LocalDrivingLicenseApplicationDto?> GetLocalDrivingLicenseApplicationByIdAsync(int id);

       
        Task<List<LocalDrivingLicenseApplicationDto>> GetLocalDrivingLicenseApplicationsByApplicationIdAsync(int applicationId);
        Task<List<LocalDrivingLicenseApplicationDto>> GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(int licenseClassId);
        Task<List<LocalDrivingLicenseApplicationDto>> GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(int applicantPersonId);

       
        Task<int> AddLocalDrivingLicenseApplicationAsync(LocalDrivingLicenseApplicationCreateUpdateDto dto);
        Task<bool> UpdateLocalDrivingLicenseApplicationAsync(int id, LocalDrivingLicenseApplicationCreateUpdateDto dto);
        Task<bool> DeleteLocalDrivingLicenseApplicationAsync(int id);

        
        Task<bool> IsLocalDrivingLicenseApplicationExistsAsync(int id);
    }
}