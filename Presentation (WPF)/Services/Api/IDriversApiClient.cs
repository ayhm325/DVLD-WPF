using DVLD.Contracts.Driver;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface IDriversApiClient
{
    Task<ApiResult<IReadOnlyList<DriverResponse>>>
        GetAllAsync(
            CancellationToken cancellationToken = default);

    Task<ApiResult<DriverResponse>>
        GetByPersonIdAsync(
            int personId,
            CancellationToken cancellationToken = default);
}