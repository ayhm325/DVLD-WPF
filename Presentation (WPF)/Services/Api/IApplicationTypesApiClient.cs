using DVLD.Contracts.ApplicationType;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface IApplicationTypesApiClient
{
    Task<ApiResult<List<ApplicationTypeResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<ApplicationTypeResponse>> GetByIdAsync(
        int applicationTypeId,
        CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateAsync(
        int applicationTypeId,
        UpdateApplicationTypeRequest request,
        CancellationToken cancellationToken = default);
}