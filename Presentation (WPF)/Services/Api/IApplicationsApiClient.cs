using DVLD.Contracts.Application;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface IApplicationsApiClient
{
    Task<ApiResult<List<ApplicationResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<ApplicationResponse>> GetByIdAsync(
        int applicationId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<ApplicationBasicInfoResponse>> GetBasicInfoAsync(
        int applicationId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<int>> CreateAsync(
        CreateApplicationRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateAsync(
        int applicationId,
        UpdateApplicationRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteAsync(
        int applicationId,
        CancellationToken cancellationToken = default);

    Task<ApiResult> CompleteAsync(
        int applicationId,
        CancellationToken cancellationToken = default);

    Task<ApiResult> CancelAsync(
        int applicationId,
        CancellationToken cancellationToken = default);
}