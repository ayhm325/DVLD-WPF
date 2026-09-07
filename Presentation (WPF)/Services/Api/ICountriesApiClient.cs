using DVLD.Contracts.Country;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface ICountriesApiClient
{
    Task<ApiResult<List<CountryResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}