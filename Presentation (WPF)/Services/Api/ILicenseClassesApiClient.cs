using DVLD.Contracts.LicenseClass;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface ILicenseClassesApiClient
{
    Task<ApiResult<List<LicenseClassResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<LicenseClassResponse>> GetByIdAsync(
        int licenseClassId,
        CancellationToken cancellationToken = default);
}