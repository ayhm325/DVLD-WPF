using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces
{
    public interface IInternationalService
    {
        // =========================
        // GET
        // =========================

        Task<Result<List<InternationalDto>>> GetAllAsync();

        Task<Result<InternationalDto>> GetByIdAsync(int internationalLicenseId);

        Task<Result<List<InternationalDto>>> GetByDriverIdAsync(int driverId);

        Task<Result<InternationalDto>> GetByApplicationIdAsync(int applicationId);

        Task<Result<List<InternationalDto>>> GetByLocalLicenseIdAsync(int localLicenseId);


        // =========================
        // CHECKS
        // =========================

        Task<bool> HasActiveInternationalLicenseAsync(int driverId);


        // =========================
        // COMMANDS
        // =========================

        Task<Result> AddAsync(InternationalDto dto);

        Task<Result> UpdateAsync(InternationalDto dto);

        Task<Result> DeleteAsync(int internationalLicenseId);


        // =========================
        // BUSINESS
        // =========================

        Task<Result<int>> IssueInternationalLicenseAsync(int localLicenseId);

        Task<Result<DriverLicenseInfoDto>> GetLocalLicenseInfoAsync(int licenseId);
    }
}