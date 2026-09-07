using DVLD.Contracts.Application;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class ApplicationsApiClient(
    IApiClient apiClient) : IApplicationsApiClient
{
    public Task<ApiResult<List<ApplicationResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<List<ApplicationResponse>>(
            "api/applications",
            cancellationToken);
    }

    public Task<ApiResult<ApplicationResponse>> GetByIdAsync(
        int applicationId,
        CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<ApplicationResponse>(
            $"api/applications/{applicationId}",
            cancellationToken);
    }

    public Task<ApiResult<ApplicationBasicInfoResponse>> GetBasicInfoAsync(
        int applicationId,
        CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<ApplicationBasicInfoResponse>(
            $"api/applications/{applicationId}/basic-info",
            cancellationToken);
    }

    public async Task<ApiResult<int>> CreateAsync(
        CreateApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await apiClient.PostAsync<
            CreateApplicationRequest,
            CreateApplicationResponse>(
            "api/applications",
            request,
            cancellationToken);

        if (result.IsFailure)
            return ApiResult<int>.Failure(result.Error);

        return ApiResult<int>.Success(result.Value!.ApplicationId);
    }

    public Task<ApiResult> UpdateAsync(
        int applicationId,
        UpdateApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        return apiClient.PutAsync(
            $"api/applications/{applicationId}",
            request,
            cancellationToken);
    }

    public Task<ApiResult> DeleteAsync(
        int applicationId,
        CancellationToken cancellationToken = default)
    {
        return apiClient.DeleteAsync(
            $"api/applications/{applicationId}",
            cancellationToken);
    }

    public async Task<ApiResult> CompleteAsync(
    int applicationId,
    CancellationToken cancellationToken = default)
    {
        var result =
            await apiClient.PostAsync<object, object>(
                $"api/applications/{applicationId}/complete",
                new { },
                cancellationToken);

        return result.IsSuccess
            ? ApiResult.Success()
            : ApiResult.Failure(result.Error);
    }

    public async Task<ApiResult> CancelAsync(
        int applicationId,
        CancellationToken cancellationToken = default)
    {
        var result =
            await apiClient.PostAsync<object, object>(
                $"api/applications/{applicationId}/cancel",
                new { },
                cancellationToken);

        return result.IsSuccess
            ? ApiResult.Success()
            : ApiResult.Failure(result.Error);
    }
}