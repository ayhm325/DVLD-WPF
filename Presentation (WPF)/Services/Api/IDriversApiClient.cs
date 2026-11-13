using DVLD.Contracts.Driver;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface IDriversApiClient
{
    Task<ApiResult<IReadOnlyList<DriverListResponse>>>
        GetAllAsync(
            CancellationToken cancellationToken = default);

    Task<ApiResult<DriverResponse>>
        GetByPersonIdAsync(
            int personId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<int>> CreateAsync(
        CreateDriverRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteAsync(
        int driverId,
        CancellationToken cancellationToken = default);
}
