using Application.Common.Results;
using Application.DTOs.CountryDTO;

namespace Presentation.Services.Api;

public interface ICountriesApiClient
{
    Task<Result<List<CountryDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}