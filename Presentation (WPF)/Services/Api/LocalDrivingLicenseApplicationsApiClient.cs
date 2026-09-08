using DVLD.Contracts.LocalDrivingLicenseApplication;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class LocalDrivingLicenseApplicationsApiClient(
    IApiClient apiClient) : ILocalDrivingLicenseApplicationsApiClient
{
    public Task<ApiResult<List<LocalDrivingLicenseApplicationResponse>>>
        GetAllAsync(CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<LocalDrivingLicenseApplicationResponse>>(
            "api/localdrivinglicenseapplications",
            cancellationToken);

    public Task<ApiResult<LocalDrivingLicenseApplicationResponse>>
        GetByIdAsync(
            int localApplicationId,
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<LocalDrivingLicenseApplicationResponse>(
            $"api/localdrivinglicenseapplications/{localApplicationId}",
            cancellationToken);

    public Task<ApiResult<List<LocalDrivingLicenseApplicationResponse>>>
        GetByApplicationIdAsync(
            int applicationId,
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<LocalDrivingLicenseApplicationResponse>>(
            $"api/localdrivinglicenseapplications/application/{applicationId}",
            cancellationToken);

    public Task<ApiResult<List<LocalDrivingLicenseApplicationResponse>>>
        GetByLicenseClassIdAsync(
            int licenseClassId,
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<LocalDrivingLicenseApplicationResponse>>(
            $"api/localdrivinglicenseapplications/license-class/{licenseClassId}",
            cancellationToken);

    public Task<ApiResult<List<LocalDrivingLicenseApplicationResponse>>>
        GetByApplicantPersonIdAsync(
            int personId,
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<LocalDrivingLicenseApplicationResponse>>(
            $"api/localdrivinglicenseapplications/person/{personId}",
            cancellationToken);

    public async Task<ApiResult<int>>
        GetApplicationIdAsync(
            int localApplicationId,
            CancellationToken cancellationToken = default)
    {
        var result =
            await apiClient.GetAsync<ApplicationIdResponse>(
                $"api/localdrivinglicenseapplications/{localApplicationId}/application-id",
                cancellationToken);

        return result.IsFailure
            ? ApiResult<int>.Failure(result.Error)
            : ApiResult<int>.Success(result.Value!.ApplicationId);
    }

    public Task<ApiResult<CreateLocalDrivingLicenseApplicationInfoResponse>>
        GetCreateInfoAsync(
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<CreateLocalDrivingLicenseApplicationInfoResponse>(
            "api/localdrivinglicenseapplications/create-info",
            cancellationToken);

    public async Task<ApiResult<int>>
        CreateAsync(
            CreateLocalDrivingLicenseApplicationRequest request,
            CancellationToken cancellationToken = default)
    {
        var result =
            await apiClient.PostAsync<
                CreateLocalDrivingLicenseApplicationRequest,
                CreateLocalDrivingLicenseApplicationResponse>(
                "api/localdrivinglicenseapplications",
                request,
                cancellationToken);

        return result.IsFailure
            ? ApiResult<int>.Failure(result.Error)
            : ApiResult<int>.Success(
                result.Value!.LocalDrivingLicenseApplicationId);
    }

    public Task<ApiResult>
        UpdateAsync(
            int localApplicationId,
            UpdateLocalDrivingLicenseApplicationRequest request,
            CancellationToken cancellationToken = default)
        => apiClient.PutAsync(
            $"api/localdrivinglicenseapplications/{localApplicationId}",
            request,
            cancellationToken);

    public Task<ApiResult>
        DeleteAsync(
            int localApplicationId,
            CancellationToken cancellationToken = default)
        => apiClient.DeleteAsync(
            $"api/localdrivinglicenseapplications/{localApplicationId}",
            cancellationToken);

    public Task<ApiResult>
        CancelAsync(
            int localApplicationId,
            CancellationToken cancellationToken = default)
        => apiClient.PostAsync(
            $"api/localdrivinglicenseapplications/{localApplicationId}/cancel",
            cancellationToken);

    private sealed class ApplicationIdResponse
    {
        public int ApplicationId { get; init; }
    }
}