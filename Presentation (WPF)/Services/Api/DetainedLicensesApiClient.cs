using DVLD.Contracts.DetainedLicense;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class DetainedLicensesApiClient(
    IApiClient apiClient) : IDetainedLicensesApiClient
{
    public Task<ApiResult<List<DetainedLicenseResponse>>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<DetainedLicenseResponse>>(
            "api/detainedlicenses",
            cancellationToken);

    public Task<ApiResult<DetainedLicenseResponse>>
        GetByIdAsync(
            int detainId,
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<DetainedLicenseResponse>(
            $"api/detainedlicenses/{detainId}",
            cancellationToken);

    public Task<ApiResult<DetainedLicenseResponse>>
        GetActiveByLicenseIdAsync(
            int licenseId,
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<DetainedLicenseResponse>(
            $"api/detainedlicenses/license/{licenseId}/active",
            cancellationToken);

    public async Task<ApiResult<bool>>
        IsLicenseDetainedAsync(
            int licenseId,
            CancellationToken cancellationToken = default)
    {
        var result = await apiClient.GetAsync<DetainedStatusResponse>(
            $"api/detainedlicenses/license/{licenseId}/detained",
            cancellationToken);

        if (result.IsFailure)
            return ApiResult<bool>.Failure(result.Error);

        return ApiResult<bool>.Success(
            result.Value?.Detained ?? false);
    }

    public Task<ApiResult<DetainedLicenseResponse>>
        DetainAsync(
            CreateDetainedLicenseRequest request,
            CancellationToken cancellationToken = default)
        => apiClient.PostAsync<
            CreateDetainedLicenseRequest,
            DetainedLicenseResponse>(
            "api/detainedlicenses",
            request,
            cancellationToken);

    public async Task<ApiResult>
        ReleaseAsync(
            ReleaseDetainedLicenseRequest request,
            CancellationToken cancellationToken = default)
    {
        var result = await apiClient.PostAsync<
            ReleaseDetainedLicenseRequest,
            object>(
            "api/detainedlicenses/release",
            request,
            cancellationToken);

        return result.IsSuccess
            ? ApiResult.Success()
            : ApiResult.Failure(result.Error);
    }

    private sealed class DetainedStatusResponse
    {
        public bool Detained { get; init; }
    }
}