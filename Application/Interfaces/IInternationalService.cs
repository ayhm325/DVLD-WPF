using Application.Common.Results;
using Application.DTOs.InternationalLicenseDTO;
using Application.DTOs.LicenseDTO;

namespace Application.Interfaces;

public interface IInternationalService
{
    Task<Result<List<InternationalDto>>> GetAllAsync();

    Task<Result<InternationalDto>> GetByIdAsync(
    int internationalLicenseId);

    Task<Result<List<InternationalDto>>> GetByDriverIdAsync(
        int driverId);

    Task<Result<InternationalDto>> GetByApplicationIdAsync(
        int applicationId);

    Task<Result<List<InternationalDto>>> GetByLocalLicenseIdAsync(
        int localLicenseId);

    Task<bool> HasActiveInternationalLicenseAsync(
        int driverId);

    Task<Result<int>> IssueInternationalLicenseAsync(
        int localLicenseId);

    Task<Result<DriverLicenseInfoDto>> GetLocalLicenseInfoAsync(
        int licenseId);
}
