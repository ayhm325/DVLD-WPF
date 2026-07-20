using Domain.Entities;

namespace Application.Interfaces
{
    public interface ILocalDrivingLicenseApplicationRepository
    {
        Task<List<LocalDrivingLicenseApplication>> GetAllAsync();

        Task<LocalDrivingLicenseApplication?> GetByIdAsync(int id);

        Task<List<LocalDrivingLicenseApplication>> GetByPersonIdAsync(int personId);

        Task<List<LocalDrivingLicenseApplication>> GetByApplicationIdAsync(int applicationId);

        Task<List<LocalDrivingLicenseApplication>> GetByLicenseClassIdAsync(int licenseClassId);


        Task<int> GetPassedTestCountAsync(int localAppId);

        Task<int?> GetApplicationIdByLocalIdAsync(int localId);


        Task<int> CreateLocalDrivingLicenseApplicationAsync(
            LocalDrivingLicenseApplication entity);


        Task<bool> UpdateAsync(
            LocalDrivingLicenseApplication entity);


        Task<bool> DeleteAsync(int id);
    }
}