using DVLD.Contracts.Country;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class CountriesApiClient(
    IApiClient apiClient) : ICountriesApiClient
{
    private readonly IApiClient _apiClient =
        apiClient
        ?? throw new ArgumentNullException(nameof(apiClient));

    public Task<ApiResult<List<CountryResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAsync<List<CountryResponse>>(
            "api/countries",
            cancellationToken);
    }
}