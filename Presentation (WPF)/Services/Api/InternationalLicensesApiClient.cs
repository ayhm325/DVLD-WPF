using DVLD.Contracts.InternationalLicense;
using DVLD.Contracts.License;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class InternationalLicensesApiClient(
    IApiClient apiClient) : IInternationalLicensesApiClient
{
    public Task<ApiResult<List<InternationalLicenseListResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<List<InternationalLicenseListResponse>>(
            "api/internationallicenses", cancellationToken);

    public Task<ApiResult<List<InternationalLicenseListResponse>>> GetByDriverIdAsync(
        int driverId,
        CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<List<InternationalLicenseListResponse>>(
            $"api/internationallicenses/driver/{driverId}", cancellationToken);

    public Task<ApiResult<InternationalLicenseResponse>>
        GetByIdAsync(
            int internationalLicenseId,
            CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<InternationalLicenseResponse>(
            $"api/internationallicenses/{internationalLicenseId}",
            cancellationToken);
    }

    public Task<ApiResult<List<InternationalLicenseListResponse>>> GetByLocalLicenseIdAsync(
        int localLicenseId,
        CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<List<InternationalLicenseListResponse>>(
            $"api/internationallicenses/license/{localLicenseId}", cancellationToken);

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