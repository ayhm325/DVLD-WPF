using Application.Common.Results;
using Application.DTOs.InternationalLicenseDTO;
using Application.DTOs.LicenseDTO;

namespace Application.Interfaces;

public interface IInternationalService
{
    Task<Result<List<InternationalDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<InternationalDto>> GetByIdAsync(
    int internationalLicenseId,
    CancellationToken cancellationToken = default);

    Task<Result<List<InternationalDto>>> GetByDriverIdAsync(
        int driverId,
        CancellationToken cancellationToken = default);

    Task<Result<InternationalDto>> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken cancellationToken = default);

    Task<Result<List<InternationalDto>>> GetByLocalLicenseIdAsync(
        int localLicenseId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveInternationalLicenseAsync(
        int driverId,
        CancellationToken cancellationToken = default);

    Task<Result<int>> IssueInternationalLicenseAsync(
        int localLicenseId,
        CancellationToken cancellationToken = default);

    Task<Result<DriverLicenseInfoDto>> GetLocalLicenseInfoAsync(
        int licenseId,
        CancellationToken cancellationToken = default);
}
