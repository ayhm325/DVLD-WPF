using DVLD.Contracts.ApplicationType;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class ApplicationTypesApiClient(
    IApiClient apiClient) : IApplicationTypesApiClient
{
    public Task<ApiResult<List<ApplicationTypeResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<List<ApplicationTypeResponse>>(
            "api/applicationtypes",
            cancellationToken);
    }

    public Task<ApiResult<ApplicationTypeResponse>> GetByIdAsync(
        int applicationTypeId,
        CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<ApplicationTypeResponse>(
            $"api/applicationtypes/{applicationTypeId}",
            cancellationToken);
    }

    public Task<ApiResult> UpdateAsync(
        int applicationTypeId,
        UpdateApplicationTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        return apiClient.PutAsync(
            $"api/applicationtypes/{applicationTypeId}",
            request,
            cancellationToken);
    }
}