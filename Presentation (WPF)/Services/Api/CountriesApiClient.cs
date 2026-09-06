using Application.Common.Results;
using Application.DTOs.CountryDTO;

namespace Presentation.Services.Api;

public sealed class CountriesApiClient(
    IApiClient apiClient) : ICountriesApiClient
{
    private readonly IApiClient _apiClient =
        apiClient ?? throw new ArgumentNullException(nameof(apiClient));

    public Task<Result<List<CountryDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAsync<List<CountryDto>>(
            "api/countries",
            cancellationToken);
    }
}