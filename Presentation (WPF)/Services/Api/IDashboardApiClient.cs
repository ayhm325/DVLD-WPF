using DVLD.Contracts.Dashboard;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface IDashboardApiClient
{
    Task<ApiResult<DashboardResponse>> GetStatisticsAsync(
        CancellationToken cancellationToken = default);
}