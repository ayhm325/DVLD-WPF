using DVLD.Contracts.Dashboard;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class DashboardApiClient(
    IApiClient apiClient) : IDashboardApiClient
{
    private readonly IApiClient _apiClient =
        apiClient
        ?? throw new ArgumentNullException(nameof(apiClient));

    public Task<ApiResult<DashboardResponse>> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAsync<DashboardResponse>(
            "api/dashboard/statistics",
            cancellationToken);
    }
}