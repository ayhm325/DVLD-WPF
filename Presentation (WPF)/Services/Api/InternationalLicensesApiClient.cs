using DVLD.Contracts.InternationalLicense;
using DVLD.Contracts.License;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class InternationalLicensesApiClient(
    IApiClient apiClient) : IInternationalLicensesApiClient
{
    public Task<ApiResult<List<InternationalLicenseResponse>>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<List<InternationalLicenseResponse>>(
            "api/internationallicenses",
            cancellationToken);
    }

    public Task<ApiResult<List<InternationalLicenseResponse>>>
        GetByDriverIdAsync(
            int driverId,
            CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<List<InternationalLicenseResponse>>(
            $"api/internationallicenses/driver/{driverId}",
            cancellationToken);
    }

    public Task<ApiResult<InternationalLicenseResponse>>
        GetByIdAsync(
            int internationalLicenseId,
            CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<InternationalLicenseResponse>(
            $"api/internationallicenses/{internationalLicenseId}",
            cancellationToken);
    }

    public Task<ApiResult<List<InternationalLicenseResponse>>>
        GetByLocalLicenseIdAsync(
            int localLicenseId,
            CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<List<InternationalLicenseResponse>>(
            $"api/internationallicenses/license/{localLicenseId}",
            cancellationToken);
    }

    public Task<ApiResult<DriverLicenseInfoResponse>>
        GetLocalLicenseInfoAsync(
            int licenseId,
            CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<DriverLicenseInfoResponse>(
            $"api/internationallicenses/license/{licenseId}/info",
            cancellationToken);
    }

    public Task<ApiResult<InternationalLicenseResponse>>
        IssueAsync(
            IssueInternationalLicenseRequest request,
            CancellationToken cancellationToken = default)
    {
        return apiClient.PostAsync<
            IssueInternationalLicenseRequest,
            InternationalLicenseResponse>(
            "api/internationallicenses",
            request,
            cancellationToken);
    }
}