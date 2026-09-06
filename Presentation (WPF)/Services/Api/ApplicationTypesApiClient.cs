using Application.Common.Results;
using Application.DTOs;

namespace Presentation.Services.Api;

public sealed class ApplicationTypesApiClient(
    IApiClient apiClient) : IApplicationTypesApiClient
{
    private readonly IApiClient _apiClient =
        apiClient ?? throw new ArgumentNullException(nameof(apiClient));

    public Task<Result<List<ApplicationTypeDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAsync<List<ApplicationTypeDto>>(
            "api/applicationtypes",
            cancellationToken);
    }

    public Task<Result<ApplicationTypeDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Task.FromResult(
                Result<ApplicationTypeDto>.FromValidationFailure(
                    "Application type ID must be greater than zero."));
        }

        return _apiClient.GetAsync<ApplicationTypeDto>(
            $"api/applicationtypes/{id}",
            cancellationToken);
    }

    public Task<Result> UpdateAsync(
        int id,
        ApplicationTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (id <= 0)
        {
            return Task.FromResult(
                Result.ValidationFailure(
                    "Application type ID must be greater than zero."));
        }

        return _apiClient.PutAsync(
            $"api/applicationtypes/{id}",
            dto,
            cancellationToken);
    }
}